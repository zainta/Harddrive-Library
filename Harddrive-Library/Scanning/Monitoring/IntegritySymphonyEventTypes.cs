// Copyright (c) Zain Al-Ahmary.  All rights reserved.
// Licensed under the MIT License, (the "License"); you may not use this file except in compliance with the License. 
// You may obtain a copy of the License at https://mit-license.org/

namespace HDDL.Scanning.Monitoring
{
    /// <summary>
    /// The types of events represented by the IntegritySymphonyEvent class
    /// </summary>
    public enum IntegritySymphonyEventTypes
    {
        /// <summary>
        /// Occurs when an integrity check starts
        /// </summary>
        ScanStarts,
        /// <summary>
        /// Occurs when an integrity check ends
        /// </summary>
        ScanEnds,
        /// <summary>
        /// Occurs when the Integrity Symphony's state changes
        /// </summary>
        StateChange
    }
}
