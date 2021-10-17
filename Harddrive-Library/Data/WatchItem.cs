// Copyright (c) Zain Al-Ahmary.  All rights reserved.
// Licensed under the MIT License, (the "License"); you may not use this file except in compliance with the License. 
// You may obtain a copy of the License at https://mit-license.org/

using System.Data;
using System.Data.SQLite;
using System.Text;

namespace HDDL.Data
{
    /// <summary>
    /// Describes both an initial disk scan and follow up monitoring of the location
    /// </summary>
    public class WatchItem : HDDLRecordBase
    {
        /// <summary>
        /// The path string to the location of the scan
        /// </summary>
        internal string Path { get; set; }

        /// <summary>
        /// If in passive mode, this will contain the disk item representations the targeted path
        /// </summary>
        public DiskItem Target { get; set; }

        /// <summary>
        /// True if the initial scan has occurred or reset, false otherwise
        /// </summary>
        public bool InPassiveMode { get; set; }

        /// <summary>
        /// Creates an instance from the current record in the data reader
        /// </summary>
        /// <param name="row"></param>
        /// <param name="di"></param>
        public WatchItem(SQLiteDataReader row, DiskItem di) : base(row)
        {
            Path = row.GetString("path");
            InPassiveMode = row.GetBoolean("inPassiveMode");
            Target = di;
        }

        /// <summary>
        /// Default constructor
        /// </summary>
        public WatchItem() : base()
        {
        }

        /// <summary>
        /// Generates and returns a SQLite Insert statement for this record
        /// </summary>
        /// <returns>The line of SQL</returns>
        public override string ToInsertStatement()
        {
            return $@"insert into watches 
                        (id, path, inPassiveMode) 
                      values 
                        ('{Id}', '{DataHelper.Sanitize(Path)}', {InPassiveMode});";
        }

        /// <summary>
        /// Generates and returns a SQLite Update statement for this record
        /// </summary>
        /// <returns>The line of SQL</returns>
        public override string ToUpdateStatement()
        {
            return $@"update watches 
                        set path = '{DataHelper.Sanitize(Path)}',
                            inPassiveMode = {InPassiveMode}
                        where id = '{Id}';";
        }

        public override string ToString()
        {
            var state = InPassiveMode ? "Passive" : "Fresh";
            return $"[Watch: '{Id}' - {state} - '{Path}']";
        }
    }
}
