using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HDDL.Threading
{
    /// <summary>
    /// Represents the possible states of a ThreadedQueue
    /// </summary>
    enum ThreadQueueStatus
    {
        /// <summary>
        /// The ThreadedQueue is not currently running
        /// </summary>
        Idle,
        /// <summary>
        /// The ThreadedQueue is preparing to begin work
        /// </summary>
        Starting,
        /// <summary>
        /// The ThreadedQueue is running
        /// </summary>
        Active,
        /// <summary>
        /// The ThreadedQueue has been shutdown
        /// </summary>
        Disposed,
        /// <summary>
        /// An exception was thrown
        /// </summary>
        Faulted
    }
}
