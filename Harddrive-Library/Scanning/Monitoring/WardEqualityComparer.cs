// Copyright (c) Zain Al-Ahmary.  All rights reserved.
// Licensed under the MIT License, (the "License"); you may not use this file except in compliance with the License. 
// You may obtain a copy of the License at https://mit-license.org/

using System.Linq;
using HDDL.Data;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System;
using HDDL.IO.Disk;

namespace HDDL.Scanning.Monitoring
{
    /// <summary>
    /// Compares two WardItem instances
    /// </summary>
    class WardEqualityComparer : IEqualityComparer<WardItem>
    {
        /// <summary>
        /// For wards to be equal, one's path must be within the others and they must have identical where clauses.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public bool Equals(WardItem x, WardItem y)
        {
            // Get the where clauses
            var xWhere = string.Empty;
            var yWhere = string.Empty;
            if (x.HDSL.ToLower().Contains("where"))
            {
                xWhere = x.HDSL.ToLower().Split("where").Last();
            }
            if (y.HDSL.ToLower().Contains("where"))
            {
                yWhere = y.HDSL.ToLower().Split("where").Last();
            }

            // Check to see if one checks a path containing the other
            if (PathHelper.IsWithinPath(x.Path, y.Path) ||
                PathHelper.IsWithinPath(y.Path, x.Path))
            {
                // Check to see if they have identical where clauses
                if (xWhere.Equals(yWhere, StringComparison.InvariantCultureIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        public int GetHashCode([DisallowNull] WardItem obj)
        {
            return obj.GetHashCode();
        }
    }
}
