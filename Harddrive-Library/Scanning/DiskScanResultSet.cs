// Copyright (c) Zain Al-Ahmary.  All rights reserved.
// Licensed under the MIT License, (the "License"); you may not use this file except in compliance with the License. 
// You may obtain a copy of the License at https://mit-license.org/

using HDDL.Data;
using HDDL.HDSL;
using System;
using System.Collections.Generic;
using System.Linq;

namespace HDDL.Scanning
{
    /// <summary>
    /// Contains the results for a HDSL Find query
    /// </summary>
    public class DiskScanResultSet : HDSLQueryOutcome
    {
        /// <summary>
        /// The number of new disk items found
        /// </summary>
        public long Discoveries { get; private set; }

        /// <summary>
        /// The number of already known disk items found
        /// </summary>
        public long Rediscoveries { get; private set; }

        /// <summary>
        /// The number of known disk items that were not rediscovered
        /// </summary>
        public long Losses { get; private set; }

        /// <summary>
        /// Information about the time taken by the disk scan's individual stages
        /// </summary>
        public Timings Times { get; internal set; }

        /// <summary>
        /// Creates a HDSLQueryOutcome
        /// </summary>
        /// <param name="inserts">The number of new disk items found</param>
        /// <param name="updates">The number of already known disk items found</param>
        /// <param name="deletions">The number of known disk items that were not rediscovered</param>
        /// <param name="durationInformation">Information about the time taken by the disk scan's individual stages</param>
        /// <param name="columnData">The default column data to use (path, size, creation date - psc)</param>
        /// <param name="pagingData">The default paging data to use (32 line pages, starting at page 1)</param>
        public DiskScanResultSet(long inserts, long updates, long deletions, Timings durationInformation, string pagingData = "-1:-1", string columnData = "psc") : base()
        {
            DefaultColumnData = columnData;
            DefaultPagingData = pagingData;
            Times = durationInformation;
            Discoveries = inserts;
            Rediscoveries = updates;
            Losses = deletions;
        }

        /// <summary>
        /// Displays the appropriate information from the DiskScanResultSet instance
        /// </summary>
        /// <param name="paging">The paging data dictionary</param>
        /// <param name="columns">A character encoded column string</param>
        /// <param name="displayCounts">Whether or not to display count information</param>
        /// <param name="displayTableEmbelishments">Whether or not to display table lines in the results</param>
        public void Display(
            bool displayCounts = true, 
            bool displayTimingInformation = true)
        {
            if (displayCounts)
            {
                Console.WriteLine();
                Console.WriteLine("-------------------------------------------------------------------");
                if (Discoveries > 0)
                {
                    Console.WriteLine($"| Successfully added {Discoveries} records to the database.");
                }
                if (Rediscoveries > 0)
                {
                    Console.WriteLine($"| Successfully updated {Rediscoveries} records in the database.");
                }
                if (Losses > 0)
                {
                    Console.WriteLine($"| Successfully deleted {Losses} records from the database.");
                }
                Console.WriteLine("-------------------------------------------------------------------");
            }

            if (displayTimingInformation)
            {
                var total = Discoveries + Rediscoveries + Losses;

                Console.WriteLine();
                Console.WriteLine("| Timing Information");
                Console.WriteLine("-------------------------------------------------------------------");
                Console.WriteLine($"| Discovered {total} disk items in {Times.GetDirectoryStructureScanDuration()}.");
                Console.WriteLine($"| Processed {total} disk items in {Times.GetDirectoryStructureProcessingDuration()}.");
                Console.WriteLine($"| Saving changes to the database took {Times.GetDatabaseWriteDuration()}.");
                Console.WriteLine($"| The entire process took {Times.GetScanDuration()}.");
            }
        }
    }
}
