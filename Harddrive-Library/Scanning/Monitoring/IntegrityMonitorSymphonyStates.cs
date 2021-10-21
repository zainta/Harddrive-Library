// Copyright (c) Zain Al-Ahmary.  All rights reserved.
// Licensed under the MIT License, (the "License"); you may not use this file except in compliance with the License. 
// You may obtain a copy of the License at https://mit-license.org/

namespace HDDL.Scanning.Monitoring
{
    /// <summary>
    /// Defines the possible states of the IntegrityMonitorSymphony
    /// </summary>
    enum IntegrityMonitorSymphonyStates
    {
        Idle,
        Active,
        Stopping,
        Faulted
    }
}
