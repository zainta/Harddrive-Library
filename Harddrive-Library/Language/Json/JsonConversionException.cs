// Copyright (c) Zain Al-Ahmary.  All rights reserved.
// Licensed under the MIT License, (the "License"); you may not use this file except in compliance with the License. 
// You may obtain a copy of the License at https://mit-license.org/

using System;

namespace HDDL.Language.Json
{
    /// <summary>
    /// Thrown when an error is encountered during json conversion
    /// </summary>
    public class JsonConversionException : Exception
    {
        public LogItemBase[] Issues { get; private set; }

        /// <summary>
        /// Create a JsonConversionException
        /// </summary>
        /// <param name="message">The error message</param>
        /// <param name="issues">The issues encountered</param>
        public JsonConversionException(string message, LogItemBase[] issues) : base(message)
        {
            Issues = issues;
        }

        /// <summary>
        /// Create a JsonConversionException
        /// </summary>
        /// <param name="message">The error message</param>
        /// <param name="inner">The originating exception</param>
        public JsonConversionException(string message, Exception inner) : base(message, inner)
        {
            Issues = Array.Empty<LogItemBase>();
        }
    }
}
