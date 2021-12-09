// Copyright (c) Zain Al-Ahmary.  All rights reserved.
// Licensed under the MIT License, (the "License"); you may not use this file except in compliance with the License. 
// You may obtain a copy of the License at https://mit-license.org/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace HDDL.HDSL.Results
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
        /// Creates an HDSLRecord
        /// </summary>
        /// <param name="content"></param>
        public HDSLRecord(IEnumerable<HDSLValueItem> content)
        {
            Data = content.ToArray();
        }
    }
}
