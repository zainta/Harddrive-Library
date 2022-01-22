// Copyright (c) Zain Al-Ahmary.  All rights reserved.
// Licensed under the MIT License, (the "License"); you may not use this file except in compliance with the License. 
// You may obtain a copy of the License at https://mit-license.org/

using HDDL.Data;

namespace HDDL.Language.HDSL.Where
{
    /// <summary>
    /// Represents the And comparison
    /// </summary>
    class And : OperatorBase
    {
        /// <summary>
        /// Creates an And operator
        /// </summary>
        /// <param name="left">The operator who's result will be used as the left</param>
        /// <param name="right">The operator who's result will be used as the right</param>
        /// <param name="self">The token that represents this operator</param>
        public And(HDSLToken self, OperatorBase left, OperatorBase right) : base(self)
        {
            LeftContent = left;
            RightContent = right;
        }

        /// <summary>
        /// Evaluates the expression against the record
        /// </summary>
        /// <param name="record">The DiskItem record to evaluate</param>
        /// <returns>The boolean result of the evaluation</returns>
        public override bool Evaluate(HDDLRecordBase record)
        {
            return LeftContent.Evaluate(record) && RightContent.Evaluate(record);
        }

        /// <summary>
        /// Returns the sign used to represent the operation
        /// </summary>
        /// <returns></returns>
        protected override string GetOperatorSign()
        {
            return "and";
        }
    }
}
