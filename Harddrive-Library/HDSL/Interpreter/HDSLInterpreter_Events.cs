// Copyright (c) Zain Al-Ahmary.  All rights reserved.
// Licensed under the MIT License, (the "License"); you may not use this file except in compliance with the License. 
// You may obtain a copy of the License at https://mit-license.org/

using System.Text;

namespace HDDL.HDSL.Interpreter
{
    /// <summary>
    /// Contains the HDSLInterpreter class' static methods, events, and event related methods
    /// </summary>
    partial class HDSLInterpreter
    {
        /// <summary>
        /// Correct adds a token to the string builder
        /// </summary>
        /// <param name="statement">The statement to append to</param>
        /// <param name="token">The token to append</param>
        /// <returns></returns>
        public static StringBuilder AppendStatementPiece(StringBuilder statement, HDSLToken token)
        {
            if (token.Type == HDSLTokenTypes.String)
            {
                if (token.Code.Contains('\\'))
                {
                    statement.Append(statement.Length == 0 ? $"@{token.Code}" : $" @{token.Code}");
                }
                else
                {
                    statement.Append(statement.Length == 0 ? token.Code : $" {token.Code}");
                }
            }
            else
            {
                statement.Append(statement.Length == 0 ? token.Code : $" {token.Code}");
            }

            return statement;
        }
    }
}
