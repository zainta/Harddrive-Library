using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HDDL.Scanning
{
    /// <summary>
    /// Represents the possible outcomes of a scan operation
    /// </summary>
    public enum ScanOperationOutcome
    {
        /// <summary>
        /// The scan ran to fruition
        /// </summary>
        Completed,
        /// <summary>
        /// The scan was terminated early
        /// </summary>
        Interrupted
    }
}
