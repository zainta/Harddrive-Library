// Copyright (c) Zain Al-Ahmary.  All rights reserved.
// Licensed under the MIT License, (the "License"); you may not use this file except in compliance with the License. 
// You may obtain a copy of the License at https://mit-license.org/

using HDDL.Data;
using HDDL.Language.HDSL.Where.Exceptions;
using System;
using System.Text.RegularExpressions;

namespace HDDL.Language.HDSL.Where
{
    /// <summary>
    /// Represents a regular expression comparison
    /// </summary>
    class Like : OperatorBase
    {
        /// <summary>
        /// Creates a Like operator
        /// </summary>
        /// <param name="left">The value used as the left</param>
        /// <param name="right">The value used as the right</param>
        /// <param name="cc">The clause's execution context</param>
        /// <param name="self">The token that represents this operator</param>
        public Like(HDSLToken self, HDSLToken left, HDSLToken right, ClauseContext cc) : base(self)
        {
            LeftValue = new WhereValue(left, cc);
            RightValue = new WhereValue(right, cc);
        }

        /// <summary>
        /// Evaluates the expression against the record
        /// </summary>
        /// <param name="record">The DiskItem record to evaluate</param>
        /// <returns>The boolean result of the evaluation</returns>
        public override bool Evaluate(DiskItem record)
        {
            if (LeftValue.ValueType != RightValue.ValueType)
            {
                throw new WhereClauseException(Column, Row, $"Cannot compare {LeftValue.ValueType} to {RightValue.ValueType}.", WhereClauseExceptionTypes.TypeMismatch);
            }
            
            var result = false;
            switch (LeftValue.ValueType)
            {
                case WhereValueTypes.String:
                    try
                    {
                        result = Regex.IsMatch(LeftValue.Get<string>(record), RightValue.Get<string>(record));
                    }
                    catch (Exception ex)
                    {
                        throw new WhereClauseException(Column, Row, $"Error during Like (~): {ex.Message}", WhereClauseExceptionTypes.InvalidUseOfLike);
                    }
                    break;
                default:
                    throw new WhereClauseException(Column, Row, $"The Like operator (~) can only be used when comparing strings.", WhereClauseExceptionTypes.InvalidUseOfLike);
            }

            return result;
        }

        /// <summary>
        /// Returns the sign used to represent the operation
        /// </summary>
        /// <returns></returns>
        protected override string GetOperatorSign()
        {
            return "~";
        }

        /// <summary>
        /// path REGEXP '.*\.exe$'
        /// </summary>
        /// <returns></returns>
        public override string ToSQL()
        {
            var result = string.Empty;
            if (LeftValue != null && RightValue != null)
            {
                result = $"{LeftValue.ToSQL()} REGEXP {RightValue.ToSQL()}";
            }

            return result;
        }
    }
}
