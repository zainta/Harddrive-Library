// Copyright (c) Zain Al-Ahmary.  All rights reserved.
// Licensed under the MIT License, (the "License"); you may not use this file except in compliance with the License. 
// You may obtain a copy of the License at https://mit-license.org/

using HDDL.Data;
using System;

namespace HDDL.HDSL.Where
{
    /// <summary>
    /// Represents the Equals comparison
    /// </summary>
    class Equals : OperatorBase
    {
        /// <summary>
        /// Creates an Equal operator
        /// </summary>
        /// <param name="left">The value used as the left</param>
        /// <param name="right">The value used as the right</param>
        /// <param name="cc">The clause's execution context</param>
        public Equals(HDSLToken left, HDSLToken right, ClauseContext cc)
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
                throw new TypeMismatchException($"Cannot compare {LeftValue.ValueType} to {RightValue.ValueType}.");
            }

            var result = false;
            switch (LeftValue.ValueType)
            {
                case WhereValueTypes.DateTime:
                    result = LeftValue.Get<DateTime>(record) == RightValue.Get<DateTime>(record);
                    break;
                case WhereValueTypes.RealNumber:
                    result = LeftValue.Get<double>(record) == RightValue.Get<double>(record);
                    break;
                case WhereValueTypes.String:
                    result = LeftValue.Get<string>(record) == RightValue.Get<string>(record);
                    break;
                case WhereValueTypes.WholeNumber:
                    result = LeftValue.Get<long>(record) == RightValue.Get<long>(record);
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
            return "=";
        }
    }
}
