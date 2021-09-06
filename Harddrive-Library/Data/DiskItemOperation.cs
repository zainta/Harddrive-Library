using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HDDL.Data
{
    /// <summary>
    /// Defines an insert or update operation
    /// </summary>
    class DiskItemOperation
    {
        public DiskItem Item;
        public bool IsInsert;

        public override string ToString()
        {
            var op = IsInsert ? "insert" : "update";
            return $"{op} '{Item.Path}'";
        }
    }
}
