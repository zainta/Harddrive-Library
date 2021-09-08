using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections;
using System.IO;
using HDDL.IO.Disk;
using HDDL.Data;
using LiteDB;
using System.Collections.Concurrent;
using System.Threading;
using HDDL.Collections;
using HDDL.Threading;

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
        public delegate void ScanEventMassAdditionDelegate(DiskScan scanner, int additions);
        public delegate void ScanDatabaseResetRequested(DiskScan scanner);

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
        /// Occurs after the bulk insert operation for new records
        /// </summary>
        public event ScanEventMassAdditionDelegate ScanInsertsCompleted;

        /// <summary>
        /// Occurs when the database initialization is set to recreate the database.
        /// </summary>
        public event ScanDatabaseResetRequested DatabaseResetRequested;

        /// <summary>
        /// This is the table in the database where items discovered via scan are stored
        /// </summary>
        public const string TableName = "DiskItems";

        /// <summary>
        /// Where to start scanning from
        /// </summary>
        private List<string> startingPaths;

        /// <summary>
        /// The list of scanning tasks
        /// </summary>
        private List<Task> scanningTasks;

        /// <summary>
        /// Used as the "last scanned" timestamp for all items altered during the scan
        /// </summary>
        private DateTime _scanMarker;

        /// <summary>
        /// The database instance in use
        /// </summary>
        private LiteDatabase _db;

        /// <summary>
        /// The DiskItem table
        /// </summary>
        private ILiteCollection<DiskItem> _table;

        /// <summary>
        /// Used to store record batches between writes
        /// </summary>
        private ConcurrentBag<DiskItemOperation> _recordCache;

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

        /// <summary>
        /// The location of the database file
        /// </summary>
        public string StoragePath { get; private set; }

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
        /// Create a disk scanner
        /// </summary>
        /// <param name="dbPath">Where the tracking database is located</param>
        /// <param name="scanPaths">The paths to start scans from</param>
        public DiskScan(string dbPath, params string[] scanPaths)
        {
            startingPaths = new List<string>(scanPaths);
            _scanMarker = DateTime.Now;
            StoragePath = dbPath;
            scanningTasks = new List<Task>();
            _lookupTable = new ConcurrentDictionary<string, Guid>();
            _recordCache = new ConcurrentBag<DiskItemOperation>();
            _anchoredPaths = new List<string>();
            _durations = null;
            _db = null;
        }

        /// <summary>
        /// Initializes the database at the indicated path.
        /// Recreates it if it already exists
        /// </summary>
        /// <param name="recreate">If true, deletes and rebuilds the file database</param>
        public void InitializeDatabase(bool recreate = false)
        {
            using (var db = new LiteDatabase(StoragePath))
            {
                if (recreate && File.Exists(StoragePath))
                {
                    DatabaseResetRequested?.Invoke(this);
                    var records = db.GetCollection<DiskItem>(TableName);
                    db.DropCollection(TableName);
                }
                else
                {
                    // Forces the creation of the table
                    var records = db.GetCollection<DiskItem>(TableName);
                }
            }
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
                info = PathHelper.GetContentsSortedByRoot(startingPaths);
            });
            Task.WhenAll(task).ContinueWith((t) =>
            {
                if (Status == ScanStatus.Ready || Status == ScanStatus.Interrupted || Status == ScanStatus.InitiatingScan)
                {
                    Status = ScanStatus.Scanning;
                    ScanStarted?.Invoke(this, info.TotalDirectories, info.TotalFiles);
                    _scanMarker = DateTime.UtcNow;
                    OpenConnection();

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
                    var queue = new ThreadedQueue<DiskItemType>((work) => WorkerMethod(work), 2);

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

                    _db.BeginTrans();
                    // start the database writer task
                    var writeTask =
                        Task.Run(() =>
                        {
                            // keep looking for records in the cache while:
                            // there are sets of work to perform or
                            // the threadqueue is running or
                            // there are records ready to write
                            _databaseWriteStart = DateTime.Now;

                            // Mass insert the new records
                            var inserts = from i in _recordCache where i.IsInsert select i.Item;
                            _table.InsertBulk(inserts);
                            ScanInsertsCompleted?.Invoke(this, inserts.Count());

                            // Handle the updates
                            var updates = from u in _recordCache where !u.IsInsert select u.Item;
                            foreach (var item in updates)
                            {
                                try
                                {
                                    _table.Update(item);
                                    ScanEventOccurred?.Invoke(this, new ScanEvent(ScanEventType.Update, item.Path, item.IsFile));
                                }
                                catch (Exception ex)
                                {
                                    ScanEventOccurred?.Invoke(this, new ScanEvent(ScanEventType.DatabaseError, null, false, ex));
                                }
                            }
                        }).ContinueWith((tsk) =>
                        {
                            _db.Commit();
                            _db.Dispose();
                            _durations.DatabaseWriteDuration = DateTime.Now.Subtract(_databaseWriteStart);

                            _durations.ScanDuration = DateTime.Now.Subtract(_scanStart);

                            if (Status == ScanStatus.Scanning || Status == ScanStatus.Deleting)
                            {
                                Status = ScanStatus.Ready;
                                ScanEnded?.Invoke(this, -1, _durations, ScanOperationOutcome.Completed);
                            }
                        });
                    writeTask.Wait();
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
                record = _table.Query()
                    .Where(r => r.Path == fullName)
                    .SingleOrDefault();

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
                _recordCache.Add(
                    new DiskItemOperation()
                    {
                        Item = record,
                        IsInsert = isInsert
                    });
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
        /// Opens the connection
        /// </summary>
        private void OpenConnection()
        {
            _db = new LiteDatabase(StoragePath);
            _table = _db.GetCollection<DiskItem>(TableName);
        }

        /// <summary>
        /// Retrieves and deletes all items in the database with old last scanner field entries
        /// These items no longer exist
        /// </summary>
        /// <param name="paths">The paths originally searched</param>
        /// <returns>The total number of items deleted</returns>
        private int DeleteUnfoundEntries(IEnumerable<string> paths)
        {
            var totalDeletions = 0;
            if (Status == ScanStatus.Scanning)
            {
                Status = ScanStatus.Deleting;

                var records = _db.GetCollection<DiskItem>(TableName);
                // get all old records (weren't updated by the most recent scan)
                var old = records.Query()
                    .Where(r => r.LastScanned < _scanMarker)
                    .ToArray();

                _db.BeginTrans();
                // We want to delete all records that are under the scanned path but were not updated (are no longer present)
                foreach (var r in old)
                {
                    if (PathHelper.IsWithinPaths(r.Path, paths))
                    {
                        try
                        {
                            if (records.Delete(r.Id))
                            {
                                totalDeletions++;
                                ScanEventOccurred?.Invoke(this, new ScanEvent(ScanEventType.Delete, r.Path, r.IsFile));
                            }
                        }
                        catch (Exception ex)
                        {
                            ScanEventOccurred?.Invoke(this, new ScanEvent(ScanEventType.DeleteAttempted, r.Path, r.IsFile, ex));
                        }
                    }
                }
                _db.Commit();
            }

            return totalDeletions;
        }
    }
}
