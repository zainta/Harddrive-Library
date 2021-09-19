// Copyright (c) Zain Al-Ahmary.  All rights reserved.
// Licensed under the MIT License, (the "License"); you may not use this file except in compliance with the License. 
// You may obtain a copy of the License at https://mit-license.org/

namespace HDDL.Scanning
{
    /// <summary>
    /// Represents the possible outcomes of a scan operation
    /// </summary>
    public enum ScanOperationOutcome
    {
        /// <summary>
        /// The scan ran to fruition
        /// </summary>
        Completed,
        /// <summary>
        /// The scan was terminated early
        /// </summary>
        Interrupted
    }
}
