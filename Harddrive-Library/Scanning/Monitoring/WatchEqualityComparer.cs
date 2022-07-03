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
    /// Compares two WatchItem instances
    /// </summary>
    class WatchEqualityComparer : IEqualityComparer<WatchItem>
    {
        /// <summary>
        /// For watches to be equal, one's path must be equal to or within the others.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public bool Equals(WatchItem x, WatchItem y)
        {
            var xInY = PathHelper.IsWithinPath(x.Path, y.Path);
            var yInX = PathHelper.IsWithinPath(y.Path, x.Path);

            return xInY || yInX;
        }

        public int GetHashCode([DisallowNull] WatchItem obj)
        {
            return obj.GetHashCode();
        }
    }
}
