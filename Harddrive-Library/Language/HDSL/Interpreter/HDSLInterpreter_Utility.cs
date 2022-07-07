// Copyright (c) Zain Al-Ahmary.  All rights reserved.
// Licensed under the MIT License, (the "License"); you may not use this file except in compliance with the License. 
// You may obtain a copy of the License at https://mit-license.org/

using ReddWare.Language;
using HDDL.Data;
using HDDL.Language.HDSL.Results;
using HDDL.Language.HDSL.Where;
using System;
using System.Collections.Generic;
using System.IO;

namespace HDDL.Language.HDSL.Interpreter
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
        /// Reports an error by storing it in the queue
        /// </summary>
        /// <param name="error">The error to report</param>
        /// <returns>The current number of errors</returns>
        private int Report(LogItemBase error)
        {
            _encounteredErrors.Add(error);
            return _encounteredErrors.Count;
        }

        /// <summary>
        /// Returns a value indicating if there are no errors
        /// </summary>
        /// <returns>True if none, false otherwise</returns>
        private bool NoErrors()
        {
            return _encounteredErrors.Count == 0;
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
                Report(new LogItemBase(bookmark.Column, bookmark.Row, $"Unknown bookmark '{bookmark.Code}'."));
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
                Report(new LogItemBase(Peek().Column, Peek().Row, $"Unknown bookmark '{bookmark}'."));
            }

            return result;
        }

        /// <summary>
        /// Evaluates the operator base to ensure that it is valid
        /// </summary>
        /// <param name="queryDetail">The operator to evaluate</param>
        /// <param name="testItem">The testing record</param>
        private void ValidateWhereExpression(OperatorBase queryDetail, HDDLRecordBase testItem)
        {
            queryDetail.Evaluate(testItem);
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
                Report(new LogItemBase(Peek().Column, Peek().Row, $"';' or end of file expected."));
            }

            return outcome;
        }

        /// <summary>
        /// Generates and returns an HDDLRecordBase
        /// </summary>
        /// <param name="typeContext">The type to generate</param>
        /// <exception cref="ArgumentException">Thrown if the type isn't one of the four accepted types</exception>
        /// <returns>An instance of the record</returns>
        private HDDLRecordBase GetTestRecord(Type typeContext)
        {
            HDDLRecordBase result = null;
            if (typeContext == typeof(DiskItem))
            {
                result = new DiskItem()
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
                };
            }
            else if (typeContext == typeof(WatchItem))
            {
                result = new WatchItem()
                {
                    Id = Guid.NewGuid(),
                    Path = @"c:\fakefile.txt",
                    InPassiveMode = false,
                    Target = null
                };
            }
            else if (typeContext == typeof(WardItem))
            {
                result = new WardItem()
                {
                    Id = Guid.NewGuid(),
                    HDSL = @"find [dev] where extension = '.obj';",
                    Interval = new TimeSpan(2, 0, 0),
                    Path = @"C:\Development",
                    NextScan = DateTime.Now,
                    Target =  null
                };
            }
            else if (typeContext == typeof(DiskItemHashLogItem))
            {
                result = new DiskItemHashLogItem()
                {
                    Id = Guid.NewGuid(),
                    MachineUNCName = @"\\C\",
                    NewFileHash = "DSFSDGFDFGDFGHDFGDFGFGHGJKYTUJGHJGHJ",
                    Occurred = DateTime.Now,
                    OldFileHash = "SDFGDFGDGH#RFEHTTIKHJKGHJGFHDFGDFG",
                    ParentId = Guid.NewGuid(),
                    Path = "fakefile.txt"
                };
            }

            return result;
        }
    }
}
