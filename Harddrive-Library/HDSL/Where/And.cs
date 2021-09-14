using HDDL.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HDDL.HDSL.Where
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
        public And(OperatorBase left, OperatorBase right)
        {
            LeftContent = left;
            RightContent = right;
        }

        /// <summary>
        /// Evaluates the expression against the record
        /// </summary>
        /// <param name="record">The DiskItem record to evaluate</param>
        /// <returns>The boolean result of the evaluation</returns>
        public override bool Evaluate(DiskItem record)
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
