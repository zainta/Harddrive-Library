// Copyright (c) Zain Al-Ahmary.  All rights reserved.
// Licensed under the MIT License, (the "License"); you may not use this file except in compliance with the License. 
// You may obtain a copy of the License at https://mit-license.org/

using HDDL.Data;
using HDDL.Language.HDSL;
using HDDL.Language.HDSL.Results;
using System.Collections.Generic;

namespace HDDL.Scanning.Results
{
    /// <summary>
    /// Represents the outcome of an integrity scan
    /// </summary>
    public class HDSLScanOutcome : HDSLOutcome
    {
        /// <summary>
        /// Contains information about how long different parts of the scan took
        /// </summary>
        public Timings DurationData { get; internal set; }

        /// <summary>
        /// The number of new disk items found
        /// </summary>
        public long Inserts { get; internal set; }

        /// <summary>
        /// The number of existing disk items were found to still exist
        /// </summary>
        public long Updates { get; internal set; }

        /// <summary>
        /// The number of disk items the scan revealed to have been deleted
        /// </summary>
        public long Deletions { get; internal set; }

        /// <summary>
        /// Takes a column header set and a set of disk items and builds the results
        /// </summary>
        /// <param name="scanned">The records that were scanned</param>
        /// <param name="timings">Contains information about how long different parts of the scan took</param>
        /// <param name="columns">The columns from those records to return</param>
        /// <param name="deletions">The number of disk items the scan revealed to have been deleted</param>
        /// <param name="inserts">The number of new disk items found</param>
        /// <param name="updates">The number of existing disk items were found to still exist</param>
        /// <param name="statement">The HDSL statement that produced these results</param>
        public HDSLScanOutcome(
            IEnumerable<DiskItem> scanned, 
            long inserts, 
            long updates, 
            long deletions, 
            Timings timings, 
            ColumnHeaderSet columns,
            string statement) : base(scanned, columns, typeof(DiskItem), statement)
        {
            DurationData = timings;
            Inserts = inserts;
            Updates = updates;
            Deletions = deletions;
        }
    }
}