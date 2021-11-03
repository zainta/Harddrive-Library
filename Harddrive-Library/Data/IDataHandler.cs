// Copyright (c) Zain Al-Ahmary.  All rights reserved.
// Licensed under the MIT License, (the "License"); you may not use this file except in compliance with the License. 
// You may obtain a copy of the License at https://mit-license.org/

using HDDL.IO.Disk;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Text;

namespace HDDL.Data
{
    /// <summary>
    /// Describes all methods available through the DataHandler class
    /// </summary>
    public interface IDataHandler
    {
        /// <summary>
        /// The connection string to the database
        /// </summary>
        public string ConnectionString { get; }

        #region DiskItemHashLog Related

        /// <summary>
        /// Deletes all DiskitemHashLogs from the database
        /// </summary>
        public long ClearDiskItemHashLogs();

        /// <summary>
        /// Queues the records for transactionary insertion
        /// </summary>
        /// <param name="items">The records to be added</param>
        /// <returns></returns>
        public void Insert(params DiskItemHashLogItem[] items);

        /// <summary>
        /// Queues the records for the transactionary update
        /// </summary>
        /// <param name="items">The records to be updated</param>
        public void Update(params DiskItemHashLogItem[] items);

        /// <summary>
        /// Queues the records for the transactionary deletion
        /// </summary>
        /// <param name="items"></param>
        public void Delete(params DiskItemHashLogItem[] items);

        /// <summary>
        /// Performs a transactionary database execution of all pending inserts, updates, and deletes
        /// </summary>
        /// <returns>A tuple in the format total [inserts, updates, deletes]</returns>
        public Tuple<long, long, long> WriteDiskItemHashLogs();

        #endregion

        #region Watch Related

        /// <summary>
        /// Retrieves and returns the watches
        /// </summary>
        /// <returns></returns>
        public List<WatchItem> GetWatches();

        /// <summary>
        /// Clears any cached bookmarks, forcing them to be reloaded for the next GetBookmarks() call
        /// </summary>
        public void ClearWatchCache();

        /// <summary>
        /// Deletes all watches from the database
        /// </summary>
        public long ClearWatches();

        /// <summary>
        /// Queues the records for transactionary insertion
        /// </summary>
        /// <param name="items">The records to be added</param>
        /// <returns></returns>
        public void Insert(params WatchItem[] items);

        /// <summary>
        /// Queues the records for transactionary insertion
        /// </summary>
        /// <param name="items">The records to be added</param>
        /// <returns></returns>
        public void Insert(params string[] items);

        /// <summary>
        /// Queues the records for the transactionary update
        /// </summary>
        /// <param name="items">The records to be updated</param>
        public void Update(params WatchItem[] items);

        /// <summary>
        /// Queues the record(s) for a reset (having its passive mode turned off to force a fresh disk scan)
        /// </summary>
        /// <param name="items">The records to be updated</param>
        public void Reset(params WatchItem[] items);

        /// <summary>
        /// Queues the record(s) for a reset (having its passive mode turned off to force a fresh disk scan)
        /// </summary>
        /// <param name="items">The records to be updated</param>
        public void Reset(params string[] items);

        /// <summary>
        /// Queues the records for the transactionary deletion
        /// </summary>
        /// <param name="items"></param>
        public void Delete(params WatchItem[] items);

        /// <summary>
        /// Performs a transactionary database execution of all pending inserts, updates, and deletes
        /// </summary>
        /// <returns>A tuple in the format total [inserts, updates, deletes]</returns>
        public Tuple<long, long, long> WriteWatches();

        #endregion

        #region Ward Related

        /// <summary>
        /// Retrieves and returns the wards
        /// </summary>
        /// <returns></returns>
        public List<WardItem> GetWards();

        /// <summary>
        /// Clears any cached wards, forcing them to be reloaded for the next GetBookmarks() call
        /// </summary>
        public void ClearWardCache();

        /// <summary>
        /// Deletes all watches from the database
        /// </summary>
        public long ClearWards();

        /// <summary>
        /// Queues the records for transactionary insertion
        /// </summary>
        /// <param name="items">The records to be added</param>
        /// <returns></returns>
        public void Insert(params WardItem[] items);

        /// <summary>
        /// Queues the records for the transactionary update
        /// </summary>
        /// <param name="items">The records to be updated</param>
        public void Update(params WardItem[] items);

        /// <summary>
        /// Queues the records for the transactionary deletion
        /// </summary>
        /// <param name="items"></param>
        public void Delete(params WardItem[] items);

        /// <summary>
        /// Performs a transactionary database execution of all pending inserts, updates, and deletes
        /// </summary>
        /// <returns>A tuple in the format total [inserts, updates, deletes]</returns>
        public Tuple<long, long, long> WriteWards();

        #endregion

        #region Exclusion Related

        /// <summary>
        /// Retrieves and returns the bookmarks
        /// </summary>
        /// <returns></returns>
        public List<ExclusionItem> GetExclusions();

        /// <summary>
        /// Retrieves and returns all Exclusions and expands dynamic exclusions (removing dead ones from the list)
        /// </summary>
        /// <returns></returns>
        public List<ExclusionItem> GetProcessedExclusions();

        /// <summary>
        /// Clears any cached bookmarks, forcing them to be reloaded for the next GetBookmarks() call
        /// </summary>
        public void ClearExclusionCache();

        /// <summary>
        /// Deletes all bookmarks from the database
        /// </summary>
        public long ClearExclusions();

        /// <summary>
        /// Queues the records for transactionary insertion
        /// </summary>
        /// <param name="items">The records to be added</param>
        /// <returns></returns>
        public void Insert(params ExclusionItem[] items);

        /// <summary>
        /// Queues the records for the transactionary update
        /// </summary>
        /// <param name="items">The records to be updated</param>
        public void Update(params ExclusionItem[] items);

        /// <summary>
        /// Queues the records for the transactionary deletion
        /// </summary>
        /// <param name="items"></param>
        public void Delete(params ExclusionItem[] items);

        /// <summary>
        /// Performs a transactionary database execution of all pending inserts, updates, and deletes
        /// </summary>
        /// <returns>A tuple in the format total [inserts, updates, deletes]</returns>
        public Tuple<long, long, long> WriteExclusions();

        #endregion

        #region Bookmark Related

        /// <summary>
        /// Replaces bookmarks with their values
        /// </summary>
        /// <param name="text">The text to look over</param>
        /// <param name="markType">The type of bookmark to apply</param>
        /// <returns>Return the result</returns>
        public string ApplyBookmarks(string text);

        /// <summary>
        /// Retrieves and returns the bookmarks
        /// </summary>
        /// <returns></returns>
        public List<BookmarkItem> GetBookmarks();

        /// <summary>
        /// Clears any cached bookmarks, forcing them to be reloaded for the next GetBookmarks() call
        /// </summary>
        public void ClearBookmarkCache();

        /// <summary>
        /// Deletes all bookmarks from the database
        /// </summary>
        public long ClearBookmarks();

        /// <summary>
        /// Queues the records for transactionary insertion
        /// </summary>
        /// <param name="items">The records to be added</param>
        /// <returns></returns>
        public void Insert(params BookmarkItem[] items);

        /// <summary>
        /// Queues the records for the transactionary update
        /// </summary>
        /// <param name="items">The records to be updated</param>
        public void Update(params BookmarkItem[] items);

        /// <summary>
        /// Queues the records for the transactionary deletion
        /// </summary>
        /// <param name="items"></param>
        public void Delete(params BookmarkItem[] items);

        /// <summary>
        /// Performs a transactionary database execution of all pending inserts, updates, and deletes
        /// </summary>
        /// <returns>A tuple in the format total [inserts, updates, deletes]</returns>
        public Tuple<long, long, long> WriteBookmarks();

        #endregion

        #region DiskItem Related

        /// <summary>
        /// Retrieves and returns the current number of Disk Items in the database
        /// </summary>
        /// <returns>The number of Disk Item records in the database</returns>
        public long GetDiskItemCount();

        /// <summary>
        /// Updates the provided records' integrity hashes and last hashed dates
        /// </summary>
        /// <param name="diskitems">The records to check</param>
        /// <returns></returns>
        public int UpdateHashes(IEnumerable<DiskItem> diskitems);

        /// <summary>
        /// Removes the matching diskitem records from the database, effectively removing tracking for those files.  
        /// Subsequent scans can however return the records to the structure.
        /// </summary>
        /// <param name="whereDetail">The filtering detail provided through the Find statement's where clause</param>
        /// <param name="paths">The paths to search</param>
        /// <returns></returns>
        public int PurgeQueried(string whereDetail, IEnumerable<string> paths);

        /// <summary>
        /// Retrieves all records immediately inside of any of the provided paths, matching the given filter with the provided whereDetail
        /// </summary>
        /// <param name="whereDetail">The filtering detail provided through the Find statement's where clause</param>
        /// <param name="filter">The wildcard filter</param>
        /// <param name="paths">The paths to search</param>
        /// <returns>The matching DiskItems</returns>
        public IEnumerable<DiskItem> GetFilteredDiskItemsByIn(string whereDetail, string filter, IEnumerable<string> paths);

        /// <summary>
        /// Retrieves all records located at any depth inside of any of the provided paths, matching the given filter with the provided whereDetail
        /// </summary>
        /// <param name="whereDetail">The filtering detail provided through the Find statement's where clause</param>
        /// <param name="filter">The wildcard filter</param>
        /// <param name="paths">The paths to search</param>
        /// <returns>The matching DiskItems</returns>
        public IEnumerable<DiskItem> GetFilteredDiskItemsByWithin(string whereDetail, string filter, IEnumerable<string> paths);

        /// <summary>
        /// Retrieves all records located within any subdirectories immediately inside of any of the provided paths, matching the given filter with the provided whereDetail
        /// </summary>
        /// <param name="whereDetail">The filtering detail provided through the Find statement's where clause</param>
        /// <param name="filter">The wildcard filter</param>
        /// <param name="paths">The paths to search</param>
        /// <returns>The matching DiskItems</returns>
        public IEnumerable<DiskItem> GetFilteredDiskItemsByUnder(string whereDetail, string filter, IEnumerable<string> paths);

        /// <summary>
        /// Attempts to retrieve and return a DiskItem by it's path
        /// </summary>
        /// <param name="paths">The paths to search for</param>
        /// <returns>The DiskItem if found, null otherwise</returns>
        public DiskItem GetDiskItemByPath(string path);

        /// <summary>
        /// Retrieves and returns all disk items with paths that perfectly match an item in the given list
        /// </summary>
        /// <param name="paths">The paths to search for</param>
        /// <returns></returns>
        public IEnumerable<DiskItem> GetDiskItemsByPaths(IEnumerable<string> paths);

        /// <summary>
        /// Deletes all records located within one of the given paths with a LastScanned value prior to the timestamp
        /// </summary>
        /// <param name="timestamp">The cut off point for records' "old" status</param>
        /// <param name="paths">The disk paths where old files must reside</param>
        /// <returns>The number of records deleted</returns>
        public long DeleteOldDiskItems(DateTime timestamp, IEnumerable<string> paths);

        /// <summary>
        /// Queues the records for transactionary insertion
        /// </summary>
        /// <param name="items">The records to be added</param>
        /// <returns></returns>
        public void Insert(params DiskItem[] items);

        /// <summary>
        /// Queues the records for the transactionary update
        /// </summary>
        /// <param name="items">The records to be updated</param>
        public void Update(params DiskItem[] items);

        /// <summary>
        /// Queues the records for the transactionary deletion
        /// </summary>
        /// <param name="items"></param>
        public void Delete(params DiskItem[] items);

        /// <summary>
        /// Performs a transactionary database execution of all pending inserts, updates, and deletes
        /// </summary>
        /// <returns>A tuple in the format total [inserts, updates, deletes]</returns>
        public Tuple<long, long, long> WriteDiskItems();

        #endregion
    }
}
