// Copyright (c) Zain Al-Ahmary.  All rights reserved.
// Licensed under the MIT License, (the "License"); you may not use this file except in compliance with the License. 
// You may obtain a copy of the License at https://mit-license.org/

using HDDL.Language.HDSL.Results;

namespace HDDL.Data
{
    /// <summary>
    /// Contains paging information for WideSearches
    /// </summary>
    public class DataHandlerPagingInformation : HDSLRecord
    {
        /// <summary>
        /// The number of records per page
        /// </summary>
        public int RecordsPerPage { get; private set; }

        /// <summary>
        /// The total number of records
        /// </summary>
        public long TotalRecords { get; private set; }

        /// <summary>
        /// Total number of pages
        /// </summary>
        public long TotalPages { get; private set; }

        /// <summary>
        /// The current page index
        /// </summary>
        public long PageIndex { get; private set; }

        /// <summary>
        /// Constructor for Json
        /// </summary>
        public DataHandlerPagingInformation()
        {

        }

        /// <summary>
        /// Creates a paging information record
        /// </summary>
        /// <param name="rpp">The number of records per page</param>
        /// <param name="tr">The total number of records</param>
        /// <param name="pi">The current page index</param>
        public DataHandlerPagingInformation(int rpp, long tr, long pi) : base()
        {
            RecordsPerPage = rpp;
            TotalRecords = tr;
            TotalPages = tr / rpp;
            PageIndex = pi;
        }
    }
}
