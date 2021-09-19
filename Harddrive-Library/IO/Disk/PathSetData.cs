// Copyright (c) Zain Al-Ahmary.  All rights reserved.
// Licensed under the MIT License, (the "License"); you may not use this file except in compliance with the License. 
// You may obtain a copy of the License at https://mit-license.org/

using System.Collections.Generic;

namespace HDDL.IO.Disk
{
    /// <summary>
    /// Contains information on a set of path scan targets
    /// </summary>
    public class PathSetData
    {
        /// <summary>
        /// Structure information about files and folders arranged by root
        /// </summary>
        public Dictionary<string, List<DiskItemType>> TargetInformation { get; private set; }

        /// <summary>
        /// The total number of files to scan
        /// </summary>
        public int TotalFiles { get; set; }

        /// <summary>
        /// The total number of directories
        /// </summary>
        public int TotalDirectories { get; set; }

        /// <summary>
        /// Create a PathSetData
        /// </summary>
        /// <param name="info">Structure information about files and folders arranged by root</param>
        /// <param name="fileCount">The total number of files to scan</param>
        /// <param name="directoryCount">The total number of directories</param>
        public PathSetData(Dictionary<string, List<DiskItemType>> info, int fileCount, int directoryCount)
        {
            TargetInformation = info;
            TotalFiles = fileCount;
            TotalDirectories = directoryCount;
        }
    }
}
