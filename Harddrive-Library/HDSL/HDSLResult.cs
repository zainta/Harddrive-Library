using HDDL.HDSL.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HDDL.HDSL
{
    /// <summary>
    /// Represents the results of an HDSL code execution
    /// </summary>
    public class HDSLResult
    {
        /// <summary>
        /// The paths that matched the query
        /// </summary>
        public string[] Paths { get; private set; }

        /// <summary>
        /// Any errors encountered during the process
        /// </summary>
        public HDSLLogBase[] Errors { get; private set; }

        /// <summary>
        /// Creates a success result with the resulting paths as its contents
        /// </summary>
        /// <param name="paths">The paths matching the query</param>
        public HDSLResult(IEnumerable<string> paths)
        {
            Paths = paths.ToArray();
            Errors = new HDSLLogBase[] { };
        }

        /// <summary>
        /// Creates an error result with the errors encountered
        /// </summary>
        /// <param name="errors">The errors encountered during execution</param>
        public HDSLResult(IEnumerable<HDSLLogBase> errors)
        {
            Paths = new string[] { };
            Errors = errors.ToArray();
        }
    }
}
