// Copyright (c) Zain Al-Ahmary.  All rights reserved.
// Licensed under the MIT License, (the "License"); you may not use this file except in compliance with the License. 
// You may obtain a copy of the License at https://mit-license.org/

using System;
using System.Data;
using System.Data.SQLite;
using System.Text;

namespace HDDL.Data
{
    /// <summary>
    /// Captures a change recorded in a file
    /// </summary>
    public class DiskItemHashLogItem : HDDLRecordBase
    {
        /// <summary>
        /// The Id of the referenced DiskItem
        /// </summary>
        public Guid ParentId { get; set; }

        /// <summary>
        /// The path to the disk item
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        /// The UTC time that the change occurred
        /// </summary>
        public DateTime Occurred { get; set; }

        /// <summary>
        /// The original file hash
        /// </summary>
        public string OldFileHash { get; set; }

        /// <summary>
        /// The new file hash
        /// </summary>
        public string NewFileHash { get; set; }

        /// <summary>
        /// The UNC name of the machine where the disk item resides
        /// </summary>
        public string MachineUNCName { get; set; }

        /// <summary>
        /// Creates an instance from the current record in the data reader
        /// </summary>
        /// <param name="row"></param>
        internal DiskItemHashLogItem(SQLiteDataReader row) : base(row)
        {
            ParentId = row.GetGuid("parentId");
            Path = row.GetString("path");
            Occurred = DateTimeDataHelper.ConvertToDateTime(row.GetString("occurred"));
            OldFileHash = row.GetString("oldhash");
            NewFileHash = row.GetString("newhash");
            MachineUNCName = row.GetString("unc");
        }

        /// <summary>
        /// Default constructor
        /// </summary>
        internal DiskItemHashLogItem() : base()
        {

        }

        /// <summary>
        /// Generates and returns a SQLite Insert statement for this record
        /// </summary>
        /// <returns>The line of SQL</returns>
        public override string ToInsertStatement()
        {
            return $@"insert into hashlog 
                        (id, parentId, path, occurred, oldhash, newhash, unc) 
                      values 
                        ('{Id}', 
                         '{ParentId}', 
                         '{DataHelper.Sanitize(Path)}',
                         '{DateTimeDataHelper.ConvertToString(Occurred)}', 
                         '{OldFileHash}', 
                         '{NewFileHash}', 
                         '{DataHelper.Sanitize(MachineUNCName)}');";
        }

        /// <summary>
        /// Generates and returns a SQLite Update statement for this record
        /// </summary>
        /// <returns>The line of SQL</returns>
        public override string ToUpdateStatement()
        {
            return $@"update hashlog 
                      set 
                        parentId = '{ParentId}', 
                        path = '{DataHelper.Sanitize(Path)}',
                        occurred = '{DateTimeDataHelper.ConvertToString(Occurred)}',
                        oldhash = '{OldFileHash}',
                        newhash = '{NewFileHash}',
                        unc = '{DataHelper.Sanitize(MachineUNCName)}'
                      where 
                        id = '{Id}';";
        }

        public override string ToString()
        {
            return $"'{MachineUNCName}' - '{Path}' went from '{OldFileHash}' to '{NewFileHash}'";
        }
    }
}
