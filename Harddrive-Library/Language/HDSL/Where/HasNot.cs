// Copyright (c) Zain Al-Ahmary.  All rights reserved.
// Licensed under the MIT License, (the "License"); you may not use this file except in compliance with the License. 
// You may obtain a copy of the License at https://mit-license.org/

using HDDL.Data;
using HDDL.Language.HDSL.Where.Exceptions;

namespace HDDL.Language.HDSL.Where
{
    /// <summary>
    /// Represents the inverse of the Has Attribute unary operator
    /// </summary>
    class HasNot : OperatorBase
    {
        /// <summary>
        /// Creates an And operator
        /// </summary>
        /// <param name="right">The operator who's result will be used as the right</param>
        /// <param name="cc">The clause's execution context</param>
        /// <param name="self">The token that represents this operator</param>
        public HasNot(HDSLToken self, HDSLToken right, ClauseContext cc) : base(self)
        {
            RightValue = new WhereValue(right, cc);
        }

        /// <summary>
        /// Evaluates the expression against the record
        /// </summary>
        /// <param name="record">The DiskItem record to evaluate</param>
        /// <returns>The boolean result of the evaluation</returns>
        public override bool Evaluate(HDDLRecordBase record)
        {
            if (record is DiskItem)
            {
                return !((DiskItem)record).Attributes.HasFlag(RightValue.Get<System.IO.FileAttributes>(record));
            }
            else
            {
                throw new WhereClauseException(RightValue.Column, RightValue.Row, $"The Has Not (-) operator is only valid when querying the file system.", WhereClauseExceptionTypes.InvalidUseofHasOrHasNot);
            }
        }

        /// <summary>
        /// Returns the sign used to represent the operation
        /// </summary>
        /// <returns></returns>
        protected override string GetOperatorSign()
        {
            return "-";
        }
    }
}
