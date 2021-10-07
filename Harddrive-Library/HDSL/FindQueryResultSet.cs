// Copyright (c) Zain Al-Ahmary.  All rights reserved.
// Licensed under the MIT License, (the "License"); you may not use this file except in compliance with the License. 
// You may obtain a copy of the License at https://mit-license.org/

using HDDL.Data;
using System;
using System.Collections.Generic;
using System.Linq;

namespace HDDL.HDSL
{
    /// <summary>
    /// Contains the results for a HDSL Find query
    /// </summary>
    public class FindQueryResultSet : HDSLQueryOutcome
    {

        /// <summary>
        /// The items resulting from the query
        /// </summary>
        public DiskItem[] Items { get; private set; }

        /// <summary>
        /// Creates a HDSLQueryOutcome
        /// </summary>
        /// <param name="items">The DiskItems to display</param>
        /// <param name="columnData">The default column data to use (path, size, creation date - psc)</param>
        /// <param name="pagingData">The default paging data to use (32 line pages, starting at page 1)</param>
        public FindQueryResultSet(IEnumerable<DiskItem> items, string pagingData = "-1:-1", string columnData = "psc") : base()
        {
            DefaultColumnData = columnData;
            DefaultPagingData = pagingData;
            Items = items.ToArray();
        }

        /// <summary>
        /// Displays the appropriate information from the FindQueryResultSet instance
        /// </summary>
        /// <param name="paging">The paging data dictionary</param>
        /// <param name="columns">A character encoded column string</param>
        /// <param name="displayCounts">Whether or not to display count information</param>
        /// <param name="displayTableEmbelishments">Whether or not to display table lines in the results</param>
        public void Display(string columns = null, string pagingCode = null, bool displayCounts = true, bool displayTableEmbelishments = true)
        {
            DisplayResultTable(Items, columns, pagingCode, displayTableEmbelishments);

            if (displayCounts)
            {
                Console.WriteLine();
                Console.WriteLine($"{Items.Length} matches found.");
            }
        }
    }
}
