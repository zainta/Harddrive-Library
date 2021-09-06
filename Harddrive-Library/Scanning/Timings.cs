using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
                ScanDuration.Hours, 
                ScanDuration.Minutes, 
                ScanDuration.Seconds, 
                ScanDuration.Milliseconds);
        }

        /// <summary>
        /// Returns the directory structure scan duration as a formatted string
        /// </summary>
        /// <returns></returns>
        public string GetDirectoryStructureScanDuration()
        {
            return string.Format("{0}:{1}:{2}.{3}", 
                DirectoryStructureScanDuration.Hours, 
                DirectoryStructureScanDuration.Minutes, 
                DirectoryStructureScanDuration.Seconds, 
                DirectoryStructureScanDuration.Milliseconds);
        }

        /// <summary>
        /// Returns the directory structure processing duration as a formatted string
        /// </summary>
        /// <returns></returns>
        public string GetDirectoryStructureProcessingDuration()
        {
            return string.Format("{0}:{1}:{2}.{3}",
                DirectoryStructureProcessingDuration.Hours,
                DirectoryStructureProcessingDuration.Minutes,
                DirectoryStructureProcessingDuration.Seconds,
                DirectoryStructureProcessingDuration.Milliseconds);
        }

        /// <summary>
        /// Returns the database write duration as a formatted string
        /// </summary>
        /// <returns></returns>
        public string GetDatabaseWriteDuration()
        {
            return string.Format("{0}:{1}:{2}.{3}",
                DatabaseWriteDuration.Hours,
                DatabaseWriteDuration.Minutes,
                DatabaseWriteDuration.Seconds,
                DatabaseWriteDuration.Milliseconds);
        }
    }
}
