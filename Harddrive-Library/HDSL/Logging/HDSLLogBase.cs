using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HDDL.HDSL.Logging
{
    /// <summary>
    /// Base class for error tracking classes
    /// </summary>
    class HDSLLogBase
    {
        /// <summary>
        /// A description of what happened
        /// </summary>
        public string Message { get; private set; }

        /// <summary>
        /// The column where the error occurred
        /// </summary>
        public int Column { get; private set; }

        /// <summary>
        /// The row where the error occurred
        /// </summary>
        public int Row { get; private set; }

        /// <summary>
        /// Create a log entry
        /// </summary>
        /// <param name="message">The error message</param>
        /// <param name="col">The column where the error occurred</param>
        /// <param name="row">The row where the error occurred</param>
        public HDSLLogBase(int col, int row, string message)
        {
            Message = message;
            Column = col;
            Row = row;
        }
    }
}
