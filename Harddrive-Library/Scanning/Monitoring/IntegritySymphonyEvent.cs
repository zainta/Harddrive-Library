// Copyright (c) Zain Al-Ahmary.  All rights reserved.
// Licensed under the MIT License, (the "License"); you may not use this file except in compliance with the License. 
// You may obtain a copy of the License at https://mit-license.org/

using HDDL.Data;
using HDDL.Language.HDSL.Results;

namespace HDDL.Scanning.Monitoring
{
    /// <summary>
    /// Represents something that happened in/to an Integrity Symphony instance
    /// </summary>
    class IntegritySymphonyEvent : EventMessageBase
    {
        /// <summary>
        /// The type of event represented
        /// </summary>
        public IntegritySymphonyEventTypes EventType { get; internal set; }

        /// <summary>
        /// The ward that triggered the integrity check
        /// </summary>
        public WardItem CheckTrigger { get; internal set; }

        /// <summary>
        /// The outcome of the triggered check
        /// </summary>
        public HDSLOutcome CheckOutcome { get; internal set; }

        /// <summary>
        /// The integrity symphony's new state
        /// </summary>
        public IntegrityMonitorSymphonyStates NewState { get; internal set; }

        /// <summary>
        /// The integrity symphony's old state
        /// </summary>
        public IntegrityMonitorSymphonyStates OldState { get; internal set; }

        /// <summary>
        /// Creates an event for a state change
        /// </summary>
        /// <param name="source"></param>
        /// <param name="oldState"></param>
        /// <param name="newState"></param>
        public IntegritySymphonyEvent(IntegrityMonitorSymphony source, IntegrityMonitorSymphonyStates oldState, IntegrityMonitorSymphonyStates newState) : base(source)
        {
            EventType = IntegritySymphonyEventTypes.StateChange;
            NewState = newState;
            OldState = oldState;
        }

        /// <summary>
        /// Creates an event for an integrity check start
        /// </summary>
        /// <param name="source"></param>
        /// <param name="check"></param>
        public IntegritySymphonyEvent(IntegrityMonitorSymphony source, WardItem check) : base(source)
        {
            EventType = IntegritySymphonyEventTypes.ScanStarts;
            CheckTrigger = check;
        }

        /// <summary>
        /// Creates an event for an integrity check end
        /// </summary>
        /// <param name="source"></param>
        /// <param name="check"></param>
        /// <param name="outcome"></param>
        public IntegritySymphonyEvent(IntegrityMonitorSymphony source, WardItem check, HDSLOutcome outcome) : base(source)
        {
            EventType = IntegritySymphonyEventTypes.ScanEnds;
            CheckTrigger = check;
            CheckOutcome = outcome;
        }
    }
}
