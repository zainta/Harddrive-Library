// Copyright (c) Zain Al-Ahmary.  All rights reserved.
// Licensed under the MIT License, (the "License"); you may not use this file except in compliance with the License. 
// You may obtain a copy of the License at https://mit-license.org/

using HDDL.Collections;
using HDDL.Data;
using HDDL.Language.HDSL.Interpreter;
using HDDL.Language.HDSL.Where.Exceptions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace HDDL.Language.HDSL.Where
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
        /// The token row where the error occurred
        /// </summary>
        public int Row { get; set; }

        /// <summary>
        /// The token column where the error occurred
        /// </summary>
        public int Column { get; set; }

        /// <summary>
        /// The token that represents the operator
        /// </summary>
        /// <param name="self">The token that represents this operator</param>
        public OperatorBase(HDSLToken self)
        {
            Row = self.Row;
            Column = self.Column;
        }

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
        /// <param name="tokens">The tokens to convert</param>
        /// <param name="dh">The data handler to use for mapping resolution</param>
        /// <param name="cc">The current statement context</param>
        /// <param name="currentStatement">The currently execution statement</param>
        /// <returns>The converted token structure's root node</returns>
        public static OperatorBase ConvertClause(ListStack<HDSLToken> tokens, ClauseContext cc, StringBuilder currentStatement)
        {
            ListStack<HDSLToken> queue = new ListStack<HDSLToken>();

            if (tokens.Peek().Type == HDSLTokenTypes.Where)
            {
                tokens.Pop();
            }

            // We build the structure using reverse token order due to how evaluation works
            while (!tokens.Empty)
            {
                if (tokens.Peek().Type == HDSLTokenTypes.EndOfLine ||
                    tokens.Peek().Type == HDSLTokenTypes.EndOfFile)
                {
                    break;
                }

                if (tokens.Peek().Family == HDSLTokenFamilies.DataTypes ||
                    tokens.Peek().Family == HDSLTokenFamilies.LogicalOperators ||
                    tokens.Peek().Family == HDSLTokenFamilies.RelativeOperators ||
                    tokens.Peek().Family == HDSLTokenFamilies.ValueKeywords ||
                    tokens.Peek().Family == HDSLTokenFamilies.AttributeLiterals ||
                    tokens.Peek().Family == HDSLTokenFamilies.StateOperators)
                {
                    queue.Push(tokens.Peek());
                }
                else
                {
                    break;
                }
                HDSLInterpreter.AppendStatementPiece(currentStatement, tokens.Pop());
            }

            // recursively go through the queue building the operator instances
            var logicals = new Queue<HDSLToken>();
            Func<ListStack<HDSLToken>, OperatorBase, OperatorBase> recursor = null;
            recursor = (tq, right) =>
            {
                OperatorBase result = null;
                if (tq.Count > 1 && tq.Peek(1).Family == HDSLTokenFamilies.RelativeOperators)
                {
                    var topic = Get(tq, cc);
                    if (tq.Count > 1 && tq.Peek(1).Family == HDSLTokenFamilies.RelativeOperators)
                    {
                        throw new WhereClauseException(tq.Peek(1).Column, tq.Peek(1).Row, "Logical expression expected between relative expressions.", WhereClauseExceptionTypes.InvalidTermPosition);
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
                else if (tq.Count > 1 && tq.Peek(1).Family == HDSLTokenFamilies.StateOperators)
                {
                    var val = tq.Pop();
                    var stateOp = tq.Pop();
                    switch (stateOp.Type)
                    {
                        case HDSLTokenTypes.Has:
                            if (cc.QueriedType != typeof(DiskItem))
                            {
                                throw new WhereClauseException(stateOp.Column, stateOp.Row, $"The Has (+) operator is only valid when querying the file system.", WhereClauseExceptionTypes.InvalidUseofHasOrHasNot);
                            }
                            result = new Has(stateOp, val, cc);
                            break;
                        case HDSLTokenTypes.HasNot:
                            if (cc.QueriedType != typeof(DiskItem))
                            {
                                throw new WhereClauseException(stateOp.Column, stateOp.Row, $"The Has Not (-) operator is only valid when querying the file system.", WhereClauseExceptionTypes.InvalidUseofHasOrHasNot);
                            }
                            result = new HasNot(stateOp, val, cc);
                            break;
                    }

                    if (tq.Count > 0)
                    {
                        result = recursor(tq, result);
                    }
                }
                else if (tq.Count > 0 && tq.Peek().Family == HDSLTokenFamilies.LogicalOperators)
                {
                    var op = tq.Pop();
                    switch (op.Type)
                    {
                        case HDSLTokenTypes.And:
                            result = new And(op, recursor(tq, null), right);
                            break;
                        case HDSLTokenTypes.Or:
                            result = new Or(op, recursor(tq, null), right);
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
        /// <param name="dh">The data handler to use for mapping resolution</param>
        /// <returns>The operator base</returns>
        private static OperatorBase Get(ListStack<HDSLToken> tokens, ClauseContext cc)
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
                        result = new Equals(opratr, left, right, cc);
                        break;
                    case HDSLTokenTypes.NotEqual:
                        result = new NotEqual(opratr, left, right, cc);
                        break;
                    case HDSLTokenTypes.GreaterThan:
                        result = new GreaterThan(opratr, left, right, cc);
                        break;
                    case HDSLTokenTypes.GreaterOrEqual:
                        result = new GreaterOrEqual(opratr, left, right, cc);
                        break;
                    case HDSLTokenTypes.LessThan:
                        result = new LessThan(opratr, left, right, cc);
                        break;
                    case HDSLTokenTypes.LessOrEqual:
                        result = new LessOrEqual(opratr, left, right, cc);
                        break;
                    case HDSLTokenTypes.Like:
                        result = new Like(opratr, left, right, cc);
                        break;
                    default:
                        throw new WhereClauseException(opratr.Column, opratr.Row, $"Unknown operator type '{opratr.Type}'.", WhereClauseExceptionTypes.UnknownOperatorType);
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

        /// <summary>
        /// Converts the clause into SQL
        /// </summary>
        /// <returns></returns>
        public virtual string ToSQL()
        {
            var result = string.Empty;
            if (LeftContent != null && RightContent != null)
            {
                result = $"{LeftContent.ToSQL()} {GetOperatorSign()} {RightContent.ToSQL()}";
            }
            else if (LeftValue != null && RightValue != null)
            {
                result = $"{LeftValue.ToSQL()} {GetOperatorSign()} {RightValue.ToSQL()}";
            }
            else if (this is Has || this is HasNot)
            {
                var attributeValue = Convert.ToInt32(Enum.Parse<FileAttributes>(RightValue.ToSQL()));
                if (this is Has)
                {
                    result = $"attributes & {attributeValue} = {attributeValue}";
                }
                else if (this is HasNot)
                {
                    result = $"attributes & {attributeValue} <> {attributeValue}";
                }
            }

            return result;
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
            else if (this is Has || this is HasNot)
            {
                result = $"{GetOperatorSign()}{RightValue}";
            }

            return result;
        }
    }
}
