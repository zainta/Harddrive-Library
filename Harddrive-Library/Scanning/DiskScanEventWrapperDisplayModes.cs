// Copyright (c) Zain Al-Ahmary.  All rights reserved.
// Licensed under the MIT License, (the "License"); you may not use this file except in compliance with the License. 
// You may obtain a copy of the License at https://mit-license.org/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HDDL.Scanning
{
    /// <summary>
    /// The modes of output possible for the DiskScanEventWrapper class
    /// </summary>
    public enum DiskScanEventWrapperDisplayModes
    {
        ProgressBar,
        Spinner,
        Text,
        Displayless
    }
}
