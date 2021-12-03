// Copyright (c) Zain Al-Ahmary.  All rights reserved.
// Licensed under the MIT License, (the "License"); you may not use this file except in compliance with the License. 
// You may obtain a copy of the License at https://mit-license.org/

namespace HDDL.Scanning.Monitoring
{
    public enum NonSpammingFileSystemWatcherEventTypes
    {
        /// <summary>
        /// Occurs when a disk event (change, delete, create, etc) happens
        /// </summary>
        DiskEvent,
        /// <summary>
        /// Occurs when the non-spamming file system watcher encounters an error and needs to be recycled
        /// </summary>
        InternalError,
        /// <summary>
        /// Occurs when the non-spamming file system watcher encounters an error and needs to be recycled
        /// </summary>
        InternalErrorWithException
    }
}
