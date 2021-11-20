// Copyright (c) Zain Al-Ahmary.  All rights reserved.
// Licensed under the MIT License, (the "License"); you may not use this file except in compliance with the License. 
// You may obtain a copy of the License at https://mit-license.org/

using HDDL.Data;
using HDDL.HDSL;
using HDDL.HDSL.Results;
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
    class IntegrityMonitorSymphony : ReporterBase
    {
        public delegate void IntegrityScanStartsDelegate(IntegrityMonitorSymphony originator, WardItem integrityCheck);
        public delegate void IntegrityScanEndsDelegate(IntegrityMonitorSymphony originator, WardItem integrityCheck, HDSLOutcome result);
        public delegate void IntegrityScanStateChanged(IntegrityMonitorSymphony originator, IntegrityMonitorSymphonyStates newState, IntegrityMonitorSymphonyStates oldState);

        /// <summary>
        /// Occurs when the state property changes
        /// </summary>
        public event IntegrityScanStateChanged StateChanged;

        /// <summary>
        /// Occurs when an integrity check begins
        /// </summary>
        public event IntegrityScanStartsDelegate ScanStarted;

        /// <summary>
        /// Occurs when an integrity check concludes
        /// </summary>
        public event IntegrityScanEndsDelegate ScanEnded;

        private DataHandler _dh;

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

        private IntegrityMonitorSymphonyStates _state;
        /// <summary>
        /// The Integrity Monitor Symphony's current state
        /// </summary>
        public IntegrityMonitorSymphonyStates State
        {
            get
            {
                return _state;
            }
            private set
            {
                if (_state != value)
                {
                    var orig = _state;
                    _state = value;
                    StateChanged?.Invoke(this, _state, orig);
                }
            }
        }

        /// <summary>
        /// Creates an Integrity Monitor Symphony
        /// </summary>
        /// <param name="dh">The data handler to use</param>
        /// <param name="messenging">The kinds and styles of messages that will be relayed</param>
        public IntegrityMonitorSymphony(DataHandler dh, MessagingModes messenging) : base(messenging)
        {
            _dh = dh;
            State = IntegrityMonitorSymphonyStates.Idle;
        }

        /// <summary>
        /// Starts the Integrity Monitor Symphony
        /// </summary>
        public void Start()
        {
            if (State == IntegrityMonitorSymphonyStates.Idle)
            {
                State = IntegrityMonitorSymphonyStates.Active;
                _worker = Task.Run(() => WorkerMethod());
            }
        }

        /// <summary>
        /// Stops the Integrity Monitor Symphony and waits until termination completes
        /// </summary>
        public void Stop()
        {
            if (State == IntegrityMonitorSymphonyStates.Active)
            {
                State = IntegrityMonitorSymphonyStates.Stopping;
                _worker.Wait();
                State = IntegrityMonitorSymphonyStates.Idle;
            }
        }

        /// <summary>
        /// The worker task's body
        /// </summary>
        private void WorkerMethod()
        {
            try
            {
                while (State == IntegrityMonitorSymphonyStates.Active)
                {
                    // get the first scan that's due to be executed
                    var due = (from w in Wards where w.IsDue() select w).FirstOrDefault();
                    if (due != null)
                    {
                        ScanStarted?.Invoke(this, due);
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
                        ScanEnded?.Invoke(this, due, result.Results.FirstOrDefault());
                    }

                    // wait 10 seconds before checking for another integrity check's need
                    Task.Delay(10000).Wait();
                }
            }
            catch (Exception ex)
            {
                Error($"The integrity monitoring symphony encountered an error and requires recycling.", ex);
                State = IntegrityMonitorSymphonyStates.Faulted;
            }
        }
    }
}
