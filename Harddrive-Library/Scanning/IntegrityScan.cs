﻿// Copyright (c) Zain Al-Ahmary.  All rights reserved.
// Licensed under the MIT License, (the "License"); you may not use this file except in compliance with the License. 
// You may obtain a copy of the License at https://mit-license.org/

using HDDL.Data;
using HDDL.Threading;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace HDDL.Scanning
{
    /// <summary>
    /// Utility class that performs integrity scans against stored hashes of files to see if anything has changed
    /// </summary>
    class IntegrityScan
    {
        private const int Max_Threads = 4;

        public delegate void IntegrityScanInitializing(IntegrityScan scanner, IEnumerable<string> targets);
        public delegate void IntegrityScanStarts(IntegrityScan scanner, int total);
        public delegate void IntegrityScanEnds(IntegrityScan scanner, IEnumerable<DiskItem> changedFiles, IEnumerable<DiskItem> scannedFiles);
        public delegate void IntegrityScanFocusStarts(IntegrityScan scanner, string target, long total, long current);
        public delegate void IntegrityScanFocusProgress(IntegrityScan scanner, string target, long total, long current);
        public delegate void IntegrityScanFocusEnds(IntegrityScan scanner, string target, long total, long current);
        public delegate void IntegrityScanFocusFailed(IntegrityScan scanner, string target, Exception cause);
        public delegate void IntegrityScanUnknownError(IntegrityScan scanner, Exception ex);
        public delegate void ScanStatusEventDelegate(IntegrityScan scanner, ScanStatus newStatus, ScanStatus oldStatus);

        /// <summary>
        /// Occurs when the status changes
        /// </summary>
        public event ScanStatusEventDelegate StatusEventOccurred;

        /// <summary>
        /// Occurs when the scan begins preparing to perform a scan
        /// </summary>
        public event IntegrityScanInitializing Preparing;

        /// <summary>
        /// Occurs when the scan starts
        /// </summary>
        public event IntegrityScanStarts ScanStarts;

        /// <summary>
        /// Occurs when the scan ends
        /// </summary>
        public event IntegrityScanEnds ScanEnds;

        /// <summary>
        /// Occurs when work on a file starts
        /// </summary>
        public event IntegrityScanFocusStarts StartedScanningFile;

        /// <summary>
        /// Occurs when a file scan completes
        /// </summary>
        public event IntegrityScanFocusEnds FinishedScanningFile;

        /// <summary>
        /// Occurs when an integrity scan on a file fails
        /// </summary>
        public event IntegrityScanFocusFailed FileScanFailed;

        /// <summary>
        /// Occurs when an unknown exception is thrown
        /// </summary>
        public event IntegrityScanUnknownError UnknownErrorOccurred;

        /// <summary>
        /// Used as the "last scanned" timestamp for all items altered during the scan
        /// </summary>
        private DateTime _scanMarker;

        /// <summary>
        /// Handles all data reads and writes
        /// </summary>
        private DataHandler _dh;

        /// <summary>
        /// A running list of Disk Items that have changed since their last scan
        /// </summary>
        private ConcurrentBag<DiskItem> _changes;

        /// <summary>
        /// A running list of all Disk Items scanned successfully
        /// </summary>
        private ConcurrentBag<DiskItem> _all;


        /// <summary>
        /// The scan location
        /// </summary>
        private List<FilteredLocationItem> _startLocations;

        private ScanStatus _status;
        /// <summary>
        /// The scanner's current status
        /// </summary>
        public ScanStatus Status
        {
            get
            {
                return _status;
            }
            private set
            {
                if (value != _status)
                {
                    var oldStatus = _status;
                    _status = value;
                    StatusEventOccurred?.Invoke(this, _status, oldStatus);
                }
            }
        }

        /// <summary>
        /// Creates an integrity scan
        /// </summary>
        /// <param name="dbPath">Where the tracking database is located</param>
        /// <param name="scansLocations">The locations to integrity scan</param>
        public IntegrityScan(string dbPath, params FilteredLocationItem[] scansLocations)
        {
            Status = ScanStatus.Ready;
            _scanMarker = DateTime.Now;
            _dh = new DataHandler(dbPath);
            _startLocations = new List<FilteredLocationItem>(scansLocations);
            _all = new ConcurrentBag<DiskItem>();
            _changes = new ConcurrentBag<DiskItem>();
        }

        /// <summary>
        /// Creates an integrity scan
        /// </summary>
        /// <param name="dh">A precreated data handler instance to use rather than create a new one</param>
        /// <param name="scansLocations">The locations to integrity scan</param>
        public IntegrityScan(DataHandler dh, IEnumerable<FilteredLocationItem> scansLocations)
        {
            Status = ScanStatus.Ready;
            _scanMarker = DateTime.Now;
            _dh = dh;
            _startLocations = new List<FilteredLocationItem>(scansLocations);
            _all = new ConcurrentBag<DiskItem>();
            _changes = new ConcurrentBag<DiskItem>();
        }

        /// <summary>
        /// Performs an integrity check on all items that match the given description
        /// </summary>
        /// <param name="maxThreads">The maximum number of threads to use for the scan</param>
        public void StartScan(int maxThreads)
        {
            try
            {
                Status = ScanStatus.InitiatingScan;
                Preparing?.Invoke(this, from p in _startLocations select p.Target);

                // first get all of the relevant items from the database
                var count = 0;
                var workDictionary = new Dictionary<FilteredLocationItem, IEnumerable<DiskItem>>();
                foreach (var loc in _startLocations)
                {
                    IEnumerable<DiskItem> workSet = null;
                    switch (loc.ExplorationMode)
                    {
                        case FilteredLocationExplorationMethod.In:
                            workSet = _dh.GetFilteredDiskItemsByIn(null, loc.Filter, new string[] { loc.Target });
                            break;
                        case FilteredLocationExplorationMethod.Under:
                            workSet = _dh.GetFilteredDiskItemsByUnder(null, loc.Filter, new string[] { loc.Target });
                            break;
                        case FilteredLocationExplorationMethod.Within:
                            workSet = _dh.GetFilteredDiskItemsByWithin(null, loc.Filter, new string[] { loc.Target });
                            break;
                    }

                    // Get all items within the given filter area,
                    // then only take files that match the additional filters on the FilteredLocationItem
                    var work = new ConcurrentBag<DiskItem>();
                    Parallel.ForEach(workSet, 
                        (di) =>
                        {
                            if (di.IsFile && loc.IsMatch(di.Path))
                            {
                                work.Add(di);
                            }
                        });

                    if (work.Count > 0)
                    {
                        workDictionary.Add(loc, work);
                        count += work.Count();
                    }
                }

                Status = ScanStatus.Scanning;
                ScanStarts?.Invoke(this, count);

                _all.Clear();
                _changes.Clear();
                // now loop through our work dictionary and perform an integrity scan on each item of each subset
                foreach (var loc in workDictionary.Keys)
                {
                    var tq = new ThreadedQueue<DiskItem>((di) => WorkerMethod(di),
                        1);
                    tq.Start(workDictionary[loc]);
                    tq.WaitAll();
                }

                _dh.UpdateHashes(_all);

                Status = ScanStatus.Ready;
                ScanEnds?.Invoke(this, _changes, _all);
            }
            catch (Exception ex)
            {
                Status = ScanStatus.Ready;
                UnknownErrorOccurred?.Invoke(this, ex);
            }
        }

        /// <summary>
        /// Calculates the SHA512 hash of the given disk item, adds it to the all bag upon completion,
        /// and adds it to the changed bag if it was modified since the last scan
        /// 
        /// This method is used as the worker in the ThreadedQueue.
        /// </summary>
        /// <param name="di">The file to calculate the hash</param>
        void WorkerMethod(DiskItem di)
        {
            try
            {
                using (FileStream fs = File.OpenRead(di.Path))
                {
                    using (HashAlgorithm hasher = SHA512.Create())
                    {

                        StartedScanningFile?.Invoke(this, di.Path, di.SizeInBytes, 0);

                        byte[] hash = hasher.ComputeHash(fs);
                        var sb = new StringBuilder();
                        foreach (var b in hash)
                        {
                            sb.Append($"{b:x2}".ToLower());
                        }

                        var hashStr = sb.ToString();
                        if (!string.IsNullOrWhiteSpace(di.FileHash) &&
                            hashStr != di.FileHash)
                        {
                            _changes.Add(di);
                        }

                        _all.Add(di);
                        di.HashTimestamp = _scanMarker;
                        di.FileHash = hashStr;

                        FinishedScanningFile?.Invoke(this, di.Path, di.SizeInBytes, di.SizeInBytes);
                    }
                }
            }
            catch (Exception ex)
            {
                FileScanFailed?.Invoke(this, di.Path, ex);
            }
        }
    }
}