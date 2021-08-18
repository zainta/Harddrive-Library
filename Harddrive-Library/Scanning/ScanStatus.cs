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
        /// Preparing to perform a scan
        /// </summary>
        InitiatingScan,
        /// <summary>
        /// Performing a scan
        /// </summary>
        Scanning,
        /// <summary>
        /// Deleting items that were previously found but no longer exist
        /// </summary>
        Deleting,
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
