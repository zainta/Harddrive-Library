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
    /// Describes both an initial disk scan and follow up monitoring of the location
    /// </summary>
    public class WatchItem : HDDLRecordBase
    {
        /// <summary>
        /// The path string to the location of the scan
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        /// If in passive mode, this will contain the disk item representations the targeted path
        /// </summary>
        public DiskItem Target { get; set; }

        /// <summary>
        /// True if the initial scan has occurred or reset, false otherwise
        /// </summary>
        public bool InPassiveMode { get; set; }

        /// <summary>
        /// Whether or not scans should be reexecuted at specific times
        /// </summary>
        public bool PerformRefreshScans { get; set; }

        /// <summary>
        /// Stores the time when refresh scans are performed
        /// </summary>
        public DateTime? RefreshTime { get; set; }

        /// <summary>
        /// Creates an instance from the current record in the data reader
        /// </summary>
        /// <param name="row"></param>
        /// <param name="di"></param>
        public WatchItem(SQLiteDataReader row, DiskItem di) : base(row)
        {
            Path = row.GetString("path");
            InPassiveMode = row.GetBoolean("inPassiveMode");
            PerformRefreshScans = row.GetBoolean("performRefreshScans");
            RefreshTime = row["refreshTime"] != null ? DateTimeDataHelper.ConvertToDateTime(row.GetString("refreshTime")) : null;
            Target = di;
        }

        /// <summary>
        /// Default constructor
        /// </summary>
        public WatchItem() : base()
        {
        }

        /// <summary>
        /// Returns whether or not the watch is due for a refresh
        /// </summary>
        /// <returns></returns>
        public bool IsDue()
        {
            return DateTime.Now >= RefreshTime;
        }

        /// <summary>
        /// Steps the RefreshTime field forward to the next day at the same time as today
        /// </summary>
        public void Increment()
        {
            if (RefreshTime.HasValue)
            {
                RefreshTime = DateTime.Today
                    .AddDays(1)
                    .AddHours(RefreshTime.Value.Hour)
                    .AddMinutes(RefreshTime.Value.Minute)
                    .AddSeconds(RefreshTime.Value.Second);
            }
        }

        /// <summary>
        /// Generates and returns a SQLite Insert statement for this record
        /// </summary>
        /// <returns>The line of SQL</returns>
        public override string ToInsertStatement()
        {
            var refreshTimePart = PerformRefreshScans ? $",\n '{DateTimeDataHelper.ConvertToString(RefreshTime.Value)}'" : string.Empty;
            return $@"insert into watches 
                        (id, path, inPassiveMode, performRefreshScans, refreshTime) 
                      values 
                        (
                            '{Id}', 
                            '{DataHelper.Sanitize(Path)}', 
                            {InPassiveMode}, 
                            {PerformRefreshScans}{refreshTimePart}
                        );";
        }

        /// <summary>
        /// Generates and returns a SQLite Update statement for this record
        /// </summary>
        /// <returns>The line of SQL</returns>
        public override string ToUpdateStatement()
        {
            var refreshTimePart = PerformRefreshScans ? $",\n refreshTime = '{DateTimeDataHelper.ConvertToString(RefreshTime.Value)}'" : string.Empty;
            return $@"update watches 
                        set path = '{DataHelper.Sanitize(Path)}',
                            inPassiveMode = {InPassiveMode},
                            performRefreshScans = {PerformRefreshScans}{refreshTimePart}
                        where id = '{Id}';";
        }

        public override string ToString()
        {
            var state = InPassiveMode ? "Passive" : "Fresh";
            return $"[Watch: '{Id}' - {state} - '{Path}']";
        }
    }
}
