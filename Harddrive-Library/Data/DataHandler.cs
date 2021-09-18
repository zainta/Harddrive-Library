﻿using LiteDB;
using Microsoft.VisualBasic.CompilerServices;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HDDL.Data
{
    /// <summary>
    /// Provides data encapsulation to allow modification of systems without complete overhauls
    /// </summary>
    public class DataHandler : IDisposable
    {
        /// <summary>
        /// This is the table in the database where items discovered via scan are stored
        /// </summary>
        public const string DiskItemsTableName = "DiskItems";

        /// <summary>
        /// The connection string to the database
        /// </summary>
        private string _connectionString;

        /// <summary>
        /// Stores pending database actions for DiskItems
        /// </summary>
        private RecordActionContainer<DiskItem> _diskItems { get; set; }

        /// <summary>
        /// The current database connection
        /// </summary>
        private LiteDatabase _db;

        /// <summary>
        /// Creates a DataHandler
        /// </summary>
        /// <param name="connectionString">The connection string</param>
        public DataHandler(string connectionString)
        {
            _connectionString = connectionString;
            _diskItems = new RecordActionContainer<DiskItem>();
        }

        /// <summary>
        /// Initializes the database at the indicated path.
        /// Recreates it if it already exists
        /// </summary>
        /// <param name="recreate">If true, deletes and rebuilds the file database</param>
        /// <param name="connectionString">The connection string</param>
        public static void InitializeDatabase(string connectionString, bool recreate = false)
        {
            using (var db = new LiteDatabase(connectionString))
            {
                if (recreate && File.Exists(connectionString))
                {
                    var records = db.GetCollection<DiskItem>(DiskItemsTableName);
                    db.DropCollection(DiskItemsTableName);
                }
                else
                {
                    // Forces the creation of the table
                    var records = db.GetCollection<DiskItem>(DiskItemsTableName);
                }
            }
        }

        #region DiskItem Related

        /// <summary>
        /// Returns all disk items matching the filter and stored under any of the given paths
        /// </summary>
        /// <param name="filter">A wildcard string to filter files by</param>
        /// <param name="paths">The acceptable paths they can be within</param>
        /// <returns>The matching files</returns>
        public IEnumerable<DiskItem> GetFilteredDiskItemsByPath(string filter, IEnumerable<string> paths)
        {
            IEnumerable<DiskItem> results = null;
            var indexesOf = string.Join(" or ", (from tp in paths select $"indexof(Path, '{tp.Replace("\\", "\\\\")}') = 0").ToArray());
            var reader = EnsureConnection().Execute($"select $ from {DiskItemsTableName} where {indexesOf}");
            results =
                from rec in reader.ToList()
                where
                    LikeOperator.LikeString(rec["ItemName"], filter, Microsoft.VisualBasic.CompareMethod.Binary)
                select new DiskItem(rec);

            return results;
        }

        /// <summary>
        /// Attempts to retrieve and return a DiskItem by it's path
        /// </summary>
        /// <param name="path">The path to search for</param>
        /// <returns>The DiskItem if found, null otherwise</returns>
        public DiskItem GetRecordByPath(string path)
        {
            DiskItem result = null;
            var diTable = EnsureConnection().GetCollection<DiskItem>(DiskItemsTableName);
            result = diTable.Query()
                .Where(r => r.Path == path)
                .SingleOrDefault();

            return result;
        }

        /// <summary>
        /// Deletes all records located within one of the given paths with a LastScanned value prior to the timestamp
        /// </summary>
        /// <param name="timestamp">The cut off point for records' "old" status</param>
        /// <param name="paths">The disk paths where old files must reside</param>
        /// <returns>The number of records deleted</returns>
        public int DeleteOldDiskItems(DateTime timestamp, IEnumerable<string> paths)
        {
            var count = 0;
            EnsureConnection().BeginTrans();
            var diTable = _db.GetCollection<DiskItem>(DiskItemsTableName);

            // delete all old records (weren't updated by the most recent scan)
            count = diTable.DeleteMany(x => x.LastScanned < timestamp && paths.Where(p => x.Path.StartsWith(p)).Any());
            _db.Commit();

            return count;
        }

        /// <summary>
        /// Deletes all records
        /// </summary>
        /// <returns>The number of records deleted</returns>
        public int DeleteAllDiskItems()
        {
            var count = 0;
            var diTable = EnsureConnection().GetCollection<DiskItem>(DiskItemsTableName);

            // delete all old records (weren't updated by the most recent scan)
            count = diTable.DeleteAll();

            return count;
        }

        /// <summary>
        /// Queues the records for transactionary insertion
        /// </summary>
        /// <param name="items">The records to be added</param>
        /// <returns></returns>
        public void InsertDiskItems(params DiskItem[] items)
        {
            foreach (var item in items)
            {
                _diskItems.Inserts.Add(item);
            }
        }

        /// <summary>
        /// Queues the records for transactionary insertion
        /// </summary>
        /// <param name="items">The records to be added</param>
        /// <returns></returns>
        public void InsertDiskItems(IEnumerable<DiskItem> items)
        {
            foreach (var item in items)
            {
                _diskItems.Inserts.Add(item);
            }
        }

        /// <summary>
        /// Queues the records for the transactionary update
        /// </summary>
        /// <param name="items">The records to be updated</param>
        public void UpdateDiskItems(params DiskItem[] items)
        {
            foreach (var item in items)
            {
                _diskItems.Updates.Add(item);
            }
        }

        /// <summary>
        /// Queues the records for the transactionary update
        /// </summary>
        /// <param name="items">The records to be updated</param>
        public void UpdateDiskItems(IEnumerable<DiskItem> items)
        {
            foreach (var item in items)
            {
                _diskItems.Updates.Add(item);
            }
        }

        /// <summary>
        /// Queues the records for the transactionary deletion
        /// </summary>
        /// <param name="items"></param>
        public void DeleteDiskItems(params DiskItem[] items)
        {
            foreach (var item in items)
            {
                _diskItems.Deletions.Add(item);
            }
        }

        /// <summary>
        /// Queues the records for the transactionary deletion
        /// </summary>
        /// <param name="items"></param>
        public void DeleteDiskItems(IEnumerable<DiskItem> items)
        {
            foreach (var item in items)
            {
                _diskItems.Deletions.Add(item);
            }
        }

        /// <summary>
        /// Delets all Disk Items in the database
        /// </summary>
        public void ClearDiskItems()
        {
            var diTable = EnsureConnection().GetCollection<DiskItem>(DiskItemsTableName);
            diTable.DeleteAll();
        }

        /// <summary>
        /// Performs a transactionary database execution of all pending inserts and updates
        /// </summary>
        /// <returns>A tuple in the format total [inserts, updates, deletes]</returns>
        public Tuple<int, int, int> WriteDiskItems()
        {
            int inserts = 0, updates = 0, deletes = 0;
            if (_diskItems.HasWork)
            {
                EnsureConnection().BeginTrans();
                var diTable = _db.GetCollection<DiskItem>(DiskItemsTableName);

                if (_diskItems.Inserts.Count > 0)
                {
                    inserts = diTable.InsertBulk(_diskItems.Inserts);
                }

                if (_diskItems.Updates.Count > 0)
                {
                    foreach (var upd in _diskItems.Updates)
                    {
                        if (diTable.Update(upd))
                        {
                            updates++;
                        }
                    }
                }

                if (_diskItems.Deletions.Count > 0)
                {
                    deletes = diTable.DeleteMany(x => _diskItems.Deletions.Where(r => r.Id == x.Id).Any());
                }
                _db.Commit();
            }

            return new Tuple<int, int, int>(inserts, updates, deletes);
        }

        /// <summary>
        /// Ensures that _db contains a database
        /// </summary>
        /// <returns></returns>
        private LiteDatabase EnsureConnection()
        {
            if (_db == null)
            {
                _db = new LiteDatabase(_connectionString);
            }
            return _db;
        }

        public void Dispose()
        {
            if (_db != null)
            {
                _db.Dispose();
                _db = null;
            }
        }

        #endregion
    }
}
