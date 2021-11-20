// Copyright (c) Zain Al-Ahmary.  All rights reserved.
// Licensed under the MIT License, (the "License"); you may not use this file except in compliance with the License. 
// You may obtain a copy of the License at https://mit-license.org/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HDDL.HDSL
{
    /// <summary>
    /// Encapsulates the grouping and sorting to use for a specific query
    /// </summary>
    class QueryGroupSortSet
    {
        /// <summary>
        /// The columns to group by
        /// </summary>
        public List<string> GroupBy { get; private set; }

        /// <summary>
        /// The columns to sort the records on
        /// </summary>
        public List<string> OrderBy { get; private set; }

        /// <summary>
        /// The page index to return
        /// </summary>
        public long PageIndex { get; private set; }

        /// <summary>
        /// The number of records to count as a page
        /// </summary>
        public long RecordsPerPage { get; private set; }
    }
}
