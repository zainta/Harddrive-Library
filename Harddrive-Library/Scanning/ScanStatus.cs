using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HDDL.Scanning
{
    /// <summary>
    /// The valid scan statuses
    /// </summary>
    public enum ScanStatus
    {
        /// <summary>
        /// Waiting to perform a scan
        /// </summary>
        Ready,
        /// <summary>
        /// Performing a scan
        /// </summary>
        Scanning,
        /// <summary>
        /// Terminating a scan early
        /// </summary>
        Interrupting,
        /// <summary>
        /// Idle after a scan was terminated early
        /// </summary>
        Interrupted
    }
}
