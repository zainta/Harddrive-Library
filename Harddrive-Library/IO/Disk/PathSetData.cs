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
        /// Contains the files and directories, grouped by type (directories first) and distance from the root
        /// </summary>
        public List<List<DiskItemType>> ProcessedContent { get; set; }

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
        public PathSetData()
        {
            ProcessedContent = new List<List<DiskItemType>>();
            TotalFiles = 0;
            TotalDirectories = 0;
        }
    }
}
