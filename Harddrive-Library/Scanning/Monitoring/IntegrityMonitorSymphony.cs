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
    /// Manages scheduling of Wards and notification of when they are due
    /// </summary>
    class IntegrityMonitorSymphony : SymphonyBase
    {
        /// <summary>
        /// The available wards in the database
        /// </summary>
        private IEnumerable<WardItem> Wards
        {
            get
            {
                if (_dh != null)
                {
                    // return only the wards that do not overlap perfectly
                    return _dh.GetWards().Distinct(new WardEqualityComparer());
                }
                else
                {
                    return new WardItem[] { };
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
        public IntegrityMonitorSymphony(DataHandler dh, MessagingModes messenging) : base(dh, messenging)
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
                    var due = (from w in Wards where w.IsDue() select w).FirstOrDefault();
                    if (due != null)
                    {
                        Events.Enqueue(new SymphonyEvent(this, due));
                        var result = HDSLProvider.ExecuteCode(due.HDSL, _dh);

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
                                Inform($"Successfully executed integrity scan '{due.HDSL}'.");
                            }
                        }

                        // update the scan's due date to now + interval.
                        // This prevents scans from being spammed.
                        due.NextScan = DateTime.Now.Add(due.Interval);
                        _dh.Update(due);
                        _dh.WriteWards();
                        Events.Enqueue(new SymphonyEvent(this, due, result.Results.FirstOrDefault()));
                    }

                    // wait 10 seconds before checking for another integrity check's need
                    Task.Delay(10000).Wait();
                }
            }
            catch (Exception ex)
            {
                Error($"The integrity monitoring symphony encountered an error and requires recycling.", ex);
                State = SymphonyStates.Faulted;
            }
        }
    }
}
