// Copyright (c) Zain Al-Ahmary.  All rights reserved.
// Licensed under the MIT License, (the "License"); you may not use this file except in compliance with the License. 
// You may obtain a copy of the License at https://mit-license.org/

namespace HDDL.Scanning
{
    /// <summary>
    /// Defines the types of scan events that can occur during a scan operation
    /// </summary>
    public enum ScanEventType
    {
        /// <summary>
        /// Occurs when an error occurs during an add
        /// </summary>
        AddRequired,
        /// <summary>
        /// Occurs an add operation is queued
        /// </summary>
        UpdateRequired,
        /// <summary>
        /// Occurs an delete operation is queued
        /// </summary>
        KeyNotDeleted,
        /// <summary>
        /// Occurs when an error occurs during the record operation
        /// </summary>
        UnknownError,
        /// <summary>
        /// Occurs when an error related specifically to the database occurs
        /// </summary>
        DatabaseError
    }
}
