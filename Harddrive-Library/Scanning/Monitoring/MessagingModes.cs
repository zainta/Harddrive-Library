// Copyright (c) Zain Al-Ahmary.  All rights reserved.
// Licensed under the MIT License, (the "License"); you may not use this file except in compliance with the License. 
// You may obtain a copy of the License at https://mit-license.org/

using System;

namespace HDDL.Scanning.Monitoring
{
    /// <summary>
    /// Details how often the monitoring classes communicate with the outside world
    /// </summary>
    [Flags]
    public enum MessagingModes
    {
        Error = 1,
        Warning = 2,
        Information = 4,
        VerboseInformation = 8
    }
}
