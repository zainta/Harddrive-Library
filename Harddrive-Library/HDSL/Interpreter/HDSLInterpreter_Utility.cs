// Copyright (c) Zain Al-Ahmary.  All rights reserved.
// Licensed under the MIT License, (the "License"); you may not use this file except in compliance with the License. 
// You may obtain a copy of the License at https://mit-license.org/

using HDDL.Data;
using HDDL.HDSL.Logging;
using HDDL.HDSL.Results;
using HDDL.HDSL.Where;
using System;
using System.Collections.Generic;
using System.IO;

namespace HDDL.HDSL.Interpreter
{
    /// <summary>
    /// Contains the HDSLInterpreter class' utility methods
    /// </summary>
    partial class HDSLInterpreter
    {
        /// <summary>
        /// Returns a value indicating whether or not there are more tokens beyond the given minimum
        /// </summary>
        /// <param name="min">The minimum number of tokens to test for</param>
        /// <returns>True if there are more than min, false otherwise</returns>
        private bool More(int min = 0)
        {
            return _tokens.Count > min;
        }

        /// <summary>
        /// Peeks at the token at the given index
        /// </summary>
        /// <param name="offset">The index to look at</param>
        /// <returns>The token at the given location</returns>
        /// <exception cref="IndexOutOfRangeException" />
        private HDSLToken Peek(int offset = 0)
        {
            return _tokens.Peek(offset);
        }

        /// <summary>
        /// Removes and returns the number of tokens
        /// </summary>
        /// <param name="count">The number of tokens to return</param>
        /// <returns>The set of tokens</returns>
        /// <exception cref="IndexOutOfRangeException" />
        private HDSLToken[] Pop(int count)
        {
            if (count == 0) return new HDSLToken[] { };

            var t = new List<HDSLToken>();
            for (int i = 0; i < count; i++)
            {
                t.Add(_tokens.Pop());
            }

            return t.ToArray();
        }

        /// <summary>
        /// Removes and returns the first token
        /// </summary>
        /// <returns>The token</returns>
        /// <exception cref="IndexOutOfRangeException" />
        private HDSLToken Pop()
        {
            var pop = _tokens.Pop();
            AppendStatementPiece(_currentStatement, pop);
            return pop;
        }

        /// <summary>
        /// Attempts to convert a bookmark into its actual value
        /// </summary>
        /// <param name="bookmark">The text to convert</param>
        /// <returns>The result of the conversion</returns>
        private string ApplyBookmarks(HDSLToken bookmark)
        {
            var result = _dh.ApplyBookmarks(bookmark.Code);
            if (bookmark.Code == result)
            {
                _errors.Add(new HDSLLogBase(bookmark.Column, bookmark.Row, $"Unknown bookmark '{bookmark.Code}'."));
            }

            return result;
        }

        /// <summary>
        /// Attempts to convert a bookmark into its actual value
        /// </summary>
        /// <param name="bookmark">The text to convert</param>
        /// <returns>The result of the conversion</returns>
        private string ApplyBookmarks(string bookmark)
        {
            var result = _dh.ApplyBookmarks(bookmark);
            if (bookmark == result)
            {
                _errors.Add(new HDSLLogBase(Peek().Column, Peek().Row, $"Unknown bookmark '{bookmark}'."));
            }

            return result;
        }

        /// <summary>
        /// Evaluates the operator base to ensure that it is valid
        /// </summary>
        /// <param name="queryDetail">The operator to evaluate</param>
        private void ValidateWhereExpression(OperatorBase queryDetail)
        {
            queryDetail.Evaluate(new DiskItem()
            {
                Id = Guid.NewGuid(),
                Path = @"c:\fakefile.txt",
                CreationDate = DateTime.Now,
                Extension = ".txt",
                FirstScanned = DateTime.Now,
                LastScanned = DateTime.Now,
                FileHash = null,
                HashTimestamp = DateTime.Now,
                Attributes = FileAttributes.Normal,
                IsFile = true,
                ItemName = "fakefile.txt",
                Size = 1024,
                LastAccessed = DateTime.Now,
                LastWritten = DateTime.Now,
                MachineUNCName = "C:",
                ParentId = null,
                Depth = 2
            });
        }

        /// <summary>
        /// Resets the console standard stream to its default
        /// </summary>
        private void ResetStandardOutputToDefault()
        {
            var strm = new StreamWriter(Console.OpenStandardOutput());
            strm.AutoFlush = true;
            Console.SetOut(strm);
        }

        /// <summary>
        /// Resets the console error stream to its default
        /// </summary>
        private void ResetStandardErrorToDefault()
        {
            var strm = new StreamWriter(Console.OpenStandardError());
            strm.AutoFlush = true;
            Console.SetError(strm);
        }

        /// <summary>
        /// Checks for either a semicolon (;) or the end of the file
        /// </summary>
        /// <param name="outcome">The result</param>
        /// <returns></returns>
        private T AddStatement<T>(T outcome) where T : HDSLOutcome
        {
            if (Peek().Type == HDSLTokenTypes.EndOfLine ||
            Peek().Type == HDSLTokenTypes.EndOfFile)
            {
                if (Peek().Type == HDSLTokenTypes.EndOfLine)
                {
                    outcome.Statement = $"{_currentStatement};";
                }
                else
                {
                    outcome.Statement = _currentStatement.ToString();
                }
            }
            else
            {
                _errors.Add(new HDSLLogBase(Peek().Column, Peek().Row, $"';' or end of file expected."));
            }

            return outcome;
        }
    }
}
