// Copyright (c) Zain Al-Ahmary.  All rights reserved.
// Licensed under the MIT License, (the "License"); you may not use this file except in compliance with the License. 
// You may obtain a copy of the License at https://mit-license.org/

namespace HDDL.Scanning.Monitoring
{
    /// <summary>
    /// Base class for all event messages
    /// </summary>
    class EventMessageBase
    {
        /// <summary>
        /// The reporter base that triggered the event
        /// </summary>
        public ReporterBase Source { get; internal set; }

        /// <summary>
        /// Creates a default event message
        /// </summary>
        /// <param name="source">The reporter base that triggered it</param>
        public EventMessageBase(ReporterBase source)
        {
            Source = source;
        }
    }
}
