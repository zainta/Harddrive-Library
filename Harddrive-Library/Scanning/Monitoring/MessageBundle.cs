// Copyright (c) Zain Al-Ahmary.  All rights reserved.
// Licensed under the MIT License, (the "License"); you may not use this file except in compliance with the License. 
// You may obtain a copy of the License at https://mit-license.org/

using System;

namespace HDDL.Scanning.Monitoring
{
    /// <summary>
    /// Encapsulates a message from a ReporterBase derivative
    /// </summary>
    public class MessageBundle
    {
        /// <summary>
        /// The ReporterBase the message came from
        /// </summary>
        public ReporterBase Origin { get; set; }

        /// <summary>
        /// The message
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// The exception, if relevant
        /// </summary>
        public Exception Error { get; set; }

        /// <summary>
        /// The type of message
        /// </summary>
        public MessageTypes Type { get; set; }

        /// <summary>
        /// Creates a message bundle
        /// </summary>
        /// <param name="source">The ReporterBase instance who created the message bundle</param>
        /// <param name="message">The message</param>
        /// <param name="type">The type of message - defaults to information</param>
        public MessageBundle(ReporterBase source, string message, MessageTypes type = MessageTypes.Information)
        {
            Origin = source;
            Message = message;
            Error = null;
            Type = type;
        }

        /// <summary>
        /// Creates an error message bundle
        /// </summary>
        /// <param name="source">The ReporterBase instance who created the message bundle</param>
        /// <param name="message">The message</param>
        /// <param name="ex">The exception</param>
        public MessageBundle(ReporterBase source, string message, Exception ex)
        {
            Origin = source;
            Message = message;
            Error = ex;
            Type = MessageTypes.Error;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            var result = string.Empty;
            switch (Type)
            {
                case MessageTypes.Error:
                    result = $"Error: {Message}\nException: {Error}";
                    break;
                case MessageTypes.Information:
                    result = $"Info: {Message}";
                    break;
                case MessageTypes.Warning:
                    result = $"Warning: {Message}";
                    break;
            }

            return result;
        }
    }
}
