// Copyright (c) Zain Al-Ahmary.  All rights reserved.
// Licensed under the MIT License, (the "License"); you may not use this file except in compliance with the License. 
// You may obtain a copy of the License at https://mit-license.org/

using HDDL.Data;
using HDDL.Language.HDSL.Results;

namespace HDDL.Scanning.Monitoring
{
    /// <summary>
    /// Represents something that happened in/to a symphony-type instance
    /// </summary>
    class SymphonyEvent : EventMessageBase
    {
        /// <summary>
        /// The type of event represented
        /// </summary>
        public SymphonyEventTypes EventType { get; internal set; }

        /// <summary>
        /// The ward or watch that triggered the check
        /// </summary>
        public HDDLRecordBase CheckTrigger { get; internal set; }

        /// <summary>
        /// The outcome of the triggered check
        /// </summary>
        public HDSLOutcome CheckOutcome { get; internal set; }

        /// <summary>
        /// The symphony-type's new state
        /// </summary>
        public SymphonyStates NewState { get; internal set; }

        /// <summary>
        /// The symphony-type's old state
        /// </summary>
        public SymphonyStates OldState { get; internal set; }

        /// <summary>
        /// Creates an event for a state change
        /// </summary>
        /// <param name="source"></param>
        /// <param name="oldState"></param>
        /// <param name="newState"></param>
        public SymphonyEvent(SymphonyBase source, SymphonyStates oldState, SymphonyStates newState) : base(source)
        {
            EventType = SymphonyEventTypes.StateChange;
            NewState = newState;
            OldState = oldState;
        }

        /// <summary>
        /// Creates an event for an integrity check start
        /// </summary>
        /// <param name="source"></param>
        /// <param name="check"></param>
        public SymphonyEvent(SymphonyBase source, HDDLRecordBase check) : base(source)
        {
            EventType = SymphonyEventTypes.ScanStarts;
            CheckTrigger = check;
        }

        /// <summary>
        /// Creates an event for an integrity check end
        /// </summary>
        /// <param name="source"></param>
        /// <param name="check"></param>
        /// <param name="outcome"></param>
        public SymphonyEvent(SymphonyBase source, HDDLRecordBase check, HDSLOutcome outcome) : base(source)
        {
            EventType = SymphonyEventTypes.ScanEnds;
            CheckTrigger = check;
            CheckOutcome = outcome;
        }
    }
}
