// Copyright (c) Zain Al-Ahmary.  All rights reserved.
// Licensed under the MIT License, (the "License"); you may not use this file except in compliance with the License. 
// You may obtain a copy of the License at https://mit-license.org/

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
