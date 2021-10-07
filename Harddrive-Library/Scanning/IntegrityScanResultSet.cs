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
    public class IntegrityScanResultSet : HDSLQueryOutcome
    {
        /// <summary>
        /// A list of files that have changed since the last integrity scan
        /// </summary>
        public DiskItem[] ChangedFiles { get; private set; }

        /// <summary>
        /// A list of all scanned files
        /// </summary>
        public DiskItem[] ScannedFiles { get; private set; }

        /// <summary>
        /// Creates a HDSLQueryOutcome
        /// </summary>
        /// <param name="items">The DiskItems to display</param>
        /// <param name="columnData">The default column data to use (path, file hash, last hash date - phd)</param>
        /// <param name="pagingData">The default paging data to use (32 line pages, starting at page 1)</param>
        public IntegrityScanResultSet(IEnumerable<DiskItem> changedFiles, IEnumerable<DiskItem> scannedFiles, string pagingData = "-1:-1", string columnData = "phd") : base()
        {
            DefaultColumnData = columnData;
            DefaultPagingData = pagingData;
            ChangedFiles = changedFiles.ToArray();
            ScannedFiles = scannedFiles.ToArray();
        }

        /// <summary>
        /// Displays the appropriate information from the HDSLResult instance
        /// </summary>
        /// <param name="paging">The paging data dictionary</param>
        /// <param name="columns">A character encoded column string</param>
        /// <param name="displayCounts">Whether or not to display count information</param>
        /// <param name="displayTableEmbelishments">Whether or not to display table lines in the results</param>
        /// <param name="displayMode">What information to display</param>
        public void Display(
            string columns = null, 
            string pagingCode = null, 
            bool displayCounts = true,
            bool displayTableEmbelishments = true, 
            IntegrityResultSetDisplayModes displayMode = IntegrityResultSetDisplayModes.Changed)
        {
            var displayRecords = new List<DiskItem>();
            switch (displayMode)
            {
                case IntegrityResultSetDisplayModes.Changed:
                    displayRecords.AddRange(ChangedFiles);
                    break;
                case IntegrityResultSetDisplayModes.Scanned:
                    displayRecords.AddRange(ScannedFiles);
                    break;
                case IntegrityResultSetDisplayModes.Unchanged:
                    displayRecords.AddRange(from s in ScannedFiles where !ChangedFiles.Contains(s) select s);
                    break;
            }

            // now display all of the records
            DisplayResultTable(displayRecords, columns, pagingCode, displayTableEmbelishments);

            if (displayCounts)
            {
                Console.WriteLine();
                switch (displayMode)
                {
                    case IntegrityResultSetDisplayModes.Changed:
                        Console.WriteLine($"{displayRecords.Count} of {ScannedFiles.Count()} files changed.");
                        break;
                    case IntegrityResultSetDisplayModes.Scanned:
                        Console.WriteLine($"{ScannedFiles.Count()} files were scanned.");
                        break;
                    case IntegrityResultSetDisplayModes.Unchanged:
                        Console.WriteLine($"{displayRecords.Count} of {ScannedFiles.Count()} files are unchanged.");
                        break;
                }
            }
        }
    }
}
