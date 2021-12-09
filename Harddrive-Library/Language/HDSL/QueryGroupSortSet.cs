// Copyright (c) Zain Al-Ahmary.  All rights reserved.
// Licensed under the MIT License, (the "License"); you may not use this file except in compliance with the License. 
// You may obtain a copy of the License at https://mit-license.org/

using System.Collections.Generic;

namespace HDDL.Language.HDSL
{
    /// <summary>
    /// Encapsulates the grouping and sorting to use for a specific query
    /// </summary>
    class QueryGroupSortSet
    {
        public const long All_Pages = -1;
        public const long Default_RecordsPerPage = 32;

        /// <summary>
        /// The columns to group by
        /// </summary>
        public List<string> GroupBy { get; set; }

        /// <summary>
        /// The columns to sort the records on
        /// </summary>
        public List<string> OrderBy { get; set; }

        /// <summary>
        /// The page index to return
        /// </summary>
        public long PageIndex { get; set; }

        /// <summary>
        /// The number of records to count as a page
        /// </summary>
        public long RecordsPerPage { get; set; }

        /// <summary>
        /// The total number of pages available
        /// </summary>
        public long TotalPages { get; set; }

        /// <summary>
        /// Indicates the sort direction
        /// </summary>
        public bool AscendingSortOrder { get; set; }

        /// <summary>
        /// Creates a default group and sort set
        /// </summary>
        public QueryGroupSortSet()
        {
            GroupBy = new List<string>();
            OrderBy = new List<string>();
            PageIndex = All_Pages;
            RecordsPerPage = Default_RecordsPerPage;
            TotalPages = 0;
            AscendingSortOrder = true;
        }

        public override string ToString()
        {
            var sortDirection = AscendingSortOrder ? "asc" : "desc";
            var groupCols = GroupBy.Count > 0 ? $" groupby {string.Join(", ", GroupBy)}" : string.Empty;
            var orderCols = OrderBy.Count > 0 ? $" orderby {string.Join(", ", OrderBy)}" : string.Empty;

            if (string.IsNullOrEmpty(orderCols))
            {
                return $"{groupCols}{orderCols}";
            }
            else
            {
                return $"{groupCols}{orderCols} {sortDirection}";
            }
        }

        public string ToSQL()
        {
            var sortDirection = AscendingSortOrder ? "asc" : "desc";
            var groupCols = GroupBy.Count > 0 ? $" group by {string.Join(", ", GroupBy)}" : string.Empty;
            var orderCols = OrderBy.Count > 0 ? $" order by {string.Join(", ", OrderBy)}" : string.Empty;

            if (string.IsNullOrEmpty(orderCols))
            {
                return $"{groupCols}{orderCols}";
            }
            else
            {
                return $"{groupCols}{orderCols} {sortDirection}";
            }
        }
    }
}
