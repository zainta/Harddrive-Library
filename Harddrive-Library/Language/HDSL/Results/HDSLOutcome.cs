// Copyright (c) Zain Al-Ahmary.  All rights reserved.
// Licensed under the MIT License, (the "License"); you may not use this file except in compliance with the License. 
// You may obtain a copy of the License at https://mit-license.org/

using HDDL.Data;
using System;
using System.Collections.Generic;
using System.Linq;

namespace HDDL.Language.HDSL.Results
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
        public HDSLRecord[] Records { get; private set; }

        /// <summary>
        /// The returned records' type
        /// </summary>
        public string RecordType { get; private set; }

        /// <summary>
        /// The HDSL statement that produced these results
        /// </summary>
        public string Statement { get; internal set; }

        /// <summary>
        /// the column header set
        /// </summary>
        private ColumnHeaderSet _chs;

        /// <summary>
        /// Takes a column header set and a set of disk items and builds the results
        /// </summary>
        /// <param name="items">The records to return</param>
        /// <param name="columns">The columns from those records to return</param>
        /// <param name="type">The returned records' type</param>
        /// <param name="statement">The HDSL statement that produced these results</param>
        public HDSLOutcome(IEnumerable<HDDLRecordBase> items, ColumnHeaderSet columns, Type type, string statement)
        {
            _chs = columns;
            Statement = statement;
            RecordType = type.FullName;
            Columns = (from m in columns.Mappings select new ColumnDefinition(m)).ToArray();

            SetRecords(items);
        }

        /// <summary>
        /// Replaces the records in the Records array
        /// </summary>
        /// <param name="items">The records to convert and store</param>
        public void SetRecords(IEnumerable<HDDLRecordBase> items)
        {
            var results = new List<HDSLRecord>();
            foreach (var item in items)
            {
                results.Add(_chs.GetColumns(item));
            }
            Records = results.ToArray();
        }
    }
}
