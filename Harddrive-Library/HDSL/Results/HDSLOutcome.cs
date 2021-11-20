// Copyright (c) Zain Al-Ahmary.  All rights reserved.
// Licensed under the MIT License, (the "License"); you may not use this file except in compliance with the License. 
// You may obtain a copy of the License at https://mit-license.org/

using HDDL.Data;
using System;
using System.Collections.Generic;
using System.Linq;

namespace HDDL.HDSL.Results
{
    /// <summary>
    /// Contains the results a single query
    /// </summary>
    public class HDSLOutcome
    {
        /// <summary>
        /// Contains information about the columns returned
        /// </summary>
        public ColumnDefinition[] Columns { get; private set; }

        /// <summary>
        /// Contains the results from the query
        /// </summary>
        public Dictionary<string, object>[] Records { get; private set; }

        /// <summary>
        /// The returned records' type
        /// </summary>
        public Type RecordType { get; private set; }

        /// <summary>
        /// The HDSL statement that produced these results
        /// </summary>
        public string Statement { get; internal set; }

        /// <summary>
        /// Takes a column header set and a set of disk items and builds the results
        /// </summary>
        /// <param name="items">The records to return</param>
        /// <param name="columns">The columns from those records to return</param>
        /// <param name="type">The returned records' type</param>
        /// <param name="statement">The HDSL statement that produced these results</param>
        public HDSLOutcome(IEnumerable<HDDLRecordBase> items, ColumnHeaderSet columns, Type type, string statement)
        {
            Statement = statement;
            RecordType = type;
            Columns = (from m in columns.Mappings select new ColumnDefinition(m)).ToArray();

            var results = new List<Dictionary<string, object>>();
            foreach (var item in items)
            {
                results.Add(columns.GetColumns(item));
            }
            Records = results.ToArray();
        }
    }
}
