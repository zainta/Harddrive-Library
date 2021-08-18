using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
