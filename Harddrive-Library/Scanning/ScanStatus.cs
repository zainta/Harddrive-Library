// Copyright (c) Zain Al-Ahmary.  All rights reserved.
// Licensed under the MIT License, (the "License"); you may not use this file except in compliance with the License. 
// You may obtain a copy of the License at https://mit-license.org/

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
