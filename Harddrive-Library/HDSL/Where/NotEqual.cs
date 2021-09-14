using HDDL.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HDDL.HDSL.Where
{
    /// <summary>
    /// Represents the Not Equal comparison
    /// </summary>
    class NotEqual : OperatorBase 
    {
        /// <summary>
        /// Creates a Not Equal operator
        /// </summary>
        /// <param name="left">The value used as the left</param>
        /// <param name="right">The value used as the right</param>
        public NotEqual(HDSLToken left, HDSLToken right)
        {
            LeftValue = new WhereValue(left);
            RightValue = new WhereValue(right);
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
                case WhereValueTypes.BookMarkReference:
                    result = LeftValue.Get<string>(record) != RightValue.Get<string>(record);
                    break;
                case WhereValueTypes.DateTime:
                    result = LeftValue.Get<DateTime>(record) != RightValue.Get<DateTime>(record);
                    break;
                case WhereValueTypes.RealNumber:
                    result = LeftValue.Get<double>(record) != RightValue.Get<double>(record);
                    break;
                case WhereValueTypes.String:
                    result = LeftValue.Get<string>(record) != RightValue.Get<string>(record);
                    break;
                case WhereValueTypes.WholeNumber:
                    result = LeftValue.Get<long>(record) != RightValue.Get<long>(record);
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
            return "!=";
        }
    }
}
