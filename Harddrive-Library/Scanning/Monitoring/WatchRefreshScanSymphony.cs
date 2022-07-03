// Copyright (c) Zain Al-Ahmary.  All rights reserved.
// Licensed under the MIT License, (the "License"); you may not use this file except in compliance with the License. 
// You may obtain a copy of the License at https://mit-license.org/

using HDDL.Data;
using HDDL.Language.HDSL;
using HDDL.Language.HDSL.Results;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HDDL.Scanning.Monitoring
{
    /// <summary>
    /// Manages scheduling of Watches' refresh scans
    /// </summary>
    class WatchRefreshScanSymphony : SymphonyBase
    {
        private const string HDSL_PATH_SLUG = "[path]";
        private readonly string HDSL_SCAN = $"scan quiet @'{HDSL_PATH_SLUG}';";

        /// <summary>
        /// The available watches in the database
        /// </summary>
        private IEnumerable<WatchItem> Watches
        {
            get
            {
                if (_dh != null)
                {
                    // return only the wards that do not overlap perfectly
                    return _dh.GetWatches().Distinct(new WatchEqualityComparer());
                }
                else
                {
                    return new WatchItem[] { };
                }
            }
        }

        /// <summary>
        /// The task monitoring the wards
        /// </summary>
        private Task _worker;

        /// <summary>
        /// Creates an Integrity Monitor Symphony
        /// </summary>
        /// <param name="dh">The data handler to use</param>
        /// <param name="messenging">The kinds and styles of messages that will be relayed</param>
        public WatchRefreshScanSymphony(DataHandler dh, MessagingModes messenging) : base(dh, messenging)
        {
        }

        /// <summary>
        /// Starts the Integrity Monitor Symphony
        /// </summary>
        public void Start()
        {
            if (State == SymphonyStates.Idle)
            {
                State = SymphonyStates.Active;
                _worker = Task.Run(() => WorkerMethod());
            }
        }

        /// <summary>
        /// Stops the Integrity Monitor Symphony and waits until termination completes
        /// </summary>
        public void Stop()
        {
            if (State == SymphonyStates.Active)
            {
                State = SymphonyStates.Stopping;
                _worker.Wait();
                State = SymphonyStates.Idle;
            }
        }

        /// <summary>
        /// The worker task's body
        /// </summary>
        private void WorkerMethod()
        {
            try
            {
                while (State == SymphonyStates.Active)
                {
                    // get the first scan that's due to be executed
                    var due = (from w in Watches where w.IsDue() select w).FirstOrDefault();
                    if (due != null)
                    {
                        Events.Enqueue(new SymphonyEvent(this, due));
                        var hdsl = HDSL_SCAN.Replace(HDSL_PATH_SLUG, due.Path);
                        var result = HDSLProvider.ExecuteCode(hdsl, _dh);

                        // handle the results
                        if (result.Errors.Length > 0)
                        {
                            var sb = new StringBuilder();
                            foreach (var error in result.Errors)
                            {
                                sb.AppendLine(error.ToString());
                            }

                            Warn(sb.ToString());
                        }
                        else
                        {
                            var set = result.Results.FirstOrDefault();
                            if (set == null)
                            {
                                Warn($"Unexpected result set returned. Type '{result.Results.FirstOrDefault().GetType().FullName}' was returned instead of '{typeof(HDSLOutcomeSet).FullName}'.");
                            }
                            else
                            {
                                Inform($"Successfully executed refresh scan '{due.Path}'.");
                            }
                        }

                        // update the scan's due date to be 24 hours from the previous.
                        // This prevents scans from being spammed.
                        due.Increment();
                        _dh.Update(due);
                        _dh.WriteWatches();
                        Events.Enqueue(new SymphonyEvent(this, due, result.Results.FirstOrDefault()));
                    }

                    // wait 10 seconds before checking for another watch's need
                    Task.Delay(10000).Wait();
                }
            }
            catch (Exception ex)
            {
                Error($"The watch refresh monitoring symphony encountered an error and requires recycling.", ex);
                State = SymphonyStates.Faulted;
            }
        }
    }
}
