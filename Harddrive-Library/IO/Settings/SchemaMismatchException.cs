using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HDDL.IO.Settings
{
    /// <summary>
    /// Thrown when the IniFileManager is performing a fill and the schema and the file do not match in structure
    /// </summary>
    public class SchemaMismatchException : Exception
    {
        public SchemaMismatchException(string message) : base(message)
        {

        }
    }
}
