// Copyright (c) Zain Al-Ahmary.  All rights reserved.
// Licensed under the MIT License, (the "License"); you may not use this file except in compliance with the License. 
// You may obtain a copy of the License at https://mit-license.org/

using HDDL.Data;
using HDDL.IO.Disk;
using HDDL.Threading;
using LiteDB;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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
        public delegate void ScanOperationStartedDelegate(DiskScan scanner, int directoryCount, int fileCount);
        public delegate void ScanOperationCompletedDelegate(DiskScan scanner, int totalDeleted, Timings elapsed, ScanOperationOutcome outcome);
        public delegate void ScanStatusEventDelegate(DiskScan scanner, ScanStatus newStatus, ScanStatus oldStatus);
        public delegate void ScanEventMassDatabaseActivityDelegate(DiskScan scanner, int additions, int updates, int deletions);
        public delegate void ScanDatabaseResetRequested(DiskScan scanner);
        public delegate void ScanEventDeletionsOccurred(DiskScan scanner, int total);

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
            _startingPaths = new List<string>(scanPaths);
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
            _startingPaths = new List<string>(scanPaths);
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
            _durations = new Timings();
            PathSetData info = null;
            Status = ScanStatus.InitiatingScan;
            var task = Task.Run(() =>
            {
                _scanStart = DateTime.Now;
                _directoryStructureScanStart = DateTime.Now;

                _startingPaths = (from p in _startingPaths select _dh.ApplyBookmarks(p)).ToList();

                info = PathHelper.GetContentsSortedByRoot(_startingPaths);
            });
            Task.WhenAll(task).ContinueWith((t) =>
            {
                if (Status == ScanStatus.Ready || Status == ScanStatus.Interrupted || Status == ScanStatus.InitiatingScan)
                {
                    Status = ScanStatus.Scanning;
                    ScanStarted?.Invoke(this, info.TotalDirectories, info.TotalFiles);
                    _scanMarker = DateTime.UtcNow;

                    // The below processing allows us to run through the work items
                    // without having to perform any checks around existence
                    // (other to see if we are updating vs inserting)

                    // We have to process the results into a single queue because they come in sorted by root drive in a dictionary
                    // first merge them into sets of files and directories
                    var allFiles = new List<DiskItemType>();
                    var allDirectories = new List<DiskItemType>();
                    foreach (var root in info.TargetInformation.Keys)
                    {
                        allFiles.AddRange(from f in info.TargetInformation[root] where f.IsFile select f);
                        allDirectories.AddRange(from d in info.TargetInformation[root] orderby PathHelper.GetDependencyCount(d) ascending where !d.IsFile select d);
                    }

                    // then group the items by and sort the groups by their distance from the root (called Dependency Count)
                    var sortedFiles =
                        (from work in allFiles
                         group work by PathHelper.GetDependencyCount(work) into dependyLevel
                         orderby dependyLevel.Key
                         select dependyLevel.ToList()).ToList();

                    var sortedDirectories =
                        (from work in allDirectories
                         group work by PathHelper.GetDependencyCount(work) into dependyLevel
                         orderby dependyLevel.Key
                         select dependyLevel.ToList()).ToList();

                    // now that we have everything sorted and ready to go, merge the groups with the directories first
                    var sorted = new List<List<DiskItemType>>();
                    sorted.AddRange(sortedDirectories);
                    sorted.AddRange(sortedFiles);
                    _durations.DirectoryStructureScanDuration = DateTime.Now.Subtract(_directoryStructureScanStart);

                    // reclaim the memory from this...
                    sortedFiles = null;
                    sortedDirectories = null;

                    // populate the queue
                    var dependencyOrderedQueues = new Queue<List<DiskItemType>>();
                    sorted.ForEach((x) =>
                    {
                        dependencyOrderedQueues.Enqueue(x);
                    });

                    // Define the threadqueue here because we need to refer to it
                    var queue = new ThreadedQueue<DiskItemType>((work) => WorkerMethod(work), 1);

                    // Perform the processing work
                    _directoryStructureProcessingStart = DateTime.Now;
                    while (dependencyOrderedQueues.Count > 0)
                    {
                        if (queue.Status == ThreadQueueStatus.Idle)
                        {
                            queue.Start(dependencyOrderedQueues.Dequeue());
                        }
                    }

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
                record = _dh.GetRecordByPath(fullName);

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
                            SizeInBytes = item.FInfo.Length,
                            LastAccessed = item.FInfo.LastAccessTimeUtc,
                            LastWritten = item.FInfo.LastWriteTimeUtc,
                            CreationDate = item.FInfo.CreationTimeUtc
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
                            SizeInBytes = null,
                            LastAccessed = item.DInfo.LastAccessTimeUtc,
                            LastWritten = item.DInfo.LastWriteTimeUtc,
                            CreationDate = item.DInfo.CreationTimeUtc
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
                        _dh.InsertDiskItems(record);
                        break;
                    case false:
                        _dh.UpdateDiskItems(record);
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
            catch (LiteException ex)
            {
                ScanEventOccurred?.Invoke(this, new ScanEvent(ScanEventType.DatabaseError, record?.Path, record == null ? false : record.IsFile, ex));
            }
            catch (Exception ex)
            {
                ScanEventOccurred?.Invoke(this, new ScanEvent(ScanEventType.UnknownError, record?.Path, record == null ? false : record.IsFile, ex));
            }
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
                return _lookupTable[parent.FullName];
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
        private int DeleteUnfoundEntries(IEnumerable<string> paths)
        {
            var count = _dh.DeleteOldDiskItems(_scanMarker, paths);
            if (count > 0)
            {
                DeletionsOccurred?.Invoke(this, count);
            }

            return count;
        }
    }
}
