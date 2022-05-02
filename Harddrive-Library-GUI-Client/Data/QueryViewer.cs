// Copyright (c) Zain Al-Ahmary.  All rights reserved.
// Licensed under the MIT License, (the "License"); you may not use this file except in compliance with the License. 
// You may obtain a copy of the License at https://mit-license.org/

using HDDL.Data;
using HDDL.Language;
using HDDL.Language.HDSL.Results;
using HDDL.Web;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;

namespace HDDLC.Data
{
    /// <summary>
    /// Allows page-based scrolling through a query's results
    /// </summary>
    class QueryViewer : DependencyObject
    {
        #region SearchQuery

        /// <summary>
        /// SearchQuery Dependency Property
        /// </summary>
        public static readonly DependencyProperty SearchQueryProperty =
            DependencyProperty.Register("SearchQuery", typeof(string), typeof(QueryViewer),
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
            QueryViewer target = (QueryViewer)d;
            string oldSearchQuery = (string)e.OldValue;
            string newSearchQuery = target.SearchQuery;
            target.OnSearchQueryChanged(oldSearchQuery, newSearchQuery);
        }

        /// <summary>
        /// Provides derived classes an opportunity to handle changes to the SearchQuery property.
        /// </summary>
        protected virtual void OnSearchQueryChanged(string oldSearchQuery, string newSearchQuery)
        {
            if (!string.IsNullOrWhiteSpace(newSearchQuery) &&
                oldSearchQuery?.Trim() != newSearchQuery?.Trim())
            {
                ResetWait();
            }
            UpdateAdvancedSearchButtonAvailability();
        }

        #endregion

        #region IsAdvancedQuery

        /// <summary>
        /// IsAdvancedQuery Dependency Property
        /// </summary>
        public static readonly DependencyProperty IsAdvancedQueryProperty =
            DependencyProperty.Register("IsAdvancedQuery", typeof(bool?), typeof(QueryViewer),
                new FrameworkPropertyMetadata((bool?)null,
                    new PropertyChangedCallback(OnIsAdvancedQueryChanged)));

        /// <summary>
        /// If the query is an advanced or simple query
        /// </summary>
        public bool? IsAdvancedQuery
        {
            get { return (bool?)GetValue(IsAdvancedQueryProperty); }
            set { SetValue(IsAdvancedQueryProperty, value); }
        }

        /// <summary>
        /// Handles changes to the IsAdvancedQuery property.
        /// </summary>
        private static void OnIsAdvancedQueryChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            QueryViewer target = (QueryViewer)d;
            bool? oldIsAdvancedQuery = (bool?)e.OldValue;
            bool? newIsAdvancedQuery = target.IsAdvancedQuery;
            target.OnIsAdvancedQueryChanged(oldIsAdvancedQuery, newIsAdvancedQuery);
        }

        /// <summary>
        /// Provides derived classes an opportunity to handle changes to the IsAdvancedQuery property.
        /// </summary>
        protected virtual void OnIsAdvancedQueryChanged(bool? oldIsAdvancedQuery, bool? newIsAdvancedQuery)
        {
            CheckIsPrepared();

            if (newIsAdvancedQuery == true)
            {
                EnableAutoExecute = false;
            }
            else
            {
                EnableAutoExecute = true;
            }
            UpdateAdvancedSearchButtonAvailability();
        }

        #endregion

        #region IsAdvancedSearchAvailable

        /// <summary>
        /// IsAdvancedSearchAvailable Dependency Property
        /// </summary>
        public static readonly DependencyProperty IsAdvancedSearchAvailableProperty =
            DependencyProperty.Register("IsAdvancedSearchAvailable", typeof(bool), typeof(QueryViewer),
                new FrameworkPropertyMetadata((bool)false,
                    new PropertyChangedCallback(OnIsAdvancedSearchAvailableChanged)));

        /// <summary>
        /// Whether or not the advanced search button should be enabled
        /// </summary>
        public bool IsAdvancedSearchAvailable
        {
            get { return (bool)GetValue(IsAdvancedSearchAvailableProperty); }
            set { SetValue(IsAdvancedSearchAvailableProperty, value); }
        }

        /// <summary>
        /// Handles changes to the IsAdvancedSearchAvailable property.
        /// </summary>
        private static void OnIsAdvancedSearchAvailableChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            QueryViewer target = (QueryViewer)d;
            bool oldIsAdvancedSearchAvailable = (bool)e.OldValue;
            bool newIsAdvancedSearchAvailable = target.IsAdvancedSearchAvailable;
            target.OnIsAdvancedSearchAvailableChanged(oldIsAdvancedSearchAvailable, newIsAdvancedSearchAvailable);
        }

        /// <summary>
        /// Provides derived classes an opportunity to handle changes to the IsAdvancedSearchAvailable property.
        /// </summary>
        protected virtual void OnIsAdvancedSearchAvailableChanged(bool oldIsAdvancedSearchAvailable, bool newIsAdvancedSearchAvailable)
        {
        }

        #endregion

        #region IsBusy

        /// <summary>
        /// IsBusy Dependency Property
        /// </summary>
        public static readonly DependencyProperty IsBusyProperty =
            DependencyProperty.Register("IsBusy", typeof(bool), typeof(QueryViewer),
                new FrameworkPropertyMetadata((bool)false,
                    new PropertyChangedCallback(OnIsBusyChanged)));

        /// <summary>
        /// If the viewer is querying the service for more records
        /// </summary>
        public bool IsBusy
        {
            get { return (bool)GetValue(IsBusyProperty); }
            set { SetValue(IsBusyProperty, value); }
        }

        /// <summary>
        /// Handles changes to the IsBusy property.
        /// </summary>
        private static void OnIsBusyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            QueryViewer target = (QueryViewer)d;
            bool oldIsBusy = (bool)e.OldValue;
            bool newIsBusy = target.IsBusy;
            target.OnIsBusyChanged(oldIsBusy, newIsBusy);
        }

        /// <summary>
        /// Provides derived classes an opportunity to handle changes to the IsBusy property.
        /// </summary>
        protected virtual void OnIsBusyChanged(bool oldIsBusy, bool newIsBusy)
        {
            UpdateAdvancedSearchButtonAvailability();
        }

        #endregion

        #region IsLoaded

        /// <summary>
        /// IsLoaded Dependency Property
        /// </summary>
        public static readonly DependencyProperty IsLoadedProperty =
            DependencyProperty.Register("IsLoaded", typeof(bool), typeof(QueryViewer),
                new FrameworkPropertyMetadata((bool)false,
                    new PropertyChangedCallback(OnIsLoadedChanged)));

        /// <summary>
        /// Whether or not the viewer has everything required to function
        /// </summary>
        public bool IsLoaded
        {
            get { return (bool)GetValue(IsLoadedProperty); }
            set { SetValue(IsLoadedProperty, value); }
        }

        /// <summary>
        /// Handles changes to the IsLoaded property.
        /// </summary>
        private static void OnIsLoadedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            QueryViewer target = (QueryViewer)d;
            bool oldIsLoaded = (bool)e.OldValue;
            bool newIsLoaded = target.IsLoaded;
            target.OnIsLoadedChanged(oldIsLoaded, newIsLoaded);
        }

        /// <summary>
        /// Provides derived classes an opportunity to handle changes to the IsLoaded property.
        /// </summary>
        protected virtual void OnIsLoadedChanged(bool oldIsLoaded, bool newIsLoaded)
        {
        }

        #endregion

        #region Connection

        /// <summary>
        /// Connection Dependency Property
        /// </summary>
        public static readonly DependencyProperty ConnectionProperty =
            DependencyProperty.Register("Connection", typeof(HDSLConnection), typeof(QueryViewer),
                new FrameworkPropertyMetadata((HDSLConnection)null,
                    new PropertyChangedCallback(OnConnectionChanged)));

        /// <summary>
        /// The HDSL service connection this viewer uses
        /// </summary>
        public HDSLConnection Connection
        {
            get { return (HDSLConnection)GetValue(ConnectionProperty); }
            set { SetValue(ConnectionProperty, value); }
        }

        /// <summary>
        /// Handles changes to the Connection property.
        /// </summary>
        private static void OnConnectionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            QueryViewer target = (QueryViewer)d;
            HDSLConnection oldConnection = (HDSLConnection)e.OldValue;
            HDSLConnection newConnection = target.Connection;
            target.OnConnectionChanged(oldConnection, newConnection);
        }

        /// <summary>
        /// Provides derived classes an opportunity to handle changes to the Connection property.
        /// </summary>
        protected virtual void OnConnectionChanged(HDSLConnection oldConnection, HDSLConnection newConnection)
        {
            CheckIsPrepared();
        }

        #endregion

        #region EnableAutoExecute

        /// <summary>
        /// EnableAutoExecute Dependency Property
        /// </summary>
        public static readonly DependencyProperty EnableAutoExecuteProperty =
            DependencyProperty.Register("EnableAutoExecute", typeof(bool), typeof(QueryViewer),
                new FrameworkPropertyMetadata((bool)false,
                    new PropertyChangedCallback(OnEnableAutoExecuteChanged)));

        /// <summary>
        /// Whether or not to automatically perform a query after waiting a set amount of time
        /// </summary>
        public bool EnableAutoExecute
        {
            get { return (bool)GetValue(EnableAutoExecuteProperty); }
            set { SetValue(EnableAutoExecuteProperty, value); }
        }

        /// <summary>
        /// Handles changes to the EnableAutoExecute property.
        /// </summary>
        private static void OnEnableAutoExecuteChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            QueryViewer target = (QueryViewer)d;
            bool oldEnableAutoExecute = (bool)e.OldValue;
            bool newEnableAutoExecute = target.EnableAutoExecute;
            target.OnEnableAutoExecuteChanged(oldEnableAutoExecute, newEnableAutoExecute);
        }

        /// <summary>
        /// Provides derived classes an opportunity to handle changes to the EnableAutoExecute property.
        /// </summary>
        protected virtual void OnEnableAutoExecuteChanged(bool oldEnableAutoExecute, bool newEnableAutoExecute)
        {
            CheckIsPrepared();
        }

        #endregion

        /// <summary>
        /// The current query result panels
        /// </summary>
        public ObservableCollection<QueryViewPanel> Panels { get; private set; }

        private Timer _prequeryDelayTimer;
        private HDSLWebClient _client;

        /// <summary>
        /// Create a HDSLQueryViewer
        /// </summary>
        public QueryViewer()
        {
            _prequeryDelayTimer = new Timer(500);
            _prequeryDelayTimer.Enabled = false;
            _prequeryDelayTimer.AutoReset = false;
            _prequeryDelayTimer.Elapsed += PrequeryDelayExpired;

            Panels = new ObservableCollection<QueryViewPanel>();
        }

        #region Utility

        /// <summary>
        /// Updates the ability to click the advanced search  button paired with the advanced text box
        /// </summary>
        private void UpdateAdvancedSearchButtonAvailability()
        {
            IsAdvancedSearchAvailable =
                IsAdvancedQuery == true &&
                !string.IsNullOrWhiteSpace(SearchQuery) &&
                !IsBusy;
        }

        /// <summary>
        /// Directly triggers the query, skipping the wait phase
        /// </summary>
        public void ExecuteQuery()
        {
            if (IsLoaded && !string.IsNullOrEmpty(SearchQuery))
            {
                PerformQuery();
            }
        }

        /// <summary>
        /// Updates the IsLoaded dependency property
        /// </summary>
        private void CheckIsPrepared()
        {
            IsLoaded =
                Connection != null &&
                Connection.IsValid == true &&
                IsAdvancedQuery.HasValue;
        }

        /// <summary>
        /// Restarts the delay on an awaiting query
        /// </summary>
        private void ResetWait()
        {
            _prequeryDelayTimer.Stop();
            StartWait();
        }

        /// <summary>
        /// Cancels an awaiting query
        /// </summary>
        private void CancelWait()
        {
            _prequeryDelayTimer.Stop();
        }

        /// <summary>
        /// Initiates the delay prior to a query
        /// </summary>
        private void StartWait()
        {
            if (EnableAutoExecute &&
                !IsBusy &&
                Connection != null &&
                Connection.IsValid == true &&
                !string.IsNullOrWhiteSpace(SearchQuery) &&
                SearchQuery.Length > 2)
            {
                _prequeryDelayTimer.Start();
            }
        }

        /// <summary>
        /// Takes a query and removes the paging clause
        /// </summary>
        /// <param name="query">The query to modify</param>
        /// <returns></returns>
        private string StripPaging(string query)
        {
            return Regex.Replace(query, @"[Pp][Aa][Gg][Ee] ?\d+;?", string.Empty);
        }

        /// <summary>
        /// Processes a set of error messages for display in the UI grid
        /// </summary>
        /// <param name="errors">The errors to process</param>
        private void ProcessErrors(IEnumerable<LogItemBase> errors)
        {
            var errorRecords = new List<HDSLRecord>();
            // convert the errors into an HDSLRecord array
            foreach (var error in errors)
            {
                errorRecords.Add(new HDSLRecord(
                    new HDSLValueItem[]
                    {
                        new HDSLValueItem("Column", error.Column.GetType().ToString(), error.Column),
                        new HDSLValueItem("Row", error.Row.GetType().ToString(), error.Row),
                        new HDSLValueItem("Error", error.Message.GetType().ToString(), error.Message),
                    }));
            }

            Dispatcher.Invoke(() =>
            {
                Panels.Clear();
                Panels.Add(
                    new QueryViewPanel(
                        this,
                        0,
                        0,
                        0,
                        0,
                        string.Empty,
                        errorRecords
                    ));

                _client = null;
                IsBusy = false;
            });
        }

        /// <summary>
        /// Updates the given requester with the new page
        /// </summary>
        /// <param name="requester">The requesting QueryViewPanel</param>
        /// <param name="requestedIndex">The desired page index for the current query</param>
        public void GetPageIndex(QueryViewPanel requester, long requestedIndex)
        {
            IsBusy = true;
            _client = Connection.AsConnection();

            if (IsAdvancedQuery == true)
            {
                var query = $"{StripPaging(requester.SearchQuery)} page {requestedIndex};";
                Task<HDSLOutcomeSet>.Factory.StartNew(() => _client.Query(query))
                .ContinueWith((tsk) =>
                {
                    if (tsk.Result.Errors.Length == 0)
                    {
                        Dispatcher.Invoke(() =>
                        {
                            // add new and update existing panels
                            if (tsk.Result.Results.Count == 1)
                            {
                                var r = tsk.Result.Results.Single();
                                requester.Set(r.Records, r.PageIndex);
                            }
                            else
                            {
                                ProcessErrors(new LogItemBase[]
                                {
                                    new LogItemBase(-1, -1, "Multiple result sets returned for single query.")
                                }); ;
                            }

                            _client = null;
                            IsBusy = false;
                        });
                    }
                    else
                    {
                        ProcessErrors(tsk.Result.Errors);
                    }
                });
            }
            else
            {
                var query = requester.SearchQuery;
                var work = Task<HDSLRecord[]>.Factory.StartNew(() => _client.Search(query, Convert.ToInt32(requestedIndex)))
                    .ContinueWith((tsk) =>
                    {
                        var paging = tsk.Result.Where(r => r is DataHandlerPagingInformation).SingleOrDefault() as DataHandlerPagingInformation;
                        if (paging == null)
                        {
                            ProcessErrors(new LogItemBase[] { new LogItemBase(-1, -1, $"Failed to query system for '{query}'.") });
                        }
                        else
                        {
                            Dispatcher.Invoke(() =>
                            {
                                var existing = (from p in Panels where p.SearchQuery == query select p).SingleOrDefault();
                                if (existing == null)
                                {
                                    Panels.Clear();
                                    Panels.Add(
                                        new QueryViewPanel(
                                            this,
                                            paging.RecordsPerPage,
                                            paging.TotalRecords,
                                            paging.TotalPages,
                                            paging.PageIndex,
                                            query,
                                            tsk.Result.Where(r => !(r is DataHandlerPagingInformation))
                                        ));

                                    _client = null;
                                    IsBusy = false;
                                }
                                else
                                {
                                    existing.Set(tsk.Result.Where(r => !(r is DataHandlerPagingInformation)), paging.PageIndex);
                                }

                                _client = null;
                                IsBusy = false;
                            });
                        }
                    });
            }
        }

        /// <summary>
        /// Executes the search / HDSL query provided through "SearchQuery"
        /// </summary>
        private void PerformQuery()
        {
            var query = string.Empty;
            Dispatcher.Invoke(() =>
            {
                IsBusy = true;
                _client = Connection.AsConnection();
                query = SearchQuery;

                if (IsAdvancedQuery == true)
                {
                    Task<HDSLOutcomeSet>.Factory.StartNew(() => _client.Query(query))
                        .ContinueWith((tsk) =>
                        {
                            if (tsk.Result.Errors.Length == 0)
                            {
                                Dispatcher.Invoke(() =>
                                {
                                    // add new and update existing panels
                                    foreach (var r in tsk.Result.Results)
                                    {
                                        var q = StripPaging(r.Statement);
                                        var existing = (from p in Panels where p.SearchQuery == q select p).SingleOrDefault();
                                        if (existing == null)
                                        {
                                            Panels.Add(
                                                new QueryViewPanel(
                                                    this,
                                                    r.RecordsPerPage,
                                                    r.TotalRecords,
                                                    r.TotalRecords / r.RecordsPerPage,
                                                    r.PageIndex,
                                                    q,
                                                    r.Records
                                                ));
                                        }
                                        else
                                        {
                                            existing.Set(r.Records, r.PageIndex);
                                        }
                                    }

                                    // check all panels against the query results
                                    // panels that are not present in the results are removed
                                    var removals = new List<QueryViewPanel>();
                                    foreach (var p in Panels)
                                    {
                                        var remove = !(from r in tsk.Result.Results where StripPaging(r.Statement) == p.SearchQuery select r).Any();
                                        if (remove)
                                        {
                                            removals.Add(p);
                                        }
                                    }
                                    removals.ForEach(r => Panels.Remove(r));

                                    _client = null;
                                    IsBusy = false;
                                });
                            }
                            else
                            {
                                ProcessErrors(tsk.Result.Errors);
                            }
                        });
                }
                else
                {
                    var work = Task<HDSLRecord[]>.Factory.StartNew(() => _client.Search(query, 0))
                        .ContinueWith((tsk) =>
                        {
                            var paging = tsk.Result.Where(r => r is DataHandlerPagingInformation).SingleOrDefault() as DataHandlerPagingInformation;
                            if (paging == null)
                            {
                                ProcessErrors(new LogItemBase[] { new LogItemBase(-1, -1, $"Failed to query system for '{query}'.") });
                            }
                            else
                            {
                                Dispatcher.Invoke(() =>
                                {
                                    var existing = (from p in Panels where p.SearchQuery == query select p).SingleOrDefault();
                                    if (existing == null)
                                    {
                                        Panels.Clear();
                                        Panels.Add(
                                            new QueryViewPanel(
                                                this,
                                                paging.RecordsPerPage,
                                                paging.TotalRecords,
                                                paging.TotalPages,
                                                paging.PageIndex,
                                                query,
                                                tsk.Result.Where(r => !(r is DataHandlerPagingInformation))
                                            ));

                                        _client = null;
                                        IsBusy = false;
                                    }
                                    else
                                    {
                                        existing.Set(tsk.Result.Where(r => !(r is DataHandlerPagingInformation)), paging.PageIndex);
                                    }
                                });
                            }
                        });
                }
            });
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// Triggered when the pre-query delay expires
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PrequeryDelayExpired(object sender, ElapsedEventArgs e)
        {
            CancelWait();

            PerformQuery();
        }

        #endregion
    }
}
