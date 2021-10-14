// Copyright (c) Zain Al-Ahmary.  All rights reserved.
// Licensed under the MIT License, (the "License"); you may not use this file except in compliance with the License. 
// You may obtain a copy of the License at https://mit-license.org/

using HDDL.Data;

namespace HDDL.HDSL.Where
{
    /// <summary>
    /// Represents the Has Attribute unary operator
    /// </summary>
    class Has : OperatorBase
    {
        /// <summary>
        /// Creates an And operator
        /// </summary>
        /// <param name="right">The operator who's result will be used as the right</param>
        public Has(HDSLToken right)
        {
            RightValue = new WhereValue(right);
        }

        /// <summary>
        /// Evaluates the expression against the record
        /// </summary>
        /// <param name="record">The DiskItem record to evaluate</param>
        /// <returns>The boolean result of the evaluation</returns>
        public override bool Evaluate(DiskItem record)
        {
            return record.Attributes.HasFlag(RightValue.Get<System.IO.FileAttributes>(record));
        }

        /// <summary>
        /// Returns the sign used to represent the operation
        /// </summary>
        /// <returns></returns>
        protected override string GetOperatorSign()
        {
            return "+";
        }
    }
}
