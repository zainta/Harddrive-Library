// Copyright (c) Zain Al-Ahmary.  All rights reserved.
// Licensed under the MIT License, (the "License"); you may not use this file except in compliance with the License. 
// You may obtain a copy of the License at https://mit-license.org/

using HDDL.Language.HDSL.Where;
using System;
using System.Collections.Generic;

namespace HDDL.Language.HDSL
{
    /// <summary>
    /// Encapsulates the details of a Find query
    /// </summary>
    class FindQueryDetails
    {
        /// <summary>
        /// The where clause structure associated with the find
        /// </summary>
        public OperatorBase FurtherDetails { get; set; }

        /// <summary>
        /// The paths to search
        /// </summary>
        public IEnumerable<string> Paths { get; set; }

        /// <summary>
        /// If true then the resulting query should be empty
        /// </summary>
        public bool ResultsEmpty { get; set; }

        /// <summary>
        /// The query starting point method
        /// </summary>
        public FindQueryDepths Method { get; set; }

        /// <summary>
        /// The columns to return
        /// </summary>
        public ColumnHeaderSet Columns { get; set; }

        /// <summary>
        /// The type that's being queried
        /// </summary>
        public Type TableContext { get; set; }

        /// <summary>
        /// The grouping and sorting information for the query
        /// </summary>
        public QueryGroupSortSet GroupSortDetails { get; set; }
    }
}
