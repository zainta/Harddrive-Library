// Copyright (c) Zain Al-Ahmary.  All rights reserved.
// Licensed under the MIT License, (the "License"); you may not use this file except in compliance with the License. 
// You may obtain a copy of the License at https://mit-license.org/

using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using System.Data.SQLite;

namespace HDDL.Data
{
    /// <summary>
    /// Represents a bookmark item record, a reference to a file(s) or directory
    /// </summary>
    public class BookmarkItem : HDDLRecordBase
    {
        /// <summary>
        /// The item's target directory path
        /// </summary>
        public string Target { get; set; }

        /// <summary>
        /// The name of the item
        /// </summary>
        public string ItemName { get; set; }

        /// <summary>
        /// Creates an instance from the current record in the data reader
        /// </summary>
        /// <param name="row"></param>
        public BookmarkItem(SQLiteDataReader row) : base(row)
        {
            Target = row.GetString("target");
            ItemName = row.GetString("itemName");
        }

        /// <summary>
        /// Default constructor
        /// </summary>
        public BookmarkItem() : base()
        {
        }

        /// <summary>
        /// Generates and returns a SQLite Insert statement for this record
        /// </summary>
        /// <returns>The line of SQL</returns>
        public override string ToInsertStatement()
        {
            return $@"insert into bookmarks 
                        (id, target, itemName) 
                      values 
                        ('{Id}', '{DataHelper.Sanitize(Target)}', '{DataHelper.Sanitize(ItemName)}');";
        }

        /// <summary>
        /// Generates and returns a SQLite Update statement for this record
        /// </summary>
        /// <returns>The line of SQL</returns>
        public override string ToUpdateStatement()
        {
            return $@"update bookmarks 
                        set target = '{DataHelper.Sanitize(Target)}'
                        where itemName = '{ItemName}';";
        }

        public override string ToString()
        {
            return $"[{ItemName}:{Target}]";
        }
    }
}
