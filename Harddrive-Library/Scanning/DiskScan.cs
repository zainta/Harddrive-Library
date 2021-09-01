using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections;
using System.IO;
using HDDL.IO.Disk;
using HDDL.Data;
using Microsoft.Data.Sqlite;

namespace HDDL.Scanning
{
    /// <summary>
    /// Utility class that scans a machine and tracks all of the files' locations
    /// </summary>
    public class DiskScan
    {
        public delegate void ScanEventOccurredDelegate(DiskScan scanner, ScanEvent evnt);
        public delegate void ScanOperationStartedDelegate(DiskScan scanner, int directoryCount, int fileCount);
        public delegate void ScanOperationCompletedDelegate(DiskScan scanner, int totalDeleted, ScanOperationOutcome outcome);
        public delegate void ScanStatusEventDelegate(DiskScan scanner, ScanStatus newStatus, ScanStatus oldStatus);

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
        private DateTime scanMarker;

        /// <summary>
        /// The database instance in use
        /// </summary>
        private HDDLDataContext _db;
        
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
            scanMarker = DateTime.Now;
            StoragePath = dbPath;
            scanningTasks = new List<Task>();

            InitializeDatabase();
        }

        /// <summary>
        /// Create a disk scanner
        /// </summary>
        /// <param name="dbPath">Where the tracking database is located</param>
        /// <param name="scanPaths">The paths to start scans from</param>
        /// <param name="recreate">If true, deletes and rebuilds the file database</param>
        public DiskScan(string dbPath, bool recreate, params string[] scanPaths)
        {
            startingPaths = new List<string>(scanPaths);
            scanMarker = DateTime.Now;
            StoragePath = dbPath;
            scanningTasks = new List<Task>();

            InitializeDatabase(recreate);
        }

        /// <summary>
        /// Initializes the database at the indicated path.
        /// Recreates it if it already exists
        /// </summary>
        /// <param name="recreate">If true, deletes and rebuilds the file database</param>
        public void InitializeDatabase(bool recreate = false)
        {
            if (recreate && File.Exists(StoragePath))
            {
                File.Delete(StoragePath);
            }

            HDDLDataContext.EnsureDatabase(StoragePath);
        }

        /// <summary>
        /// Terminates a scan operation early
        /// </summary>
        public void InterruptScan()
        {
            if (Status == ScanStatus.Scanning || Status == ScanStatus.Deleting)
            {
                Status = ScanStatus.Interrupting;
                ScanEnded?.Invoke(this, -1, ScanOperationOutcome.Interrupted);
            }
        }

        /// <summary>
        /// Starts the scanning process
        /// </summary>
        /// <param name="info">Information on the full structure of the target</param>
        private void StartScan(PathSetData info)
        {
            if (Status == ScanStatus.Ready || Status == ScanStatus.Interrupted || Status == ScanStatus.InitiatingScan)
            {
                Status = ScanStatus.Scanning;
                ScanStarted?.Invoke(this, info.TotalDirectories, info.TotalFiles);
                scanMarker = DateTime.UtcNow;
                _db = new HDDLDataContext(StoragePath);

                scanningTasks.Clear();
                //foreach (var driveLetter in info.TargetInformation.Keys)
                //{
                //    scanningTasks.Add(Task.Run(() =>
                //    {
                //        return FullScan(info.TargetInformation[driveLetter]);
                //    }));
                //}
                using (var dbTransaction = _db.Database.BeginTransaction())
                {
                    foreach (var driveLetter in info.TargetInformation.Keys)
                    {
                        FullScan(info.TargetInformation[driveLetter]);
                    }


                    _db.SaveChanges();
                    dbTransaction.Commit();
                }

                //Task.WhenAll(scanningTasks).ContinueWith((t) =>
                //{
                //    var deleteCount = -1;
                //    if (Status == ScanStatus.Scanning)
                //    {
                //        deleteCount = DeleteUnfoundEntries(startingPaths);
                //    }
                //    _db.Dispose();
                //    if (Status == ScanStatus.Scanning || Status == ScanStatus.Deleting)
                //    {
                //        Status = ScanStatus.Ready;
                //        ScanEnded?.Invoke(this, deleteCount, ScanOperationOutcome.Completed);
                //    }
                //});
                var deleteCount = -1;
                if (Status == ScanStatus.Scanning)
                {
                    deleteCount = DeleteUnfoundEntries(startingPaths);
                }
                _db.Dispose();
                if (Status == ScanStatus.Scanning || Status == ScanStatus.Deleting)
                {
                    Status = ScanStatus.Ready;
                    ScanEnded?.Invoke(this, deleteCount, ScanOperationOutcome.Completed);
                }
            }
        }

        /// <summary>
        /// Starts the scanning process
        /// </summary>
        public void StartScan()
        {
            PathSetData info = null;
            Status = ScanStatus.InitiatingScan;
            var task = Task.Run(() =>
                {
                    info = PathComparison.GetContentsSortedByRoot(startingPaths);
                });
            Task.WhenAll(task).ContinueWith((t) =>
                {
                    StartScan(info);
                });
        }

        /// <summary>
        /// Retrieves and deletes all items in the database with old last scanner field entries
        /// These items no longer exist
        /// </summary>
        /// <param name="paths">The paths originally searched</param>
        /// <returns>The total number of items deleted</returns>
        private int DeleteUnfoundEntries(IEnumerable<string>  paths)
        {
            var totalDeletions = 0;
            if (Status == ScanStatus.Scanning)
            {
                Status = ScanStatus.Deleting;

                // get all old records (weren't updated by the most recent scan)
                var old = _db.DiskItems.Where(r => r.LastScanned < scanMarker).ToArray();

                // We want to delete all records that are under the scanned path but were not updated (are no longer present)
                foreach (var r in old)
                {
                    if (PathComparison.IsWithinPaths(r.Path, paths))
                    {
                        try
                        {
                            _db.Remove(r);
                            if (_db.SaveChanges() > 0)
                            {
                                totalDeletions++;
                                ScanEventOccurred?.Invoke(this, new ScanEvent(ScanEventType.Delete, r.Path, r.IsFile));
                            }
                            else
                            {
                                ScanEventOccurred?.Invoke(this, new ScanEvent(ScanEventType.KeyNotDeleted, r.Path, r.IsFile, null));
                            }
                        }
                        catch (Exception ex)
                        {
                            ScanEventOccurred?.Invoke(this, new ScanEvent(ScanEventType.DeleteAttempted, r.Path, r.IsFile, ex));
                        }
                    }
                }
            }

            return totalDeletions;
        }

        /// <summary>
        /// Records each of the given paths
        /// </summary>
        /// <param name="scanTargets">The targets to scan</param>
        /// <returns>True if scan was successful, false otherwise</returns>
        private bool FullScan(List<DiskItemType> scanTargets)
        {
            if (Status == ScanStatus.Scanning)
            {
                try
                {
                    foreach (var item in scanTargets)
                    {
                        if (item.IsFile)
                        {
                            if (!WriteRecord(item.FInfo))
                            {
                                ReboundDatabase();
                                return false;
                            }
                        }
                        else
                        {
                            if (!WriteRecord(item.DInfo))
                            {
                                ReboundDatabase();
                                return false;
                            }
                        }
                    }
                }
                catch (SqliteException ex)
                {
                    ScanEventOccurred?.Invoke(this, new ScanEvent(ScanEventType.DatabaseError, null, false, ex));
                    ReboundDatabase();
                    return false;
                }
                catch (Exception ex)
                {
                    ScanEventOccurred?.Invoke(this, new ScanEvent(ScanEventType.UnknownError, null, false, ex));
                    ReboundDatabase();
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Disposes and reinstantiates the database context to cancel all changes
        /// </summary>
        private void ReboundDatabase()
        {
            _db.Dispose();
            _db = new HDDLDataContext(StoragePath);
        }

        /// <summary>
        /// Writes the given file to the database
        /// </summary>
        /// <param name="file">The file to write</param>
        /// <returns>True upon successs, false upon failure</returns>
        private bool WriteRecord(FileInfo file)
        {
            try
            {
                var record = GetByPath(file.FullName);
                DiskItem parent = null;
                if (file.Directory != null)
                {
                    parent = GetByPath(file.Directory.FullName);

                    if (parent == null)
                    {
                        // if this file's parent folder is not already in the system
                        // then this will create records for the entire path all the way to the root
                        WriteRecord(file.Directory);
                        parent = GetByPath(file.Directory.FullName);
                    }
                }

                if (record != null)
                {
                    try
                    {
                        record.LastScanned = scanMarker;
                        _db.DiskItems.Update(record);
                        ScanEventOccurred?.Invoke(this, new ScanEvent(ScanEventType.Update, file.FullName, true));
                    }
                    catch (Exception ex)
                    {
                        ScanEventOccurred?.Invoke(this, new ScanEvent(ScanEventType.UpdateAttempted, file.FullName, true, ex));
                        return false;
                    }
                }
                else
                {
                    try
                    {
                        record = new DiskItem()
                        {
                            Id = Guid.NewGuid(),
                            ParentId = parent?.Id,
                            FirstScanned = scanMarker,
                            LastScanned = scanMarker,
                            IsFile = true,
                            Path = file.FullName,
                            ItemName = file.Name,
                            Extension = file.Extension,
                            SizeInBytes = file.Length,
                            LastAccessed = file.LastAccessTimeUtc,
                            LastWritten = file.LastWriteTimeUtc,
                            CreationDate = file.CreationTimeUtc
                        };                        
                        _db.DiskItems.Add(record);
                        ScanEventOccurred?.Invoke(this, new ScanEvent(ScanEventType.Add, file.FullName, true));
                    }
                    catch (Exception ex)
                    {
                        ScanEventOccurred?.Invoke(this, new ScanEvent(ScanEventType.AddAttempted, file.FullName, true, ex));
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                ScanEventOccurred?.Invoke(this, new ScanEvent(ScanEventType.UnknownError, file.FullName, true, ex));
                return false;
            }

            return true;
        }

        /// <summary>
        /// Writes the given directory to the database
        /// </summary>
        /// <param name="directory">The directory to write</param>
        /// <returns>True upon successs, false upon failure</returns>
        private bool WriteRecord(DirectoryInfo directory)
        {
            try
            {
                var record = GetByPath(directory.FullName);
                DiskItem parent = null;
                if (directory.Parent != null)
                {
                    parent = GetByPath(directory.Parent.FullName);
                    if (parent == null)
                    {
                        // if we are writing a directory that is within a structure,
                        // this will write the entire path back to the root
                        WriteRecord(directory.Parent);
                        parent = GetByPath(directory.Parent.FullName);
                    }
                }

                if (record != null)
                {
                    try
                    {
                        record.LastScanned = scanMarker;
                        _db.DiskItems.Update(record);
                        ScanEventOccurred?.Invoke(this, new ScanEvent(ScanEventType.Update, directory.FullName, false));
                    }
                    catch (Exception ex)
                    {
                        ScanEventOccurred?.Invoke(this, new ScanEvent(ScanEventType.UpdateAttempted, directory.FullName, false, ex));
                        return false;
                    }
                }
                else
                {
                    try
                    {
                        record = new DiskItem()
                        {
                            Id = Guid.NewGuid(),
                            ParentId = parent?.Id,
                            FirstScanned = scanMarker,
                            LastScanned = scanMarker,
                            IsFile = false,
                            Path = directory.FullName,
                            ItemName = directory.Name,
                            Extension = null,
                            SizeInBytes = null,
                            LastAccessed = directory.LastAccessTimeUtc,
                            LastWritten = directory.LastWriteTimeUtc,
                            CreationDate = directory.CreationTimeUtc
                        };
                        _db.DiskItems.Add(record);
                        ScanEventOccurred?.Invoke(this, new ScanEvent(ScanEventType.Add, directory.FullName, false));
                    }
                    catch (Exception ex)
                    {
                        ScanEventOccurred?.Invoke(this, new ScanEvent(ScanEventType.AddAttempted, directory.FullName, false, ex));
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                ScanEventOccurred?.Invoke(this, new ScanEvent(ScanEventType.UnknownError, directory.FullName, false, ex));
                return false;
            }

            return true;
        }

        /// <summary>
        /// Searches the database and then the local cache for an item with the given path
        /// </summary>
        /// <param name="path">The path to search for</param>
        /// <returns>The DiskItem if found, otherwise null</returns>
        private DiskItem GetByPath(string path)
        {
            var record = _db.DiskItems
                    .Where(r => r.Path == path)
                    .SingleOrDefault();

            if (record == null)
            {
                record = _db.DiskItems.Local
                    .Where(r => r.Path == path)
                    .SingleOrDefault();
            }

            return record;
        }
    }
}
