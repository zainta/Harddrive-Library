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
    /// Used to pass query results around with their column header set
    /// </summary>
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
        /// Marries the two items together for ease transportation
        /// </summary>
        /// <param name="records">The items</param>
        /// <param name="columns">the columns</param>
        /// <param name="type">The returned records' type</param>
        /// <param name="statement">The HDSL statement that produced these results</param>
        public HDSLResultBag(IEnumerable<HDDLRecordBase> records, ColumnHeaderSet columns, Type type, string statement)
        {
            RecordType = type;
            Columns = columns;
            Records = records.ToArray();
            Statement = statement;
        }

        /// <summary>
        /// Converts the result bag to an outcome
        /// </summary>
        /// <returns></returns>
        public HDSLOutcome AsOutcome()
        {
            return new HDSLOutcome(Records, Columns, RecordType, Statement);
        }
    }
}
