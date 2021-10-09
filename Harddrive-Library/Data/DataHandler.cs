﻿// Copyright (c) Zain Al-Ahmary.  All rights reserved.
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
    /// Provides data encapsulation to allow modification of systems without complete overhauls
    /// </summary>
    public class DataHandler : IDisposable
    {
        public const int InfiniteDepth = -1;

        /// <summary>
        /// The connection string to the database
        /// </summary>
        public string ConnectionString { get; private set; }

        /// <summary>
        /// The bookmark cache
        /// </summary>
        private List<BookmarkItem> _bookmarkCache;

        /// <summary>
        /// The exclusion cache
        /// </summary>
        private List<ExclusionItem> _exclusionCache;
        /// <summary>
        /// The filtered location cache
        /// </summary>
        private List<FilteredLocationItem> _filteredLocationCache;

        /// <summary>
        /// Stores pending database actions for DiskItems
        /// </summary>
        private RecordActionContainer<DiskItem> _diskItems;

        /// <summary>
        /// Stores pending database actions for Bookmarks
        /// </summary>
        private RecordActionContainer<BookmarkItem> _bookmarks;

        /// <summary>
        /// Stores pending database actions for Exclusions
        /// </summary>
        private RecordActionContainer<ExclusionItem> _exclusions;

        /// <summary>
        /// Stores pending database actions for Filtered Locations
        /// </summary>
        private RecordActionContainer<FilteredLocationItem> _filterLocations;

        /// <summary>
        /// The current database connection
        /// </summary>
        private SQLiteConnection _connection;

        /// <summary>
        /// Creates a DataHandler
        /// </summary>
        /// <param name="connectionString">The connection string</param>
        public DataHandler(string connectionString)
        {
            ConnectionString = connectionString;
            _diskItems = new RecordActionContainer<DiskItem>();
            _bookmarks = new RecordActionContainer<BookmarkItem>();
            _exclusions = new RecordActionContainer<ExclusionItem>();
            _filterLocations = new RecordActionContainer<FilteredLocationItem>();
            _bookmarkCache = null;
            _exclusionCache = null;
            _filteredLocationCache = null;
        }

        ~DataHandler()
        {
            Dispose();
        }

        #region Database Utility

        /// <summary>
        /// Initializes the database at the indicated path.
        /// Recreates it if it already exists
        /// </summary>
        /// <param name="recreate">If true, deletes and rebuilds the file database</param>
        /// <param name="connectionString">The connection string</param>
        public static void InitializeDatabase(string connectionString, bool recreate = false)
        {
            if (recreate && File.Exists(connectionString))
            {
                File.Delete(connectionString);
            }

            if (!File.Exists(connectionString))
            {
                SQLiteConnection.CreateFile(connectionString);

                // create the tables
                using (var sqltCon = new SQLiteConnection($"data source={connectionString}"))
                {
                    using (var command = new SQLiteCommand(
                            @"
                            create table if not exists diskitems (
                                id text not null primary key,
                                parentId text references diskitems(id) on delete cascade,
                                firstScanned text not null,
                                lastScanned text not null,
                                path text not null unique,
                                depth integer not null,
                                itemName text not null,
                                isFile integer not null,
                                extension text,
                                size integer,
                                lastWritten text,
                                lastAccessed text,
                                created text not null,
                                hash text,
                                lastHashed text
                                );
                             create unique index diskitems_path_index on diskitems(path);

                            create table if not exists bookmarks (
                                id text not null primary key,
                                target text not null,
                                itemName text not null
                                );
                            create unique index bookmarks_itemName_index on bookmarks(itemName);

                            create table if not exists filteredlocations (
                                id text not null primary key,
                                target text not null,
                                filter text,
                                expectsReadOnly integer,
                                expectsArchive integer,
                                expectsSystem integer,
                                expectsHidden integer,
                                expectsNonIndexed integer,
                                itemName text not null,
                                explorationMode text
                                );
                            create unique index filteredlocations_itemName_index on filteredlocations(itemName);

                            create table if not exists exclusions (
                                id text not null primary key,
                                region text not null
                                );
                            create unique index exclusions_region_index on exclusions(region);",
                                sqltCon))
                    {

                        if (sqltCon.State == ConnectionState.Closed)
                            sqltCon.Open();

                        command.ExecuteNonQuery();
                    }
                }
            }
        }

        /// <summary>
        /// Ensures that a connection is available and open
        /// </summary>
        /// <returns></returns>
        private SQLiteConnection EnsureConnection()
        {
            if (_connection == null)
            {
                _connection = new SQLiteConnection($"data source={ConnectionString}");
            }
            if (_connection.State == ConnectionState.Closed)
                _connection.Open();

            return _connection;
        }

        /// <summary>
        /// Executes a query
        /// </summary>
        /// <param name="sql">The sql to execute</param>
        private void ExecuteNonQuery(string sql)
        {
            using (var command = new SQLiteCommand(sql, EnsureConnection()))
            {
                command.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// Executes a query
        /// </summary>
        /// <param name="sql">The sql to execute</param>
        /// <param name="behavior">The commands behavior</param>
        private SQLiteDataReader ExecuteReader(string sql, CommandBehavior behavior = CommandBehavior.Default)
        {
            using (var command = new SQLiteCommand(sql, EnsureConnection()))
            {
                return command.ExecuteReader(behavior);
            }
        }

        /// <summary>
        /// Executes a query with a single return value
        /// </summary>
        /// <param name="sql">The sql to execute</param>
        /// <param name="behavior">The commands behavior</param>
        private T ExecuteScalar<T>(string sql, CommandBehavior behavior = CommandBehavior.Default)
        {
            T result = default(T);
            using (var command = new SQLiteCommand(sql, EnsureConnection()))
            {
                result = (T)command.ExecuteScalar();
            }

            return result;
        }

        /// <summary>
        /// Executes a SQL query and returns the number of records returned by it
        /// </summary>
        /// <param name="sql">The query to execute</param>
        /// <returns></returns>
        private long GetCount(string sql)
        {
            using (var command = new SQLiteCommand($"select count(id) from {sql}", EnsureConnection()))
            {
                return (long)command.ExecuteScalar();
            }
        }

        #endregion

        #region Exclusion Related

        /// <summary>
        /// Retrieves and returns the bookmarks
        /// </summary>
        /// <returns></returns>
        public List<ExclusionItem> GetExclusions()
        {
            if (_exclusionCache == null ||
                (_exclusionCache != null && _exclusionCache.Count == 0))
            {
                _exclusionCache = new List<ExclusionItem>();

                var exclusions = ExecuteReader(@"select * from exclusions");
                while (exclusions.Read())
                {
                    _exclusionCache.Add(new ExclusionItem(exclusions));
                }
            }

            return _exclusionCache;
        }

        /// <summary>
        /// Retrieves and returns all Exclusions and expands dynamic exclusions (removing dead ones from the list)
        /// </summary>
        /// <returns></returns>
        public List<ExclusionItem> GetProcessedExclusions()
        {
            var results = new List<ExclusionItem>();
            foreach (var e in GetExclusions())
            {
                if (e.IsDynamic)
                {
                    e.Region = ApplyBookmarks(e.Region);
                }

                results.Add(e);
            }

            return results;
        }

        /// <summary>
        /// Clears any cached bookmarks, forcing them to be reloaded for the next GetBookmarks() call
        /// </summary>
        public void ClearExclusionCache()
        {
            _exclusionCache.Clear();
            _exclusionCache = null;
        }

        /// <summary>
        /// Deletes all bookmarks from the database
        /// </summary>
        public long ClearExclusions()
        {
            var count = GetCount("exclusions");
            ExecuteNonQuery("delete from exclusions");
            return count;
        }

        /// <summary>
        /// Queues the records for transactionary insertion
        /// </summary>
        /// <param name="items">The records to be added</param>
        /// <returns></returns>
        public void Insert(params ExclusionItem[] items)
        {
            foreach (var item in items)
            {
                _exclusions.Inserts.Add(item);
            }
        }

        /// <summary>
        /// Queues the records for the transactionary update
        /// </summary>
        /// <param name="items">The records to be updated</param>
        public void Update(params ExclusionItem[] items)
        {
            foreach (var item in items)
            {
                _exclusions.Updates.Add(item);
            }
        }

        /// <summary>
        /// Queues the records for the transactionary deletion
        /// </summary>
        /// <param name="items"></param>
        public void Delete(params ExclusionItem[] items)
        {
            foreach (var item in items)
            {
                _exclusions.Deletions.Add(item);
            }
        }

        /// <summary>
        /// Performs a transactionary database execution of all pending inserts, updates, and deletes
        /// </summary>
        /// <returns>A tuple in the format total [inserts, updates, deletes]</returns>
        public Tuple<long, long, long> WriteExclusions()
        {
            long inserts = 0, updates = 0, deletes = 0;
            if (_exclusions.HasWork)
            {
                using (var transaction = EnsureConnection().BeginTransaction())
                {
                    if (_exclusions.Inserts.Count > 0)
                    {
                        foreach (var insert in _exclusions.Inserts)
                        {
                            ExecuteNonQuery(insert.ToInsertStatement());
                            inserts++;
                        }
                        _exclusions.Inserts.Clear();
                    }

                    if (_exclusions.Updates.Count > 0)
                    {
                        foreach (var update in _exclusions.Updates)
                        {
                            ExecuteNonQuery(update.ToUpdateStatement());
                            updates++;
                        }
                        _exclusions.Updates.Clear();
                    }

                    if (_exclusions.Deletions.Count > 0)
                    {
                        var sql = new StringBuilder("(");
                        foreach (var delete in _exclusions.Deletions)
                        {
                            if (sql.Length > 1)
                            {
                                sql.Append(", ");
                            }
                            sql.Append($"'{delete.Id}'");
                            deletes++;
                        }
                        sql.Append(")");

                        deletes = GetCount($"exclusions where id in {sql};");
                        ExecuteNonQuery($"DELETE from exclusions where id in {sql};");
                        _exclusions.Deletions.Clear();
                    }

                    transaction.Commit();
                }
            }

            if (inserts > 0 || updates > 0 || deletes > 0)
            {
                ClearExclusionCache();
            }

            return new Tuple<long, long, long>(inserts, updates, deletes);
        }

        #endregion

        #region Bookmark Related

        /// <summary>
        /// Replaces bookmarks with their values
        /// </summary>
        /// <param name="text">The text to look over</param>
        /// <param name="markType">The type of bookmark to apply</param>
        /// <returns>Return the result</returns>
        public string ApplyBookmarks(string text)
        {
            foreach (var bm in GetBookmarks())
            {
                text = text.Replace($"[{bm.ItemName}]", bm.Target);
            }

            return text;
        }

        /// <summary>
        /// Retrieves and returns the bookmarks
        /// </summary>
        /// <returns></returns>
        public List<BookmarkItem> GetBookmarks()
        {
            if (_bookmarkCache == null ||
                (_bookmarkCache != null && _bookmarkCache.Count == 0))
            {
                _bookmarkCache = new List<BookmarkItem>();

                var bookmarks = ExecuteReader(@"select * from bookmarks");
                while (bookmarks.Read())
                {
                    _bookmarkCache.Add(new BookmarkItem(bookmarks));
                }
            }

            return _bookmarkCache;
        }

        /// <summary>
        /// Clears any cached bookmarks, forcing them to be reloaded for the next GetBookmarks() call
        /// </summary>
        public void ClearBookmarkCache()
        {
            _bookmarkCache.Clear();
            _bookmarkCache = null;
        }

        /// <summary>
        /// Deletes all bookmarks from the database
        /// </summary>
        public long ClearBookmarks()
        {
            var count = GetCount("bookmarks");
            ExecuteNonQuery("delete from bookmarks");
            return count;
        }

        /// <summary>
        /// Queues the records for transactionary insertion
        /// </summary>
        /// <param name="items">The records to be added</param>
        /// <returns></returns>
        public void Insert(params BookmarkItem[] items)
        {
            foreach (var item in items)
            {
                _bookmarks.Inserts.Add(item);
            }
        }

        /// <summary>
        /// Queues the records for the transactionary update
        /// </summary>
        /// <param name="items">The records to be updated</param>
        public void Update(params BookmarkItem[] items)
        {
            foreach (var item in items)
            {
                _bookmarks.Updates.Add(item);
            }
        }

        /// <summary>
        /// Queues the records for the transactionary deletion
        /// </summary>
        /// <param name="items"></param>
        public void Delete(params BookmarkItem[] items)
        {
            foreach (var item in items)
            {
                _bookmarks.Deletions.Add(item);
            }
        }

        /// <summary>
        /// Performs a transactionary database execution of all pending inserts, updates, and deletes
        /// </summary>
        /// <returns>A tuple in the format total [inserts, updates, deletes]</returns>
        public Tuple<long, long, long> WriteBookmarks()
        {
            long inserts = 0, updates = 0, deletes = 0;
            if (_bookmarks.HasWork)
            {
                using (var transaction = EnsureConnection().BeginTransaction())
                {
                    if (_bookmarks.Inserts.Count > 0)
                    {
                        foreach (var insert in _bookmarks.Inserts)
                        {
                            ExecuteNonQuery(insert.ToInsertStatement());
                            inserts++; 
                        }
                        _bookmarks.Inserts.Clear();
                    }

                    if (_bookmarks.Updates.Count > 0)
                    {
                        foreach (var update in _bookmarks.Updates)
                        {
                            ExecuteNonQuery(update.ToUpdateStatement());
                            updates++;
                        }
                        _bookmarks.Updates.Clear();
                    }

                    if (_bookmarks.Deletions.Count > 0)
                    {
                        var sql = new StringBuilder("(");
                        foreach (var delete in _bookmarks.Deletions)
                        {
                            if (sql.Length > 0)
                            {
                                sql.Append(", ");
                            }
                            sql.Append($"'{delete.Id}'");
                            deletes++;
                        }
                        sql.Append(")");

                        deletes = GetCount($"bookmarks where id in {sql};");
                        ExecuteNonQuery($"DELETE from bookmarks where id in {sql};");
                        _bookmarks.Deletions.Clear();
                    }

                    transaction.Commit();
                }
            }

            if (inserts > 0 || updates > 0 || deletes > 0)
            {
                ClearBookmarkCache();
            }

            return new Tuple<long, long, long>(inserts, updates, deletes);
        }

        #endregion

        #region Filtered Locations Related

        /// <summary>
        /// Retrieves and returns the filtered locations
        /// </summary>
        /// <returns></returns>
        public List<FilteredLocationItem> GetFilteredLocations()
        {
            if (_filteredLocationCache == null ||
                (_filteredLocationCache != null && _filteredLocationCache.Count == 0))
            {
                _filteredLocationCache = new List<FilteredLocationItem>();

                var locations = ExecuteReader(@"select * from filteredlocations");
                while (locations.Read())
                {
                    _filteredLocationCache.Add(new FilteredLocationItem(locations));
                }
            }

            return _filteredLocationCache;
        }

        /// <summary>
        /// Clears any cached filtered locations, forcing them to be reloaded for the next GetFilteredLocation() call
        /// </summary>
        public void ClearFilteredLocationCache()
        {
            _filteredLocationCache.Clear();
            _filteredLocationCache = null;
        }

        /// <summary>
        /// Deletes all filtered locations from the database
        /// </summary>
        public long ClearFilteredLocations()
        {
            var count = GetCount("filteredlocations");
            ExecuteNonQuery("delete from filteredlocations");
            return count;
        }

        /// <summary>
        /// Queues the records for transactionary insertion
        /// </summary>
        /// <param name="items">The records to be added</param>
        /// <returns></returns>
        public void Insert(params FilteredLocationItem[] items)
        {
            foreach (var item in items)
            {
                _filterLocations.Inserts.Add(item);
            }
        }

        /// <summary>
        /// Queues the records for the transactionary update
        /// </summary>
        /// <param name="items">The records to be updated</param>
        public void Update(params FilteredLocationItem[] items)
        {
            foreach (var item in items)
            {
                _filterLocations.Updates.Add(item);
            }
        }

        /// <summary>
        /// Queues the records for the transactionary deletion
        /// </summary>
        /// <param name="items"></param>
        public void Delete(params FilteredLocationItem[] items)
        {
            foreach (var item in items)
            {
                _filterLocations.Deletions.Add(item);
            }
        }

        /// <summary>
        /// Performs a transactionary database execution of all pending inserts, updates, and deletes
        /// </summary>
        /// <returns>A tuple in the format total [inserts, updates, deletes]</returns>
        public Tuple<long, long, long> WriteFilteredLocations()
        {
            long inserts = 0, updates = 0, deletes = 0;
            if (_filterLocations.HasWork)
            {
                using (var transaction = EnsureConnection().BeginTransaction())
                {
                    if (_filterLocations.Inserts.Count > 0)
                    {
                        foreach (var insert in _filterLocations.Inserts)
                        {
                            ExecuteNonQuery(insert.ToInsertStatement());
                            inserts++;
                        }
                        _filterLocations.Inserts.Clear();
                    }

                    if (_filterLocations.Updates.Count > 0)
                    {
                        foreach (var update in _filterLocations.Updates)
                        {
                            ExecuteNonQuery(update.ToUpdateStatement());
                            updates++;
                        }
                        _filterLocations.Updates.Clear();
                    }

                    if (_filterLocations.Deletions.Count > 0)
                    {
                        var sql = new StringBuilder("(");
                        foreach (var delete in _filterLocations.Deletions)
                        {
                            if (sql.Length > 0)
                            {
                                sql.Append(", ");
                            }
                            sql.Append($"'{delete.Id}'");
                            deletes++;
                        }
                        sql.Append(")");

                        deletes = GetCount($"filteredlocations where id in {sql};");
                        ExecuteNonQuery($"DELETE from filteredlocations where id in {sql};");
                        _filterLocations.Deletions.Clear();
                    }

                    transaction.Commit();
                }
            }

            if (inserts > 0 || updates > 0 || deletes > 0)
            {
                ClearFilteredLocationCache();
            }

            return new Tuple<long, long, long>(inserts, updates, deletes);
        }

        #endregion

        #region DiskItem Related

        /// <summary>
        /// Retrieves and returns the current number of Disk Items in the database
        /// </summary>
        /// <returns>The number of Disk Item records in the database</returns>
        public long GetDiskItemCount()
        {
            var count = ExecuteScalar<long>("select count(*) from diskitems;");
            return count;
        }

        /// <summary>
        /// Updates the provided records' integrity hashes and last hashed dates
        /// </summary>
        /// <param name="diskitems">The records to check</param>
        /// <returns></returns>
        public int UpdateHashes(IEnumerable<DiskItem> diskitems)
        {
            var count = 0;
            if (diskitems.Count() > 0)
            {
                using (var transaction = EnsureConnection().BeginTransaction())
                {
                    foreach (var diskitem in diskitems)
                    {
                        ExecuteNonQuery(diskitem.ToHashUpdateStatement());
                        count++;
                    }

                    transaction.Commit();
                }
            }

            return count;
        }

        /// <summary>
        /// Removes the matching diskitem records from the database, effectively removing tracking for those files.  
        /// Subsequent scans can however return the records to the structure.
        /// </summary>
        /// <param name="whereDetail">The filtering detail provided through the Find statement's where clause</param>
        /// <param name="paths">The paths to search</param>
        /// <returns></returns>
        public int PurgeQueried(string whereDetail, IEnumerable<string> paths)
        {
            var queries = new List<string>();
            if (paths.Count() > 0)
            {
                foreach (var path in paths)
                {
                    var query = $"[directive] diskitems"; // " where path like '{path}%'"

                    // build the where clause
                    var whereRequired = false;
                    var whereClause = new StringBuilder();
                    if (paths.Count() > 0)
                    {
                        whereClause.Append($"path like '{path}%'");
                        whereRequired = true;
                    }

                    if (!string.IsNullOrWhiteSpace(whereDetail))
                    {
                        if (whereClause.Length > 0)
                        {
                            whereClause.Append($" and ");
                        }
                        whereClause.Append($"({whereDetail});");
                        whereRequired = true;
                    }

                    if (whereRequired)
                    {
                        queries.Add($"{query} where {whereClause}");
                    }
                }
            }
            else
            {
                // if we have no paths, check if there's a whereDetail defined.
                if (!string.IsNullOrWhiteSpace(whereDetail))
                {
                    queries.Add($"[directive] diskitems where ({whereDetail});");
                }
                else
                {
                    queries.Add($"[directive] diskitems;");
                }
            }

            // get a count of the number of records that will be purged
            var count = 0;
            var reader = ExecuteReader(string.Join('\n', queries).Replace("[directive]", "select count(*) from"));
            if (reader.HasRows)
            {
                do
                {
                    while (reader.HasRows && reader.Read())
                    {
                        count += reader.GetInt32(0);
                    }
                }
                while (reader.NextResult());
            }

            // Perform the deletions
            ExecuteNonQuery(string.Join('\n', queries).Replace("[directive]", "delete from"));
            return count;
        }

        /// <summary>
        /// Retrieves all records at the provided depths relative to the given path, obeying all defined criteria
        /// </summary>
        /// <param name="whereDetail">The filtering detail provided through the Find statement's where clause</param>
        /// <param name="filter">The wildcard filter</param>
        /// <param name="paths">The paths to search</param>
        /// <param name="depthSpecification">The depth criteria for the search</param>
        /// <returns>The matching DiskItems</returns>
        private IEnumerable<DiskItem> GetFilteredDiskItems(string whereDetail, string filter, IEnumerable<string> paths, string depthSpecification)
        {
            var queries = new List<string>();
            var results = new List<DiskItem>();
            foreach (var path in paths)
            {
                var depth = PathHelper.GetDependencyCount(new DiskItemType(path, false));
                var filterClause = !string.IsNullOrWhiteSpace(filter) ? $" and itemName like '{filter.Replace("*", "%")}'" : string.Empty;
                var query = $"select * from diskitems where path like '{path}%'{filterClause} and {depthSpecification}".Replace("[current]", depth.ToString());
                if (!string.IsNullOrWhiteSpace(whereDetail))
                {
                    queries.Add($"{query} and ({whereDetail});");
                }
                else
                {
                    queries.Add($"{query};");
                }
            }

            var reader = ExecuteReader(string.Join('\n', queries));
            if (reader.HasRows)
            {
                do
                {
                    while (reader.HasRows && reader.Read())
                    {
                        results.Add(new DiskItem(reader));
                    }
                }
                while (reader.NextResult());
            }

            return results;
        }

        /// <summary>
        /// Retrieves all records immediately inside of any of the provided paths, matching the given filter with the provided whereDetail
        /// </summary>
        /// <param name="whereDetail">The filtering detail provided through the Find statement's where clause</param>
        /// <param name="filter">The wildcard filter</param>
        /// <param name="paths">The paths to search</param>
        /// <returns>The matching DiskItems</returns>
        public IEnumerable<DiskItem> GetFilteredDiskItemsByIn(string whereDetail, string filter, IEnumerable<string> paths)
        {
            return GetFilteredDiskItems(whereDetail, filter, paths, $"depth = ([current] + 1)");
        }

        /// <summary>
        /// Retrieves all records located at any depth inside of any of the provided paths, matching the given filter with the provided whereDetail
        /// </summary>
        /// <param name="whereDetail">The filtering detail provided through the Find statement's where clause</param>
        /// <param name="filter">The wildcard filter</param>
        /// <param name="paths">The paths to search</param>
        /// <returns>The matching DiskItems</returns>
        public IEnumerable<DiskItem> GetFilteredDiskItemsByWithin(string whereDetail, string filter, IEnumerable<string> paths)
        {
            return GetFilteredDiskItems(whereDetail, filter, paths, $"depth > [current]");
        }

        /// <summary>
        /// Retrieves all records located within any subdirectories immediately inside of any of the provided paths, matching the given filter with the provided whereDetail
        /// </summary>
        /// <param name="whereDetail">The filtering detail provided through the Find statement's where clause</param>
        /// <param name="filter">The wildcard filter</param>
        /// <param name="paths">The paths to search</param>
        /// <returns>The matching DiskItems</returns>
        public IEnumerable<DiskItem> GetFilteredDiskItemsByUnder(string whereDetail, string filter, IEnumerable<string> paths)
        {
            return GetFilteredDiskItems(whereDetail, filter, paths, $"depth > ([current] + 1)");
        }

        /// <summary>
        /// Attempts to retrieve and return a DiskItem by it's path
        /// </summary>
        /// <param name="paths">The paths to search for</param>
        /// <returns>The DiskItem if found, null otherwise</returns>
        public DiskItem GetDiskItemByPath(string path)
        {
            DiskItem result = null;
            var reader = ExecuteReader($"select * from diskitems where path = '{DataHelper.Sanitize(path)}';");
            if (reader.HasRows && reader.Read())
            {
                result = new DiskItem(reader);
            }

            return result;
        }

        /// <summary>
        /// Retrieves and returns all disk items with paths that perfectly match an item in the given list
        /// </summary>
        /// <param name="paths">The paths to search for</param>
        /// <returns></returns>
        public IEnumerable<DiskItem> GetDiskItemsByPaths(IEnumerable<string> paths)
        {
            List<DiskItem> results = new List<DiskItem>();
            var reader = ExecuteReader($"select * from diskitems where path in {DataHelper.GetListing(paths, true)};");
            if (reader.HasRows)
            {
                while (reader.Read())
                {
                    results.Add(new DiskItem(reader));
                }
            }

            return results;
        }

        /// <summary>
        /// Deletes all records located within one of the given paths with a LastScanned value prior to the timestamp
        /// </summary>
        /// <param name="timestamp">The cut off point for records' "old" status</param>
        /// <param name="paths">The disk paths where old files must reside</param>
        /// <returns>The number of records deleted</returns>
        public long DeleteOldDiskItems(DateTime timestamp, IEnumerable<string> paths)
        {
            var where = $"where {DataHelper.GetListing(paths, true, " or ", "path like '", "%'")} and lastscanned < '{DateTimeDataHelper.ConvertToString(timestamp)}';";
            var count = GetCount($"diskitems {where}");
            ExecuteNonQuery($"delete from diskitems {where}");
            return count;
        }

        /// <summary>
        /// Queues the records for transactionary insertion
        /// </summary>
        /// <param name="items">The records to be added</param>
        /// <returns></returns>
        public void Insert(params DiskItem[] items)
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
        public void Update(params DiskItem[] items)
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
        public void Delete(params DiskItem[] items)
        {
            foreach (var item in items)
            {
                _diskItems.Deletions.Add(item);
            }
        }

        /// <summary>
        /// Performs a transactionary database execution of all pending inserts, updates, and deletes
        /// </summary>
        /// <returns>A tuple in the format total [inserts, updates, deletes]</returns>
        public Tuple<long, long, long> WriteDiskItems()
        {
            long inserts = 0, updates = 0, deletes = 0;
            if (_diskItems.HasWork)
            {
                using (var transaction = EnsureConnection().BeginTransaction())
                {
                    if (_diskItems.Inserts.Count > 0)
                    {
                        foreach (var insert in _diskItems.Inserts)
                        {
                            ExecuteNonQuery(insert.ToInsertStatement());
                            inserts++; 
                        }
                        _diskItems.Inserts.Clear();
                    }

                    if (_diskItems.Updates.Count > 0)
                    {
                        foreach (var update in _diskItems.Updates)
                        {
                            ExecuteNonQuery(update.ToUpdateStatement());
                            updates++;
                        }
                        _diskItems.Updates.Clear();
                    }

                    if (_diskItems.Deletions.Count > 0)
                    {
                        var sql = new StringBuilder("(");
                        foreach (var delete in _diskItems.Deletions)
                        {
                            if (sql.Length > 0)
                            {
                                sql.Append(", ");
                            }
                            sql.Append($"'{delete.Id}'");
                            deletes++;
                        }
                        sql.Append(")");

                        deletes = GetCount($"diskitems where id in {sql};");
                        ExecuteNonQuery($"DELETE from diskitems where id in {sql};");
                        _diskItems.Deletions.Clear();
                    }

                    transaction.Commit();
                }
            }

            return new Tuple<long, long, long>(inserts, updates, deletes);
        }

        #endregion

        public void Dispose()
        {
            if (_connection != null)
            {
                if (_connection.State != ConnectionState.Closed)
                {
                    _connection.Close();
                }

                _connection.Dispose();
                _connection = null;
            }
        }
    }
}
