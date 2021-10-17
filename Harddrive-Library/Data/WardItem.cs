// Copyright (c) Zain Al-Ahmary.  All rights reserved.
// Licensed under the MIT License, (the "License"); you may not use this file except in compliance with the License. 
// You may obtain a copy of the License at https://mit-license.org/

using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;

namespace HDDL.Data
{
    /// <summary>
    /// Defines a periodic integrity check against a specific file or path
    /// </summary>
    public class WardItem : HDDLRecordBase
    {
        /// <summary>
        /// The raw HDSL code for the integrity check this ward represents
        /// </summary>
        public string HDSL { get; set; }

        /// <summary>
        /// The integrity check's single target
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        /// The actual DiskItem instance pointed to by Path
        /// </summary>
        public DiskItem Target { get; set; }

        /// <summary>
        /// The next time an integrity check should be performed on behalf of this Ward
        /// </summary>
        public DateTime NextScan { get; set; }

        /// <summary>
        /// The amount of time between scans
        /// </summary>
        public TimeSpan Interval { get; set; }

        /// <summary>
        /// Creates an instance from the current record in the data reader
        /// </summary>
        /// <param name="row"></param>
        /// <param name="di"></param>
        public WardItem(SQLiteDataReader row, DiskItem di) : base(row)
        {
            Path = row.GetString("path");
            HDSL = row.GetString("call");
            NextScan = DateTimeDataHelper.ConvertToDateTime(row.GetString("scheduledFor"));
            Interval = TimeSpan.Parse(row.GetString("interval"));
            Target = di;
        }

        /// <summary>
        /// Default constructor
        /// </summary>
        public WardItem() : base()
        {
        }

        /// <summary>
        /// Generates and returns a SQLite Insert statement for this record
        /// </summary>
        /// <returns>The line of SQL</returns>
        public override string ToInsertStatement()
        {
            return $@"insert into wards 
                        (id, path, call, scheduledFor, interval) 
                      values 
                        (
                           '{Id}', 
                           '{DataHelper.Sanitize(Path)}', 
                           '{DataHelper.Sanitize(HDSL)}', 
                           '{DateTimeDataHelper.ConvertToString(NextScan)}', 
                           '{Interval}'
                        );";
        }

        /// <summary>
        /// Generates and returns a SQLite Update statement for this record
        /// </summary>
        /// <returns>The line of SQL</returns>
        public override string ToUpdateStatement()
        {
            return $@"update watches 
                        set path = '{DataHelper.Sanitize(Path)}',
                            call = '{DataHelper.Sanitize(HDSL)}',
                            scheduledFor = '{DateTimeDataHelper.ConvertToString(NextScan)}',
                            interval = '{Interval}'
                        where id = '{Id}';";
        }

        /// <summary>
        /// Returns whether or not the ward is due for a scan
        /// </summary>
        /// <returns></returns>
        public bool IsDue()
        {
            return DateTime.Now >= NextScan;
        }

        /// <summary>
        /// Steps the next scan field forward by interval
        /// </summary>
        public void Increment()
        {
            NextScan = DateTime.Now.Add(Interval);
        }

        public override string ToString()
        {
            var remaining = NextScan.Subtract(DateTime.Now);
            if (IsDue())
            {
                return $"['{Path}' T-0]";
            }
            else
            {
                var part = string.Empty;
                if (remaining.TotalDays > 0)
                {
                    part = $"{remaining.TotalDays} days";
                }
                else if (remaining.TotalHours > 0)
                {
                    part = $"{remaining.TotalHours} hours";
                }
                else if (remaining.TotalMinutes > 0)
                {
                    part = $"{remaining.TotalMinutes} minutes";
                }
                else if (remaining.TotalSeconds > 0)
                {
                    part = $"{remaining.TotalSeconds} seconds";
                }
                else if (remaining.TotalMilliseconds > 0)
                {
                    part = $"{remaining.TotalMilliseconds} milliseconds";
                }
                return $"['{Path}'T-{part}]";
            }
        }
    }
}
