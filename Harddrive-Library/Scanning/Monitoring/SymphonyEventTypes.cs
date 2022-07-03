// Copyright (c) Zain Al-Ahmary.  All rights reserved.
// Licensed under the MIT License, (the "License"); you may not use this file except in compliance with the License. 
// You may obtain a copy of the License at https://mit-license.org/

namespace HDDL.Scanning.Monitoring
{
    /// <summary>
    /// The types of events represented by the SymphonyEvent class
    /// </summary>
    public enum SymphonyEventTypes
    {
        /// <summary>
        /// Occurs when a scan starts
        /// </summary>
        ScanStarts,
        /// <summary>
        /// Occurs when a scan ends
        /// </summary>
        ScanEnds,
        /// <summary>
        /// Occurs when the Symphony-type's state changes
        /// </summary>
        StateChange
    }
}
