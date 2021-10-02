// Copyright (c) Zain Al-Ahmary.  All rights reserved.
// Licensed under the MIT License, (the "License"); you may not use this file except in compliance with the License. 
// You may obtain a copy of the License at https://mit-license.org/

using System;

namespace HDDL.Scanning
{
    /// <summary>
    /// Stores the durations of all of the DiskScan's phases
    /// </summary>
    public class Timings
    {
        /// <summary>
        /// How long the entire process took as a whole
        /// </summary>
        public TimeSpan ScanDuration { get; set; }

        /// <summary>
        /// How long it took to read and build the directory structure
        /// </summary>
        public TimeSpan DirectoryStructureScanDuration { get; set; }

        /// <summary>
        /// How long it took to process the directory structure
        /// </summary>
        public TimeSpan DirectoryStructureProcessingDuration { get; set; }

        /// <summary>
        /// How long it took to write the directory structure to the database
        /// </summary>
        public TimeSpan DatabaseWriteDuration { get; set; }

        /// <summary>
        /// Creates a Timings instance
        /// </summary>
        /// <param name="full"></param>
        /// <param name="diskRead"></param>
        /// <param name="processing"></param>
        /// <param name="dbWrite"></param>
        public Timings(TimeSpan full, TimeSpan diskRead, TimeSpan processing, TimeSpan dbWrite)
        {
            ScanDuration = full;
            DirectoryStructureScanDuration = diskRead;
            DirectoryStructureProcessingDuration = processing;
            DatabaseWriteDuration = dbWrite;
        }

        /// <summary>
        /// Creates a default timing instance
        /// </summary>
        public Timings()
        {
            ScanDuration = TimeSpan.MinValue;
            DirectoryStructureScanDuration = TimeSpan.MinValue;
            DirectoryStructureProcessingDuration = TimeSpan.MinValue;
            DatabaseWriteDuration = TimeSpan.MinValue;
        }

        /// <summary>
        /// Returns the scan duration as a formatted string
        /// </summary>
        /// <returns></returns>
        public string GetScanDuration()
        {
            return string.Format("{0}:{1}:{2}.{3}", 
                ScanDuration.Hours.ToString("D2"), 
                ScanDuration.Minutes.ToString("D2"), 
                ScanDuration.Seconds.ToString("D2"), 
                ScanDuration.Milliseconds);
        }

        /// <summary>
        /// Returns the directory structure scan duration as a formatted string
        /// </summary>
        /// <returns></returns>
        public string GetDirectoryStructureScanDuration()
        {
            return string.Format("{0}:{1}:{2}.{3}", 
                DirectoryStructureScanDuration.Hours.ToString("D2"), 
                DirectoryStructureScanDuration.Minutes.ToString("D2"), 
                DirectoryStructureScanDuration.Seconds.ToString("D2"), 
                DirectoryStructureScanDuration.Milliseconds);
        }

        /// <summary>
        /// Returns the directory structure processing duration as a formatted string
        /// </summary>
        /// <returns></returns>
        public string GetDirectoryStructureProcessingDuration()
        {
            return string.Format("{0}:{1}:{2}.{3}",
                DirectoryStructureProcessingDuration.Hours.ToString("D2"),
                DirectoryStructureProcessingDuration.Minutes.ToString("D2"),
                DirectoryStructureProcessingDuration.Seconds.ToString("D2"),
                DirectoryStructureProcessingDuration.Milliseconds);
        }

        /// <summary>
        /// Returns the database write duration as a formatted string
        /// </summary>
        /// <returns></returns>
        public string GetDatabaseWriteDuration()
        {
            return string.Format("{0}:{1}:{2}.{3}",
                DatabaseWriteDuration.Hours.ToString("D2"),
                DatabaseWriteDuration.Minutes.ToString("D2"),
                DatabaseWriteDuration.Seconds.ToString("D2"),
                DatabaseWriteDuration.Milliseconds);
        }
    }
}
