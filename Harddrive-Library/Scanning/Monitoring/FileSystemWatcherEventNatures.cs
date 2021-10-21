// Copyright (c) Zain Al-Ahmary.  All rights reserved.
// Licensed under the MIT License, (the "License"); you may not use this file except in compliance with the License. 
// You may obtain a copy of the License at https://mit-license.org/

namespace HDDL.Scanning.Monitoring
{
    /// <summary>
    /// Describes the types of occurrences a NonSpammingFileSystemWatcher can inform of
    /// </summary>
    enum FileSystemWatcherEventNatures
    {
        /// <summary>
        /// Something was created
        /// </summary>
        Creation,
        /// <summary>
        /// Something was deleted
        /// </summary>
        Deletion,
        /// <summary>
        /// Something was changed
        /// </summary>
        Alteration
    }
}
