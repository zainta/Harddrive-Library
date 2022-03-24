using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HDDL.Data
{
    /// <summary>
    /// Contains paging information for WideSearches
    /// </summary>
    public class DataHandlerPagingInformation : HDDLRecordBase
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
        /// Creates a paging information record
        /// </summary>
        /// <param name="rpp">The number of records per page</param>
        /// <param name="tr">The total number of records</param>
        /// <param name="pi">The current page index</param>
        public DataHandlerPagingInformation(int rpp, long tr, long pi) : base()
        {
            Id = Guid.Empty;

            RecordsPerPage = rpp;
            TotalRecords = tr;
            TotalPages = tr / rpp;
            PageIndex = pi;
        }
    }
}
