// Copyright (c) Zain Al-Ahmary.  All rights reserved.
// Licensed under the MIT License, (the "License"); you may not use this file except in compliance with the License. 
// You may obtain a copy of the License at https://mit-license.org/

using ReddWare.Language;
using System;

namespace HDDL.Language.HDSL.Where.Exceptions
{
    /// <summary>
    /// Any error encountered during where clause interpretation will result in one of these
    /// </summary>
    public class WhereClauseException : Exception
    {
        /// <summary>
        /// A summarization of the exception's cause
        /// </summary>
        public WhereClauseExceptionTypes Nature { get; set; }

        /// <summary>
        /// The token row where the error occurred
        /// </summary>
        public int Row { get; set; }

        /// <summary>
        /// The token column where the error occurred
        /// </summary>
        public int Column { get; set; }

        /// <summary>
        /// Creates a WhereClauseException
        /// </summary>
        /// <param name="column">The token column where the error occurred</param>
        /// <param name="row">The token row where the error occurred</param>
        /// <param name="message">The error message</param>
        /// <param name="nature">A summarization of the exception's cause</param>
        public WhereClauseException(int column, int row, string message, WhereClauseExceptionTypes nature) : base(message)
        {
            Column = column;
            Row = row;
            Nature = nature;
        }

        /// <summary>
        /// Converts the exception into a message
        /// </summary>
        /// <returns></returns>
        public LogItemBase AsHDSLLog()
        {
            return new LogItemBase(Column, Row, Message);
        }
    }
}
