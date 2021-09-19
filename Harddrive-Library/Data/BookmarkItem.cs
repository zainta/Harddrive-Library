// Copyright (c) Zain Al-Ahmary.  All rights reserved.
// Licensed under the MIT License, (the "License"); you may not use this file except in compliance with the License. 
// You may obtain a copy of the License at https://mit-license.org/

using LiteDB;
using System.ComponentModel.DataAnnotations.Schema;

namespace HDDL.Data
{
    /// <summary>
    /// Represents a bookmark item record, a reference to a file(s) or directory
    /// </summary>
    [Table("Bookmark", Schema = "main")]
    public class BookmarkItem : BsonHDDLRecordBase
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
        /// <param name="record"></param>
        public BookmarkItem(BsonValue record) : base(record)
        {
        }

        /// <summary>
        /// Default constructor
        /// </summary>
        public BookmarkItem() : base()
        {
        }

        public override string ToString()
        {
            return $"[{ItemName}:{Target}]";
        }
    }
}
