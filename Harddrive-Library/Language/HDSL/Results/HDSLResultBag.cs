// Copyright (c) Zain Al-Ahmary.  All rights reserved.
// Licensed under the MIT License, (the "License"); you may not use this file except in compliance with the License. 
// You may obtain a copy of the License at https://mit-license.org/

using HDDL.Data;
using HDDL.Language.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace HDDL.Language.HDSL.Results
{
    /// <summary>
    /// Used to pass query results around with their column header set
    /// </summary>
    [JsonIgnore]
    public class HDSLResultBag
    {
        /// <summary>
        /// The associated column header set
        /// </summary>
        public ColumnHeaderSet Columns { get; private set; }

        /// <summary>
        /// The records
        /// </summary>
        public HDDLRecordBase[] Records { get; private set; }

        /// <summary>
        /// The returned records' type
        /// </summary>
        public Type RecordType { get; private set; }

        /// <summary>
        /// The HDSL statement that produced these results
        /// </summary>
        public string Statement { get; private set; }

        /// <summary>
        /// The total number of records found by the query
        /// </summary>
        public long TotalRecords { get; internal set; }

        /// <summary>
        /// The number of records returned per page
        /// </summary>
        public long RecordsPerPage { get; internal set; }

        /// <summary>
        /// The current page index returned
        /// </summary>
        public long PageIndex { get; internal set; }

        /// <summary>
        /// Marries the two items together for ease transportation
        /// </summary>
        /// <param name="records">The items</param>
        /// <param name="columns">the columns</param>
        /// <param name="type">The returned records' type</param>
        /// <param name="statement">The HDSL statement that produced these results</param>
        /// <param name="pageIndex">The current page index returned</param>
        /// <param name="recordsPerPage">The number of records returned per page</param>
        /// <param name="totalRecords">The total number of records found by the query</param>
        public HDSLResultBag(IEnumerable<HDDLRecordBase> records, ColumnHeaderSet columns, Type type, string statement, long totalRecords, long recordsPerPage, long pageIndex)
        {
            RecordType = type;
            Columns = columns;
            Records = records.ToArray();
            Statement = statement;
            TotalRecords = totalRecords;
            RecordsPerPage = recordsPerPage;
            PageIndex = pageIndex;
        }

        /// <summary>
        /// Converts the result bag to an outcome
        /// </summary>
        /// <returns></returns>
        public HDSLOutcome AsOutcome()
        {
            return new HDSLOutcome(Records, Columns, RecordType, Statement)
            {
                PageIndex = PageIndex,
                RecordsPerPage = RecordsPerPage,
                TotalRecords = TotalRecords
            };
        }
    }
}
