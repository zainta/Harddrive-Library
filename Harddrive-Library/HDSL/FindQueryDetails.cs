using HDDL.HDSL.Where;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HDDL.HDSL
{
    /// <summary>
    /// Encapsulates the details of a Find query
    /// </summary>
    class FindQueryDetails
    {
        /// <summary>
        /// The query's wildcard
        /// </summary>
        public string Wildcard { get; set; }

        /// <summary>
        /// The where clause structure associated with the find
        /// </summary>
        public OperatorBase FurtherDetails { get; set; }

        /// <summary>
        /// The paths to search
        /// </summary>
        public IEnumerable<string> Paths { get; set; }

        /// <summary>
        /// If true then the resulting query should be empty
        /// </summary>
        public bool ResultsEmpty { get; set; }

        /// <summary>
        /// The query starting point method
        /// </summary>
        public FindQueryDepths Method { get; set; }

        /// <summary>
        /// The columns to return
        /// </summary>
        public ColumnHeaderSet Columns { get; set; }
    }
}
