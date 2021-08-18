using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections;
using System.IO;
using LiteDB;
using HDDL.IO.Disk;

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
        /// Initializes the database at the indicated path.
        /// Recreates it if it already exists
        /// </summary>
        public void InitializeDatabase(bool recreate = false)
        {
            if (recreate && File.Exists(StoragePath))
            {
                File.Delete(StoragePath);
            }

            using (var db = new LiteDatabase(StoragePath))
            {
                var records = db.GetCollection<DiskItemRecord>(TableName);
                records.EnsureIndex(r => r.Path, unique: true);
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
                scanMarker = DateTime.Now;
                var db = new LiteDatabase(StoragePath);

                scanningTasks.Clear();
                foreach (var driveLetter in info.TargetInformation.Keys)
                {
                    scanningTasks.Add(Task.Run(() =>
                    {
                        FullScan(info.TargetInformation[driveLetter], db);
                    }));
                }

                Task.WhenAll(scanningTasks).ContinueWith((t) =>
                {
                    var deleteCount = -1;
                    if (Status == ScanStatus.Scanning)
                    {
                        deleteCount = DeleteUnfoundEntries(db, startingPaths);
                    }
                    db.Dispose();
                    if (Status == ScanStatus.Scanning || Status == ScanStatus.Deleting)
                    {
                        Status = ScanStatus.Ready;
                        ScanEnded?.Invoke(this, deleteCount, ScanOperationOutcome.Completed);
                    }
                });
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
        /// <param name="db">the database</param>
        /// <returns>The total number of items deleted</returns>
        private int DeleteUnfoundEntries(LiteDatabase db, IEnumerable<string>  paths)
        {
            var totalDeletions = 0;
            if (Status == ScanStatus.Scanning)
            {
                Status = ScanStatus.Deleting;

                var records = db.GetCollection<DiskItemRecord>(TableName);
                // get all old records (weren't updated by the most recent scan)
                var old = records.Query()
                    .Where(r => r.LastScanned < scanMarker)
                    .ToArray();

                // We want to delete all records that are under the scanned path but were not updated (are no longer present)

                db.BeginTrans();
                foreach (var r in old)
                {
                    if (PathComparison.IsWithinPaths(r.Path, paths))
                    {
                        try
                        {
                            if (records.Delete(r.Id))
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
                db.Commit();
            }

            return totalDeletions;
        }

        /// <summary>
        /// Records each of the given paths
        /// </summary>
        /// <param name="scanTargets">The targets to scan</param>
        /// <param name="db">The database</param>
        private void FullScan(List<DiskItemType> scanTargets, LiteDatabase db)
        {
            if (Status == ScanStatus.Scanning)
            {
                db.BeginTrans();
                try
                {
                    foreach (var item in scanTargets)
                    {
                        if (item.IsFile)
                        {
                            WriteRecord(item.FInfo, db);
                        }
                        else
                        {
                            WriteRecord(item.DInfo, db);
                        }
                    }
                    db.Commit();
                }
                catch (LiteException ex)
                {
                    ScanEventOccurred?.Invoke(this, new ScanEvent(ScanEventType.DatabaseError, null, false, ex));
                    db.Rollback();
                }
                catch (Exception ex)
                {
                    ScanEventOccurred?.Invoke(this, new ScanEvent(ScanEventType.UnknownError, null, false, ex));
                    db.Rollback();
                }
            }
        }

        /// <summary>
        /// Scans the given path and stores all items (files and directories) found within in the database
        /// </summary>
        /// <param name="path">The path to scan</param>
        /// <param name="db">the database</param>
        //private void RecursiveFullScan(string path, LiteDatabase db)
        //{
        //    if (Status == ScanStatus.Scanning)
        //    {
        //        db.BeginTrans();
        //        try
        //        {
        //            if (File.Exists(path))
        //            {
        //                WriteRecord(new FileInfo(path), db);
        //            }
        //            else if (Directory.Exists(path))
        //            {
        //                var di = new DirectoryInfo(path);
        //                WriteRecord(di, db);

        //                var fullstructure = di.GetDirectories("*.*", SearchOption.AllDirectories);

        //                foreach (var d in fullstructure)
        //                {
        //                    WriteRecord(d, db);

        //                    foreach (var f in d.GetFiles())
        //                    {
        //                        WriteRecord(f, db);
        //                    }
        //                }
        //            }
        //            db.Commit();
        //        }
        //        catch (LiteException ex)
        //        {
        //            ScanEventOccurred?.Invoke(this, new ScanEvent(ScanEventType.DatabaseError, null, false, ex));
        //            db.Rollback();
        //        }
        //        catch (Exception ex)
        //        {
        //            ScanEventOccurred?.Invoke(this, new ScanEvent(ScanEventType.UnknownError, null, false, ex));
        //            db.Rollback();
        //        }
        //    }
        //}

        /// <summary>
        /// Writes the given file to the database
        /// </summary>
        /// <param name="file">The file to write</param>
        /// <param name="db">The database to write to</param>
        /// <returns>True upon successs, false upon failure</returns>
        private bool WriteRecord(FileInfo file, LiteDatabase db)
        {
            try
            {
                var records = db.GetCollection<DiskItemRecord>(TableName);
                var record = records.Query()
                    .Where(r => r.Path == file.FullName)
                    .SingleOrDefault();
                DiskItemRecord parent = null;
                if (file.Directory != null)
                {
                    parent = records.Query()
                        .Where(r => r.Path == file.Directory.FullName)
                        .SingleOrDefault();
                }

                if (record != null)
                {
                    try
                    {
                        record.LastScanned = scanMarker;
                        records.Update(record);
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
                        record = new DiskItemRecord()
                        {
                            Id = Guid.NewGuid(),
                            ParentItemId = parent?.Id,
                            FirstScanned = scanMarker,
                            LastScanned = scanMarker,
                            IsFile = true,
                            Path = file.FullName,
                            ItemName = file.Name,
                            Extension = file.Extension,
                            SizeInBytes = file.Length
                        };
                        records.Insert(record);
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
        /// <param name="db">The database to write to</param>
        /// <returns>True upon successs, false upon failure</returns>
        private bool WriteRecord(DirectoryInfo directory, LiteDatabase db)
        {
            try
            {
                var records = db.GetCollection<DiskItemRecord>(TableName);
                var record = records.Query()
                    .Where(r => r.Path == directory.FullName)
                    .SingleOrDefault();
                DiskItemRecord parent = null;
                if (directory.Parent != null)
                {
                    parent = records.Query()
                        .Where(r => r.Path == directory.Parent.FullName)
                        .SingleOrDefault();
                    if (parent == null)
                    {
                        // if we are writing a directory that is within a structure,
                        // this will write the entire path back to the root
                        WriteRecord(directory.Parent, db);
                        parent = records.Query()
                            .Where(r => r.Path == directory.Parent.FullName)
                            .SingleOrDefault();
                    }
                }

                if (record != null)
                {
                    try
                    {
                        record.LastScanned = scanMarker;
                        records.Update(record);
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
                        record = new DiskItemRecord()
                        {
                            Id = Guid.NewGuid(),
                            ParentItemId = parent?.Id,
                            FirstScanned = scanMarker,
                            LastScanned = scanMarker,
                            IsFile = false,
                            Path = directory.FullName,
                            ItemName = directory.Name,
                            Extension = null,
                            SizeInBytes = null
                        };
                        records.Insert(record);
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
    }
}
