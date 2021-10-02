// Copyright (c) Zain Al-Ahmary.  All rights reserved.
// Licensed under the MIT License, (the "License"); you may not use this file except in compliance with the License. 
// You may obtain a copy of the License at https://mit-license.org/

using System.Data;
using System.Data.SQLite;

namespace HDDL.Data
{
    /// <summary>
    /// Represents a path that should be avoiding when scanning hard drives
    /// </summary>
    public class ExclusionItem : HDDLRecordBase
    {
        /// <summary>
        /// The excluded region's border (starting point)
        /// </summary>
        public string Region { get; set; }

        /// <summary>
        /// Returns a value indicating whether or not the Exclusion is based on a bookmark
        /// </summary>
        public bool IsDynamic
        {
            get
            {
                return Region.Contains("[") || Region.Contains("]");
            }
        }


        /// <summary>
        /// Creates an instance from the current record in the data reader
        /// </summary>
        /// <param name="row"></param>
        public ExclusionItem(SQLiteDataReader row) : base(row)
        {
            Region = row.GetString("region");
        }

        /// <summary>
        /// Default constructor
        /// </summary>
        public ExclusionItem() : base()
        {
        }

        /// <summary>
        /// Generates and returns a SQLite Insert statement for this record
        /// </summary>
        /// <returns>The line of SQL</returns>
        public override string ToInsertStatement()
        {
            return $@"insert into exclusions 
                        (id, region) 
                      values 
                        ('{Id}', '{DataHelper.Sanitize(Region)}');";
        }

        /// <summary>
        /// Generates and returns a SQLite Update statement for this record
        /// </summary>
        /// <returns>The line of SQL</returns>
        public override string ToUpdateStatement()
        {
            return $@"update exclusions 
                        set region = '{DataHelper.Sanitize(Region)}'
                        where id = '{Id}';";
        }

        public override string ToString()
        {
            return $"[Exclusion: '{Id}' - '{Region}']";
        }
    }
}
