// Copyright (c) Zain Al-Ahmary.  All rights reserved.
// Licensed under the MIT License, (the "License"); you may not use this file except in compliance with the License. 
// You may obtain a copy of the License at https://mit-license.org/

using HDDL.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HDDL.Scanning.Monitoring
{
    /// <summary>
    /// Base class for all symphony-types
    /// </summary>
    abstract class SymphonyBase : ReporterBase
    {
        protected readonly DataHandler _dh;

        private SymphonyStates _state;
        /// <summary>
        /// The Symphony-type's current state
        /// </summary>
        public SymphonyStates State
        {
            get
            {
                return _state;
            }
            protected set
            {
                if (_state != value)
                {
                    var orig = _state;
                    _state = value;
                    Events.Enqueue(new SymphonyEvent(this, orig, _state));
                }
            }
        }

        /// <summary>
        /// Creates a SymphonyBase
        /// </summary>
        /// <param name="messenging">The kinds and styles of messages that will be relayed</param>
        /// <param name="dh">The data handler to use</param>
        public SymphonyBase(DataHandler dh, MessagingModes messenging) : base(messenging)
        {
            _dh = dh;
            State = SymphonyStates.Idle;
        }
    }
}
