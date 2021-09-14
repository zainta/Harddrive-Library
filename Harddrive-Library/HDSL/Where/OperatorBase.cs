using HDDL.Collections;
using HDDL.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HDDL.HDSL.Where
{
    /// <summary>
    /// Base class for all Where evaluation operator types.
    /// </summary>
    abstract class OperatorBase
    {
        /// <summary>
        /// The operator's left operand
        /// </summary>
        public OperatorBase LeftContent { get; set; }

        /// <summary>
        /// The operator's right operand
        /// </summary>
        public OperatorBase RightContent { get; set; }

        /// <summary>
        /// The operator's left value
        /// </summary>
        public WhereValue LeftValue { get; set; }

        /// <summary>
        /// The operator's right value
        /// </summary>
        public WhereValue RightValue { get; set; }

        /// <summary>
        /// Performs the relevant operation and returns if it evaluates to true or false
        /// </summary>
        /// <param name="record">The record to evaluate</param>
        /// <returns>Returns the resulting outcome</returns>
        public virtual bool Evaluate(DiskItem record)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Converts a listStack of tokens into an Operator structure for evaluation
        /// </summary>
        /// <param name="tokens"></param>
        /// <returns></returns>
        public static OperatorBase ConvertClause(ListStack<HDSLToken> tokens)
        {
            ListStack<HDSLToken> queue = new ListStack<HDSLToken>();

            // We build the structure using reverse token order due to how evaluation works
            while (!tokens.Empty)
            {
                if (tokens.Peek().Family == HDSLTokenFamilies.DataTypes ||
                    tokens.Peek().Family == HDSLTokenFamilies.LogicalOperators ||
                    tokens.Peek().Family == HDSLTokenFamilies.RelativeOperators ||
                    tokens.Peek().Family == HDSLTokenFamilies.ValueKeywords)
                {
                    queue.Push(tokens.Peek());
                }
                tokens.Pop();
            }

            // recursively go through the queue building the operator instances
            var logicals = new Queue<HDSLToken>();
            Func<ListStack<HDSLToken>, OperatorBase, OperatorBase> recursor = null;
            recursor = (tq, right) =>
            {
                OperatorBase result = null;
                if (tq.Count > 1 && tq.Peek(1).Family == HDSLTokenFamilies.RelativeOperators)
                {
                    var topic = Get(tq);
                    if (tq.Count > 1 && tq.Peek(1).Family == HDSLTokenFamilies.RelativeOperators)
                    {
                        throw new InvalidOperationException("Logical expression expected between relative expressions.");
                    }
                    else if (tq.Count == 0)
                    {
                        result = topic;
                    }
                    else
                    {
                        result = recursor(tq, topic);
                    }
                }
                else if (tq.Count > 0 && tq.Peek().Family == HDSLTokenFamilies.LogicalOperators)
                {
                    var type = tq.Pop().Type;
                    switch (type)
                    {
                        case HDSLTokenTypes.And:
                            result = new And(recursor(tq, null), right);
                            break;
                        case HDSLTokenTypes.Or:
                            result = new Or(recursor(tq, null), right);
                            break;
                    }
                }

                return result;
            };
            var structure = recursor(queue, null);
            return structure;
        }

        /// <summary>
        /// Extracts the most immediately available operator base and returns it
        /// </summary>
        /// <param name="tokens">The tokens to extract from</param>
        /// <returns>The operator base</returns>
        private static OperatorBase Get(ListStack<HDSLToken> tokens)
        {
            OperatorBase result = null;
            if (tokens.Count > 2 && tokens.Peek(1).Family == HDSLTokenFamilies.RelativeOperators)
            {
                var right = tokens.Pop();
                var opratr = tokens.Pop();
                var left = tokens.Pop();

                switch (opratr.Type)
                {
                    case HDSLTokenTypes.Equal:
                        result = new Equals(left, right);
                        break;
                    case HDSLTokenTypes.NotEqual:
                        result = new NotEqual(left, right);
                        break;
                    case HDSLTokenTypes.GreaterThan:
                        result = new GreaterThan(left, right);
                        break;
                    case HDSLTokenTypes.GreaterOrEqual:
                        result = new GreaterOrEqual(left, right);
                        break;
                    case HDSLTokenTypes.LessThan:
                        result = new LessThan(left, right);
                        break;
                    case HDSLTokenTypes.LessOrEqual:
                        result = new LessOrEqual(left, right);
                        break;
                }
            }

            return result;
        }

        /// <summary>
        /// Returns the sign used to represent the operation
        /// </summary>
        /// <returns></returns>
        protected virtual string GetOperatorSign()
        {
            throw new NotImplementedException();
        }

        public override string ToString()
        {
            var result = string.Empty;
            if (LeftContent != null && RightContent != null)
            {
                result = $"{LeftContent} {GetOperatorSign()} {RightContent}";
            }
            else if (LeftValue != null && RightValue != null)
            {
                result = $"{LeftValue} {GetOperatorSign()} {RightValue}";
            }

            return result;
        }
    }
}
