// Copyright (c) Zain Al-Ahmary.  All rights reserved.
// Licensed under the MIT License, (the "License"); you may not use this file except in compliance with the License. 
// You may obtain a copy of the License at https://mit-license.org/

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace HDDL.IO.Disk
{
    /// <summary>
    /// Compares two DiskItemType instances
    /// </summary>
    class DiskItemTypeEqualityComparer : IEqualityComparer<DiskItemType>
    {
        public bool Equals(DiskItemType x, DiskItemType y)
        {
            return x.Path.ToLower() == y.Path.ToLower();
        }

        public int GetHashCode([DisallowNull] DiskItemType obj)
        {
            return obj.Path.GetHashCode();
        }
    }
}
