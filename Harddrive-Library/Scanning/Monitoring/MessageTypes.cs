// Copyright (c) Zain Al-Ahmary.  All rights reserved.
// Licensed under the MIT License, (the "License"); you may not use this file except in compliance with the License. 
// You may obtain a copy of the License at https://mit-license.org/

namespace HDDL.Scanning.Monitoring
{
    /// <summary>
    /// The types of message represented by the MessageBundle
    /// </summary>
    public enum MessageTypes
    {
        /// <summary>
        /// A message that relays information
        /// </summary>
        Information,
        /// <summary>
        /// A message that relays informational details that might be excessive
        /// </summary>
        VerboseInformation,
        /// <summary>
        /// An error with no parmanent consequences
        /// </summary>
        Warning,
        /// <summary>
        /// An irrecoverable issue
        /// </summary>
        Error
    }
}
