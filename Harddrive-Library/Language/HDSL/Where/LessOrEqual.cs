// Copyright (c) Zain Al-Ahmary.  All rights reserved.
// Licensed under the MIT License, (the "License"); you may not use this file except in compliance with the License. 
// You may obtain a copy of the License at https://mit-license.org/

using HDDL.Data;
using HDDL.Language.HDSL.Where.Exceptions;
using System;

namespace HDDL.Language.HDSL.Where
{
    /// <summary>
    /// Represents the Less Than Or Equal To comparison
    /// </summary>
    class LessOrEqual : OperatorBase
    {
        /// <summary>
        /// Creates a Less Than Or Equal To operator
        /// </summary>
        /// <param name="left">The value used as the left</param>
        /// <param name="right">The value used as the right</param>
        /// <param name="cc">The clause's execution context</param>
        /// <param name="self">The token that represents this operator</param>
        public LessOrEqual(HDSLToken self, HDSLToken left, HDSLToken right, ClauseContext cc) : base(self) 
        {
            LeftValue = new WhereValue(left, cc);
            RightValue = new WhereValue(right, cc);
        }

        /// <summary>
        /// Evaluates the parameters to see if they meet the operator's requirements
        /// </summary>
        /// <param name="record">The DiskItem record to evaluate</param>
        /// <returns>Whether or not the expression is true or false</returns>
        public override bool Evaluate(HDDLRecordBase record)
        {
            if (LeftValue.ValueType != RightValue.ValueType)
            {
                throw new WhereClauseException(Column, Row, $"Cannot compare {LeftValue.ValueType} to {RightValue.ValueType}.", WhereClauseExceptionTypes.TypeMismatch);
            }

            var result = false;
            switch (LeftValue.ValueType)
            {
                case WhereValueTypes.DateTime:
                    result = LeftValue.Get<DateTime>(record) <= RightValue.Get<DateTime>(record);
                    break;
                case WhereValueTypes.RealNumber:
                    result = LeftValue.Get<double>(record) <= RightValue.Get<double>(record);
                    break;
                case WhereValueTypes.String:
                    throw new WhereClauseException(Column, Row, $"Cannot use {GetOperatorSign()} operator with string type operands.", WhereClauseExceptionTypes.OperatorTypeMismatch);
                case WhereValueTypes.WholeNumber:
                    result = LeftValue.Get<long>(record) <= RightValue.Get<long>(record);
                    break;
            }

            return result;
        }

        /// <summary>
        /// Returns the sign used to represent the operation
        /// </summary>
        /// <returns></returns>
        protected override string GetOperatorSign()
        {
            return "<=";
        }
    }
}
