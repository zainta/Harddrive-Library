// Copyright (c) Zain Al-Ahmary.  All rights reserved.
// Licensed under the MIT License, (the "License"); you may not use this file except in compliance with the License. 
// You may obtain a copy of the License at https://mit-license.org/

using HDDL.Data;
using HDDL.HDSL;
using HDDL.HDSL.Results;
using System.Collections.Generic;

namespace HDDL.Scanning.Results
{
    /// <summary>
    /// Represents the outcome of an integrity scan
    /// </summary>
    public class HDSLIntegrityOutcome : HDSLOutcome
    {
        /// <summary>
        /// Contains the results from the query
        /// </summary>
        public Dictionary<string, object>[] Changed { get; private set; }

        /// <summary>
        /// Takes a column header set and a set of disk items and builds the results
        /// </summary>
        /// <param name="scanned">The records that were scanned</param>
        /// <param name="changed">The scanned records that changed</param>
        /// <param name="columns">The columns from those records to return</param>
        /// <param name="statement">The HDSL statement that produced these results</param>
        public HDSLIntegrityOutcome(IEnumerable<DiskItem> scanned, IEnumerable<DiskItem> changed, ColumnHeaderSet columns, string statement) : 
            base(scanned, columns, typeof(DiskItem), statement)
        {
            var results = new List<Dictionary<string, object>>();
            foreach (var item in changed)
            {
                results.Add(columns.GetColumns(item));
            }
            Changed = results.ToArray();
        }
    }
}
