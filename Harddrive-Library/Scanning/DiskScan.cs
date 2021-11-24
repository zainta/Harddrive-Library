// Copyright (c) Zain Al-Ahmary.  All rights reserved.
// Licensed under the MIT License, (the "License"); you may not use this file except in compliance with the License. 
// You may obtain a copy of the License at https://mit-license.org/

using HDDL.Data;
using HDDL.IO.Disk;
using HDDL.IO;
using HDDL.Threading;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace HDDL.Scanning
{
    /// <summary>
    /// Utility class that scans a machine and tracks all of the files' locations
    /// </summary>
    public class DiskScan
    {
        public delegate void ScanEventOccurredDelegate(DiskScan scanner, ScanEvent evnt);
        public delegate void ScanOperationStartedDelegate(DiskScan scanner, long directoryCount, long fileCount);
        public delegate void ScanOperationCompletedDelegate(DiskScan scanner, long totalDeleted, Timings elapsed, ScanOperationOutcome outcome);
        public delegate void ScanStatusEventDelegate(DiskScan scanner, ScanStatus newStatus, ScanStatus oldStatus);
        public delegate void ScanEventMassDatabaseActivityDelegate(DiskScan scanner, long additions, long updates, long deletions);
        public delegate void ScanDatabaseResetRequested(DiskScan scanner);
        public delegate void ScanEventDeletionsOccurred(DiskScan scanner, long total);
        public delegate void ScanDiskStructureExplorationBegins(DiskScan scanner);
        public delegate void ScanDiskStructureExplorationEnds(DiskScan scanner);
        public delegate void NoValidScanPathsProvided(DiskScan scanner);
        public delegate void PathHelperExploringLocation(DiskScan scanner, string path, bool isFile);

        /// <summary>
        /// Occurs when the status changes
        /// </summary>
        public event ScanStatusEventDelegate StatusEventOccurred;

        /// <summary>
        /// Occurs when an item (file or directory) is scanned
        /// </summary>
        public event ScanEventOccurredDelegate ScanEventOccurred;

        /// <summary>
        /// Occurs when a scan starts
        /// </summary>
        public event ScanOperationStartedDelegate ScanStarted;

        /// <summary>
        /// Occurs when a scan ends
        /// </summary>
        public event ScanOperationCompletedDelegate ScanEnded;

        /// <summary>
        /// Reports the outcome of the deletion phase
        /// </summary>
        public event ScanEventDeletionsOccurred DeletionsOccurred;

        /// <summary>
        /// Occurs after the database operations have completed
        /// </summary>
        public event ScanEventMassDatabaseActivityDelegate ScanDatabaseActivityCompleted;

        /// <summary>
        /// Occurs when the disk structure search begins
        /// </summary>
        public event ScanDiskStructureExplorationBegins ScanExplorationBegins;

        /// <summary>
        /// Occurs when the disk structure search ends
        /// </summary>
        public event ScanDiskStructureExplorationEnds ScanExplorationEnds;

        /// <summary>
        /// Occurs when either no scan paths are supplied or those that are do not exist
        /// </summary>
        public event NoValidScanPathsProvided NoValidScanPaths;

        /// <summary>
        /// Occurs when a location's exploration begins
        /// </summary>
        public event PathHelperExploringLocation ScanDiskExploring;

        /// <summary>
        /// Where to start scanning from
        /// </summary>
        private List<string> _startingPaths;

        /// <summary>
        /// The paths to be scanned.
        /// </summary>
        public IReadOnlyList<string> ScanTargets
        {
            get
            {
                return _startingPaths.AsReadOnly();
            }
        }

        /// <summary>
        /// Used as the "last scanned" timestamp for all items altered during the scan
        /// </summary>
        private DateTime _scanMarker;

        /// <summary>
        /// Handles all data reads and writes
        /// </summary>
        private DataHandler _dh;

        /// <summary>
        /// Used to cache directory ids for quick lookups
        /// </summary>
        private ConcurrentDictionary<string, Guid> _lookupTable;

        /// <summary>
        /// Contains paths that were written as a result of a requirement by another work item
        /// </summary>
        private List<string> _anchoredPaths;
        private readonly object _Anchored_Paths_Lock = new object();

        // These variables are used to track the duration of various parts of the process
        DateTime _scanStart, _directoryStructureScanStart, _directoryStructureProcessingStart, _databaseWriteStart;
        Timings _durations;

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
        /// The connection string used for the database
        /// </summary>
        public string ConnectionString
        {
            get
            {
                return _dh.ConnectionString;
            }
        }


        /// <summary>
        /// Create a disk scanner
        /// </summary>
        /// <param name="dbPath">Where the tracking database is located</param>
        /// <param name="scanPaths">The paths to start scans from</param>
        public DiskScan(string dbPath, params string[] scanPaths)
        {
            _scanMarker = DateTime.Now;
            _dh = new DataHandler(dbPath);
            _lookupTable = new ConcurrentDictionary<string, Guid>();
            _anchoredPaths = new List<string>();
            _durations = null;
            _startingPaths = new List<string>(PathHelper.EnsurePath(scanPaths));
        }

        /// <summary>
        /// Create a disk scanner
        /// </summary>
        /// <param name="dh">A precreated data handler instance to use rather than create a new one</param>
        /// <param name="scanPaths">The paths to start scans from</param>
        public DiskScan(DataHandler dh, IEnumerable<string> scanPaths)
        {
            _scanMarker = DateTime.Now;
            _dh = dh;
            _lookupTable = new ConcurrentDictionary<string, Guid>();
            _anchoredPaths = new List<string>();
            _durations = null;
            _startingPaths = new List<string>(PathHelper.EnsurePath(scanPaths));
        }

        /// <summary>
        /// Terminates a scan operation early
        /// </summary>
        public void InterruptScan()
        {
            if (Status == ScanStatus.Scanning || Status == ScanStatus.Deleting)
            {
                Status = ScanStatus.Interrupting;
                ScanEnded?.Invoke(this, -1, null, ScanOperationOutcome.Interrupted);
            }
        }

        /// <summary>
        /// Starts the scanning process
        /// </summary>
        public void StartScan()
        {
            if (_startingPaths.Count == 0)
            {
                NoValidScanPaths?.Invoke(this);
                return;
            }

            _durations = new Timings();
            PathSetData info = null;
            Status = ScanStatus.InitiatingScan;
            var task = Task.Run(() =>
            {
                _scanStart = DateTime.Now;
                _directoryStructureScanStart = DateTime.Now;

                // Apply bookmarks and ensure paths
                _startingPaths = (from p in _startingPaths select _dh.ApplyBookmarks(p)).ToList();

                PathHelper.ExploringLocation += PathHelper_ExploringLocation;
                ScanExplorationBegins?.Invoke(this);
                info = PathHelper.GetProcessedPathContents(_startingPaths, _dh.GetProcessedExclusions());
                ScanExplorationEnds?.Invoke(this);
                PathHelper.ExploringLocation -= PathHelper_ExploringLocation;

                _durations.DirectoryStructureScanDuration = DateTime.Now.Subtract(_directoryStructureScanStart);
            });
            Task.WhenAll(task).ContinueWith((t) =>
            {
                if (Status == ScanStatus.Ready || Status == ScanStatus.Interrupted || Status == ScanStatus.InitiatingScan)
                {
                    Status = ScanStatus.Scanning;
                    ScanStarted?.Invoke(this, info.TotalDirectories, info.TotalFiles);
                    _scanMarker = DateTime.Now;

                    // populate the queue
                    var dependencyOrderedQueues = new Queue<List<DiskItemType>>();
                    info.ProcessedContent.ForEach((x) =>
                    {
                        dependencyOrderedQueues.Enqueue(x);
                    });

                    // Define the threadqueue here because we need to refer to it
                    var queue = new ThreadedQueue<DiskItemType>((work) => WorkerMethod(work), 4);

                    // Perform the processing work
                    _directoryStructureProcessingStart = DateTime.Now;
                    while (dependencyOrderedQueues.Count > 0)
                    {
                        if (queue.Status == ThreadQueueStatus.Idle)
                        {
                            queue.Start(dependencyOrderedQueues.Dequeue());
                        }
                    }
                    queue.WaitAll();

                    _durations.DirectoryStructureProcessingDuration = DateTime.Now.Subtract(_directoryStructureProcessingStart);

                    // keep looking for records in the cache while:
                    // there are sets of work to perform or
                    // the threadqueue is running or
                    // there are records ready to write
                    _databaseWriteStart = DateTime.Now;

                    // Mass insert the new records
                    var outcomes = _dh.WriteDiskItems();
                    _durations.DatabaseWriteDuration = DateTime.Now.Subtract(_databaseWriteStart);
                    ScanDatabaseActivityCompleted?.Invoke(this, outcomes.Item1, outcomes.Item2, outcomes.Item3);

                    _durations.ScanDuration = DateTime.Now.Subtract(_scanStart);

                    // Remove old records (these will be files that were deleted)
                    DeleteUnfoundEntries(_startingPaths);

                    if (Status == ScanStatus.Scanning || Status == ScanStatus.Deleting)
                    {
                        Status = ScanStatus.Ready;
                        ScanEnded?.Invoke(this, -1, _durations, ScanOperationOutcome.Completed);
                    }
                }
            });
        }

        /// <summary>
        /// Takes in a list of DiskItemTypes and returns them in a queue
        /// </summary>
        /// <param name="diskItemTypes">The disk item types to transfer over</param>
        /// <returns></returns>
        private Queue<DiskItemType> GetQueue(List<DiskItemType> diskItemTypes)
        {
            Queue<DiskItemType> result = new Queue<DiskItemType>(diskItemTypes);
            return result;
        }

        /// <summary>
        /// The worker thread method used by the record scanner
        /// </summary>
        /// <param name="item"></param>
        private void WorkerMethod(DiskItemType item)
        {
            DiskItem record = null;
            try
            {
                var parentId = GetParentDirectoryId(item);
                var fullName = item.IsFile ? item.FInfo.FullName : item.DInfo.FullName;
                // see if the record exists
                record = _dh.GetDiskItemByPath(fullName);

                // if we found a record then we're doing an update
                var isInsert = record == null;
                if (record == null)
                {
                    // it doesn't
                    if (item.IsFile)
                    {
                        record = new DiskItem()
                        {
                            Id = Guid.NewGuid(),
                            ParentId = parentId,
                            FirstScanned = _scanMarker,
                            LastScanned = _scanMarker,
                            IsFile = true,
                            Path = item.FInfo.FullName,
                            ItemName = item.FInfo.Name,
                            Extension = item.FInfo.Extension,
                            Size = item.FInfo.Length,
                            LastAccessed = item.FInfo.LastAccessTimeUtc,
                            LastWritten = item.FInfo.LastWriteTimeUtc,
                            CreationDate = item.FInfo.CreationTimeUtc,
                            Depth = PathHelper.GetDependencyCount(new DiskItemType(item.FInfo.FullName, true)),
                            Attributes = item.FInfo.Attributes,
                            MachineUNCName = GetUNC(item.FInfo.FullName, item.IsFile)
                        };
                    }
                    else
                    {
                        record = new DiskItem()
                        {
                            Id = Guid.NewGuid(),
                            ParentId = parentId,
                            FirstScanned = _scanMarker,
                            LastScanned = _scanMarker,
                            IsFile = false,
                            Path = item.DInfo.FullName,
                            ItemName = item.DInfo.Name,
                            Extension = null,
                            Size = 0,
                            LastAccessed = item.DInfo.LastAccessTimeUtc,
                            LastWritten = item.DInfo.LastWriteTimeUtc,
                            CreationDate = item.DInfo.CreationTimeUtc,
                            Depth = PathHelper.GetDependencyCount(new DiskItemType(item.DInfo.FullName, false)),
                            Attributes = item.DInfo.Attributes,
                            MachineUNCName = GetUNC(item.DInfo.FullName, item.IsFile)
                        };
                    }
                }
                else
                {
                    // it does
                    // update the record to the new values
                    if (item.IsFile)
                    {
                        record.LastScanned = _scanMarker;
                        record.LastAccessed = item.FInfo.LastAccessTimeUtc;
                        record.LastWritten = item.FInfo.LastWriteTimeUtc;
                    }
                    else
                    {
                        record.LastScanned = _scanMarker;
                        record.LastAccessed = item.DInfo.LastAccessTimeUtc;
                        record.LastWritten = item.DInfo.LastWriteTimeUtc;
                    }
                }

                // Cache and update the lookup
                switch (isInsert)
                {
                    case true:
                        _dh.Insert(record);
                        break;
                    case false:
                        _dh.Update(record);
                        break;
                }
                if (!record.IsFile)
                {
                    AddRecordToLookup(record);
                }

                if (isInsert)
                {
                    ScanEventOccurred?.Invoke(this, new ScanEvent(ScanEventType.AddRequired, record.Path, record.IsFile));
                }
                else
                {
                    ScanEventOccurred?.Invoke(this, new ScanEvent(ScanEventType.UpdateRequired, record.Path, record.IsFile));
                }
            }
            catch (SQLiteException ex)
            {
                ScanEventOccurred?.Invoke(this, new ScanEvent(ScanEventType.DatabaseError, record?.Path, record == null ? false : record.IsFile, ex));
            }
            catch (Exception ex)
            {
                ScanEventOccurred?.Invoke(this, new ScanEvent(ScanEventType.UnknownError, record?.Path, record == null ? false : record.IsFile, ex));
            }
        }

        /// <summary>
        /// Gets the UNC path, if relevant to the operating system
        /// </summary>
        /// <param name="fullPath">The path to convert</param>
        /// <param name="isFile">Whether or not the target is a file</param>
        /// <returns>Either the UNC path, or the string "N/A"</returns>
        private string GetUNC(string fullPath, bool isFile)
        {
            var unc = "N/A";
            if (OS.IsWindows)
            {
                try
                {
                    unc = MappedDriveResolver.ResolveToRootUNC(fullPath);
                }
                catch (Exception ex)
                {
                    // any exception should be related to issues surrounding networking and/or the operating system
                    ScanEventOccurred?.Invoke(this, new ScanEvent(ScanEventType.UnknownError, fullPath, isFile, ex));
                }
            }
            return unc;
        }

        /// <summary>
        /// Returns the item's containing directory's id
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        private Guid? GetParentDirectoryId(DiskItemType item)
        {
            DirectoryInfo parent = null;
            if (item.IsFile)
            {
                parent = item.FInfo.Directory;
            }
            else
            {
                parent = item.DInfo.Parent;
            }

            if (parent != null)
            {
                if (_lookupTable.ContainsKey(parent.FullName))
                {
                    return _lookupTable[parent.FullName];
                }
                else
                {
                    var record = _dh.GetDiskItemByPath(parent.FullName);
                    if (record != null)
                    {
                        AddRecordToLookup(record);
                        return record.Id;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Ensures that a record is added to the lookup table
        /// </summary>
        /// <param name="record">The record to add</param>
        private void AddRecordToLookup(DiskItem record)
        {
            if (!_lookupTable.ContainsKey(record.Path))
            {
                var counter = 0;
                while (!_lookupTable.TryAdd(record.Path, record.Id))
                {
                    if (counter > 10)
                    {
                        break;
                    }
                    counter++;
                }
            }
        }

        /// <summary>
        /// Retrieves and deletes all items in the database with old last scanner field entries
        /// These items no longer exist
        /// </summary>
        /// <param name="paths">The paths originally searched</param>
        /// <returns>The total number of items deleted</returns>
        private long DeleteUnfoundEntries(IEnumerable<string> paths)
        {
            var count = _dh.DeleteOldDiskItems(_scanMarker, paths);
            if (count > 0)
            {
                DeletionsOccurred?.Invoke(this, count);
            }

            return count;
        }

        private void PathHelper_ExploringLocation(string path, bool isFile)
        {
            ScanDiskExploring?.Invoke(this, path, isFile);
        }
    }
}
