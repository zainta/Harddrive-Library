// Copyright (c) Zain Al-Ahmary.  All rights reserved.
// Licensed under the MIT License, (the "License"); you may not use this file except in compliance with the License. 
// You may obtain a copy of the License at https://mit-license.org/

namespace HDDL.Language
{
    /// <summary>
    /// Base class for error tracking classes
    /// </summary>
    public class LogItemBase
    {
        /// <summary>
        /// A description of what happened
        /// </summary>
        public string Message { get; private set; }

        /// <summary>
        /// The column where the error occurred
        /// </summary>
        public int Column { get; private set; }

        /// <summary>
        /// The row where the error occurred
        /// </summary>
        public int Row { get; private set; }

        /// <summary>
        /// Create a log entry
        /// </summary>
        public LogItemBase()
        {
            Message = string.Empty;
            Column = 0;
            Row = 0;
        }

        /// <summary>
        /// Create a log entry
        /// </summary>
        /// <param name="message">The error message</param>
        /// <param name="col">The column where the error occurred</param>
        /// <param name="row">The row where the error occurred</param>
        public LogItemBase(int col, int row, string message)
        {
            Message = message;
            Column = col;
            Row = row;
        }

        /// <summary>
        /// Converts the error message into a single line of text
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"Syntax Error at line {Row:00}, column {Column:00}: {Message}";
        }
    }
}
