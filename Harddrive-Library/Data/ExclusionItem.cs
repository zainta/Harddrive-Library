// Copyright (c) Zain Al-Ahmary.  All rights reserved.
// Licensed under the MIT License, (the "License"); you may not use this file except in compliance with the License. 
// You may obtain a copy of the License at https://mit-license.org/

using HDDL.IO.Disk;
using System;
using System.Data;
using System.Data.SQLite;

namespace HDDL.Data
{
    /// <summary>
    /// Represents an item (file or directory) that should be ignored when scanning hard drives
    /// </summary>
    public class ExclusionItem : HDDLRecordBase
    {
        /// <summary>
        /// The excluded item's location
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        /// Returns a value indicating whether or not the Exclusion is based on a bookmark
        /// </summary>
        public bool IsDynamic
        {
            get
            {
                return BookmarkItem.HasBookmark(Path);
            }
        }

        /// <summary>
        /// Creates an instance from the current record in the data reader
        /// </summary>
        /// <param name="row"></param>
        public ExclusionItem(SQLiteDataReader row) : base(row)
        {
            Path = row.GetString("path");
        }

        /// <summary>
        /// Default constructor
        /// </summary>
        public ExclusionItem() : base()
        {
        }

        /// <summary>
        /// Checks to see if the given path matches the exclusion's target
        /// </summary>
        /// <param name="path">The path to test</param>
        /// <returns></returns>
        public bool IsExcluded(string path)
        {
            return PathHelper.IsWithinPath(path, Path);
        }

        /// <summary>
        /// Generates and returns a SQLite Insert statement for this record
        /// </summary>
        /// <returns>The line of SQL</returns>
        public override string ToInsertStatement()
        {
            return $@"insert into exclusions 
                        (id, path) 
                      values 
                        ('{Id}', '{DataHelper.Sanitize(Path)}');";
        }

        /// <summary>
        /// Generates and returns a SQLite Update statement for this record
        /// </summary>
        /// <returns>The line of SQL</returns>
        public override string ToUpdateStatement()
        {
            return $@"update exclusions 
                        set path = '{DataHelper.Sanitize(Path)}'
                        where id = '{Id}';";
        }

        public override string ToString()
        {
            return $"[Exclusion: '{Id}' - '{Path}']";
        }
    }
}
