// Copyright (c) Zain Al-Ahmary.  All rights reserved.
// Licensed under the MIT License, (the "License"); you may not use this file except in compliance with the License. 
// You may obtain a copy of the License at https://mit-license.org/

namespace HDDL.Scanning
{
    /// <summary>
    /// Represents the modes that an integrity scan's result set can be displayed
    /// </summary>
    public enum IntegrityResultSetDisplayModes
    {
        /// <summary>
        /// Displays information on the disk items that changed since the last scan
        /// </summary>
        Changed,
        /// <summary>
        /// Displays information on the disk items that were not changed since the last scan
        /// </summary>
        Unchanged,
        /// <summary>
        /// Displays information on all disk items that were scanned
        /// </summary>
        Scanned
    }
}
