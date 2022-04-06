// Copyright (c) Zain Al-Ahmary.  All rights reserved.
// Licensed under the MIT License, (the "License"); you may not use this file except in compliance with the License. 
// You may obtain a copy of the License at https://mit-license.org/

using HDDL.Data;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using HDDL.Language.HDSL.Results;
using System.Data;

namespace HDDLC.Data
{
    /// <summary>
    /// Represents a results pane from a query
    /// </summary>
    class QueryViewPanel : DependencyObject
    {
        #region TotalRecords

        /// <summary>
        /// TotalRecords Dependency Property
        /// </summary>
        public static readonly DependencyProperty TotalRecordsProperty =
            DependencyProperty.Register("TotalRecords", typeof(long), typeof(QueryViewPanel),
                new FrameworkPropertyMetadata((long)-1,
                    new PropertyChangedCallback(OnTotalRecordsChanged)));

        /// <summary>
        /// The total number of records the query resulted in
        /// </summary>
        public long TotalRecords
        {
            get { return (long)GetValue(TotalRecordsProperty); }
            set { SetValue(TotalRecordsProperty, value); }
        }

        /// <summary>
        /// Handles changes to the TotalRecords property.
        /// </summary>
        private static void OnTotalRecordsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            QueryViewPanel target = (QueryViewPanel)d;
            long oldTotalRecords = (long)e.OldValue;
            long newTotalRecords = target.TotalRecords;
            target.OnTotalRecordsChanged(oldTotalRecords, newTotalRecords);
        }

        /// <summary>
        /// Provides derived classes an opportunity to handle changes to the TotalRecords property.
        /// </summary>
        protected virtual void OnTotalRecordsChanged(long oldTotalRecords, long newTotalRecords)
        {
            UpdateTotalMessage();
        }

        #endregion

        #region RecordsPerPage

        /// <summary>
        /// RecordsPerPage Dependency Property
        /// </summary>
        public static readonly DependencyProperty RecordsPerPageProperty =
            DependencyProperty.Register("RecordsPerPage", typeof(long), typeof(QueryViewPanel),
                new FrameworkPropertyMetadata((long)-1,
                    new PropertyChangedCallback(OnRecordsPerPageChanged)));

        /// <summary>
        /// How many records will be returned per page request
        /// </summary>
        public long RecordsPerPage
        {
            get { return (long)GetValue(RecordsPerPageProperty); }
            set { SetValue(RecordsPerPageProperty, value); }
        }

        /// <summary>
        /// Handles changes to the RecordsPerPage property.
        /// </summary>
        private static void OnRecordsPerPageChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            QueryViewPanel target = (QueryViewPanel)d;
            long oldRecordsPerPage = (long)e.OldValue;
            long newRecordsPerPage = target.RecordsPerPage;
            target.OnRecordsPerPageChanged(oldRecordsPerPage, newRecordsPerPage);
        }

        /// <summary>
        /// Provides derived classes an opportunity to handle changes to the RecordsPerPage property.
        /// </summary>
        protected virtual void OnRecordsPerPageChanged(long oldRecordsPerPage, long newRecordsPerPage)
        {
            UpdateTotalMessage();
        }

        #endregion

        #region TotalPages

        /// <summary>
        /// TotalPages Dependency Property
        /// </summary>
        public static readonly DependencyProperty TotalPagesProperty =
            DependencyProperty.Register("TotalPages", typeof(long), typeof(QueryViewPanel),
                new FrameworkPropertyMetadata((long)-1,
                    new PropertyChangedCallback(OnTotalPagesChanged)));

        /// <summary>
        /// The total number of pages
        /// </summary>
        public long TotalPages
        {
            get { return (long)GetValue(TotalPagesProperty); }
            set { SetValue(TotalPagesProperty, value); }
        }

        /// <summary>
        /// Handles changes to the TotalPages property.
        /// </summary>
        private static void OnTotalPagesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            QueryViewPanel target = (QueryViewPanel)d;
            long oldTotalPages = (long)e.OldValue;
            long newTotalPages = target.TotalPages;
            target.OnTotalPagesChanged(oldTotalPages, newTotalPages);
        }

        /// <summary>
        /// Provides derived classes an opportunity to handle changes to the TotalPages property.
        /// </summary>
        protected virtual void OnTotalPagesChanged(long oldTotalPages, long newTotalPages)
        {
            UpdatePagingMessage();
        }

        #endregion

        #region CurrentPageIndex

        /// <summary>
        /// CurrentPageIndex Dependency Property
        /// </summary>
        public static readonly DependencyProperty CurrentPageIndexProperty =
            DependencyProperty.Register("CurrentPageIndex", typeof(long), typeof(QueryViewPanel),
                new FrameworkPropertyMetadata((long)-1,
                    new PropertyChangedCallback(OnCurrentPageIndexChanged)));

        /// <summary>
        /// The current page's index
        /// </summary>
        public long CurrentPageIndex
        {
            get { return (long)GetValue(CurrentPageIndexProperty); }
            set { SetValue(CurrentPageIndexProperty, value); }
        }

        /// <summary>
        /// Handles changes to the CurrentPageIndex property.
        /// </summary>
        private static void OnCurrentPageIndexChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            QueryViewPanel target = (QueryViewPanel)d;
            long oldCurrentPageIndex = (long)e.OldValue;
            long newCurrentPageIndex = target.CurrentPageIndex;
            target.OnCurrentPageIndexChanged(oldCurrentPageIndex, newCurrentPageIndex);
        }

        /// <summary>
        /// Provides derived classes an opportunity to handle changes to the CurrentPageIndex property.
        /// </summary>
        protected virtual void OnCurrentPageIndexChanged(long oldCurrentPageIndex, long newCurrentPageIndex)
        {
            UpdatePagingMessage();
        }

        #endregion

        #region SearchQuery

        /// <summary>
        /// SearchQuery Dependency Property
        /// </summary>
        public static readonly DependencyProperty SearchQueryProperty =
            DependencyProperty.Register("SearchQuery", typeof(string), typeof(QueryViewPanel),
                new FrameworkPropertyMetadata((string)null,
                    new PropertyChangedCallback(OnSearchQueryChanged)));

        /// <summary>
        /// The current query whose results are being browsed
        /// </summary>
        public string SearchQuery
        {
            get { return (string)GetValue(SearchQueryProperty); }
            set { SetValue(SearchQueryProperty, value); }
        }

        /// <summary>
        /// Handles changes to the SearchQuery property.
        /// </summary>
        private static void OnSearchQueryChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            QueryViewPanel target = (QueryViewPanel)d;
            string oldSearchQuery = (string)e.OldValue;
            string newSearchQuery = target.SearchQuery;
            target.OnSearchQueryChanged(oldSearchQuery, newSearchQuery);
        }

        /// <summary>
        /// Provides derived classes an opportunity to handle changes to the SearchQuery property.
        /// </summary>
        protected virtual void OnSearchQueryChanged(string oldSearchQuery, string newSearchQuery)
        {
        }

        #endregion

        #region PagingMessage

        /// <summary>
        /// PagingMessage Dependency Property
        /// </summary>
        public static readonly DependencyProperty PagingMessageProperty =
            DependencyProperty.Register("PagingMessage", typeof(string), typeof(QueryViewPanel),
                new FrameworkPropertyMetadata((string)null,
                    new PropertyChangedCallback(OnPagingMessageChanged)));

        /// <summary>
        /// Contains the current page index and the total
        /// </summary>
        public string PagingMessage
        {
            get { return (string)GetValue(PagingMessageProperty); }
            set { SetValue(PagingMessageProperty, value); }
        }

        /// <summary>
        /// Handles changes to the PagingMessage property.
        /// </summary>
        private static void OnPagingMessageChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            QueryViewPanel target = (QueryViewPanel)d;
            string oldPagingMessage = (string)e.OldValue;
            string newPagingMessage = target.PagingMessage;
            target.OnPagingMessageChanged(oldPagingMessage, newPagingMessage);
        }

        /// <summary>
        /// Provides derived classes an opportunity to handle changes to the PagingMessage property.
        /// </summary>
        protected virtual void OnPagingMessageChanged(string oldPagingMessage, string newPagingMessage)
        {
        }

        #endregion

        #region TotalRecordsMessage

        /// <summary>
        /// TotalRecordsMessage Dependency Property
        /// </summary>
        public static readonly DependencyProperty TotalRecordsMessageProperty =
            DependencyProperty.Register("TotalRecordsMessage", typeof(string), typeof(QueryViewPanel),
                new FrameworkPropertyMetadata((string)null,
                    new PropertyChangedCallback(OnTotalRecordsMessageChanged)));

        /// <summary>
        /// Contains the total number of records and how many are currently displayed
        /// </summary>
        public string TotalRecordsMessage
        {
            get { return (string)GetValue(TotalRecordsMessageProperty); }
            set { SetValue(TotalRecordsMessageProperty, value); }
        }

        /// <summary>
        /// Handles changes to the TotalRecordsMessage property.
        /// </summary>
        private static void OnTotalRecordsMessageChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            QueryViewPanel target = (QueryViewPanel)d;
            string oldTotalRecordsMessage = (string)e.OldValue;
            string newTotalRecordsMessage = target.TotalRecordsMessage;
            target.OnTotalRecordsMessageChanged(oldTotalRecordsMessage, newTotalRecordsMessage);
        }

        /// <summary>
        /// Provides derived classes an opportunity to handle changes to the TotalRecordsMessage property.
        /// </summary>
        protected virtual void OnTotalRecordsMessageChanged(string oldTotalRecordsMessage, string newTotalRecordsMessage)
        {
        }

        #endregion

        /// <summary>
        /// The bindable datatable
        /// </summary>
        public DataTable RecordTable { get; private set; }

        /// <summary>
        /// Creates a query view panel
        /// </summary>
        /// <param name="rpp">The number of records returned per page</param>
        /// <param name="tr">The total number of records</param>
        /// <param name="tp">The total number of pages of records</param>
        /// <param name="cpi">The current page index</param>
        /// <param name="query">The query used to retrieve the current records</param>
        /// <param name="records">The current records</param>
        public QueryViewPanel(long rpp, long tr, long tp, long cpi, string query, IEnumerable<HDSLRecord> records)
        {
            RecordsPerPage = rpp;
            TotalRecords = tr;
            TotalPages = tp;
            SearchQuery = query;

            RecordTable = new DataTable($"Results for '{query}'", "hdsl.results");
            if (records.Any())
            {
                var first = records.First();
                foreach (var colDef in first.Data)
                {
                    RecordTable.Columns.Add(new DataColumn(colDef.Column, Type.GetType(colDef.ColumnType)) { ReadOnly = true });
                }
            }

            Set(records, cpi);
        }

        /// <summary>
        /// Updates the current records to the new set
        /// </summary>
        /// <param name="records">The new set of records</param>
        /// <param name="newPageIndex">The new page index on display</param>
        public void Set(IEnumerable<HDSLRecord> records, long newPageIndex)
        {
            CurrentPageIndex = newPageIndex;
            RecordTable.Rows.Clear();
            foreach (var r in records)
            {
                var dr = RecordTable.NewRow();
                foreach (var c in r.Data)
                {
                    dr[c.Column] = c.Value;
                }
                RecordTable.Rows.Add(dr);
            }
            RecordTable.AcceptChanges();
        }

        /// <summary>
        /// Updates the paging message for display
        /// </summary>
        private void UpdatePagingMessage()
        {
            if (TotalRecords == 0)
            {
                PagingMessage = $"N/A";
            }
            else
            {
                PagingMessage = $"{CurrentPageIndex} of {TotalPages}";
            }
        }

        /// <summary>
        /// Updates the total record message for display
        /// </summary>
        private void UpdateTotalMessage()
        {
            if (TotalRecords == 0)
            {
                TotalRecordsMessage = $"N/A";
            }
            else
            {
                var rpp = RecordsPerPage > TotalRecords ? TotalRecords : RecordsPerPage;
                TotalRecordsMessage = $"{rpp} of {TotalRecords}";
            }
        }
    }
}
