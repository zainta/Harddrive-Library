// Copyright (c) Zain Al-Ahmary.  All rights reserved.
// Licensed under the MIT License, (the "License"); you may not use this file except in compliance with the License. 
// You may obtain a copy of the License at https://mit-license.org/

using ReddWare.Language.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace HDDL.Language.HDSL.Results
{
    /// <summary>
    /// Represents a single row in an outcome set
    /// </summary>
    public class HDSLRecord
    {
        /// <summary>
        /// The record's columns
        /// </summary>
        public HDSLValueItem[] Data { get; private set; }

        /// <summary>
        /// The type of database record represented
        /// </summary>
        public string Type { get; private set; }

        /// <summary>
        /// The columns represented in the data
        /// </summary>
        [JsonIgnore]
        public string[] Columns
        {
            get
            {
                return (from d in Data select d.Column).ToArray();
            }
        }

        /// <summary>
        /// Retrieves the value item for the given column
        /// </summary>
        /// <param name="column">The column to retrieve</param>
        /// <returns></returns>
        [JsonIgnore]
        public HDSLValueItem this[string column]
        {
            get
            {
                return (from d in Data where d.Column.Equals(column, StringComparison.InvariantCultureIgnoreCase) select d).SingleOrDefault();
            }
        }

        /// <summary>
        /// Json support constructor
        /// </summary>
        public HDSLRecord()
        {
            Data = null;
            Type = string.Empty;
        }

        /// <summary>
        /// Creates an HDSLRecord
        /// </summary>
        /// <param name="content"></param>
        /// <param name="type"></param>
        public HDSLRecord(IEnumerable<HDSLValueItem> content, string type)
        {
            Data = content.ToArray();
            Type = type;
        }
    }
}
