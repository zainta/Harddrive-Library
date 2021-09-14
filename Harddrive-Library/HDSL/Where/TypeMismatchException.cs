using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HDDL.HDSL.Where
{
    /// <summary>
    /// Thrown when a two WherreValues with different value types are compared
    /// </summary>
    public class TypeMismatchException : Exception
    {
        public TypeMismatchException(string message) : base(message)
        {

        }
    }
}
