// Copyright (c) Zain Al-Ahmary.  All rights reserved.
// Licensed under the MIT License, (the "License"); you may not use this file except in compliance with the License. 
// You may obtain a copy of the License at https://mit-license.org/

using HDDL.Collections;
using HDDL.Data;
using HDDL.HDSL.Logging;
using HDDL.HDSL.Where;
using HDDL.IO.Disk;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace HDDL.HDSL
{
    /// <summary>
    /// Executes tokens according to language requirements
    /// </summary>
    class HDSLInterpreter
    {
        /// <summary>
        /// The stored tokens
        /// </summary>
        private ListStack<HDSLToken> _tokens;

        /// <summary>
        /// The data handler instance used for operations
        /// </summary>
        private DataHandler _dh;

        /// <summary>
        /// The list of errors
        /// </summary>
        private List<HDSLLogBase> _errors;

        /// <summary>
        /// Creates an interpreter using the provided tokenizer's tokens and the provided file database
        /// </summary>
        /// <param name="tokenizer">The tokenizer whose tokens should be consumed</param>
        /// <param name="dh">The data handler to use</param>
        public HDSLInterpreter(ListStack<HDSLToken> tokens, DataHandler dh)
        {
            _tokens = new ListStack<HDSLToken>(tokens.ToList());
            _dh = dh;
            _errors = new List<HDSLLogBase>();
        }

        /// <summary>
        /// Interprets the tokens against the provided database
        /// </summary>
        /// <param name="closeDb">Whether or not to close the database upon completion</param>
        /// <returns>The results of the interpretation</returns>
        public HDSLResult Interpret(bool closeDb)
        {
            HDSLResult result = null;
            var results = new List<HDSLQueryOutcome>();

            try
            {
                var done = false;

                while (!_tokens.Empty && _errors.Count == 0 && !done)
                {
                    switch (Peek().Type)
                    {
                        case HDSLTokenTypes.MultiLineComment:
                        case HDSLTokenTypes.Comment:
                            Pop();
                            break;
                        case HDSLTokenTypes.BookmarkReference:
                            HandleBookmarkDefinitionStatement();
                            break;
                        case HDSLTokenTypes.Purge:
                            HandlePurgeStatement();
                            break;
                        case HDSLTokenTypes.Find:
                            results.Add(HandleFindStatement());
                            break;
                        case HDSLTokenTypes.Scan:
                            results.Add(HandleScanStatement());
                            break;
                        case HDSLTokenTypes.EndOfFile:
                            Pop();
                            done = true;
                            break;
                        case HDSLTokenTypes.EndOfLine:
                            Pop();
                            break;
                        case HDSLTokenTypes.Include:
                            HandleIncludeStatement();
                            break;
                        case HDSLTokenTypes.Exclude:
                            HandleExcludeStatement();
                            break;
                        case HDSLTokenTypes.Check:
                            results.Add(HandleIntegrityCheck());
                            break;
                        default:
                            _errors.Add(new HDSLLogBase(Peek().Column, Peek().Row, $"Unexpected token '{Peek().Code}'."));
                            done = true;
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                result = new HDSLResult(new HDSLLogBase[] { new HDSLLogBase(-1, -1, $"Exception thrown: {ex}") });
            }

            if (_errors.Count > 0)
            {
                result = new HDSLResult(_errors);
            }
            else
            {
                result = new HDSLResult(results);
                results.ForEach(r => r.Parent = result);
            }

            return result;
        }

        #region Utility Methods

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
            return _tokens.Pop();
        }

        /// <summary>
        /// Interprets and returns a comma seperated list of strings
        /// </summary>
        /// <param name="failOnNone">Whether or not the method should log an error if no paths are discovered</param>
        /// <returns>A list containing the strings</returns>
        private List<string> GetPathList(bool failOnNone = true, bool expandBookmarks = true)
        {
            HDSLToken first = null;
            var results = new List<string>();
            // get the list of paths
            while (More() && Peek().Type == HDSLTokenTypes.String || Peek().Type == HDSLTokenTypes.BookmarkReference)
            {
                // save the first one for error reporting
                if (first == null) first = Peek();

                if (Peek().Type == HDSLTokenTypes.BookmarkReference)
                {
                    if (expandBookmarks)
                    {
                        results.Add(_dh.ApplyBookmarks(Pop().Code));
                    }
                    else
                    {
                        results.Add(Pop().Literal);
                    }
                }
                else
                {
                    results.Add(Pop().Literal);
                }

                // check if we have at least 2 more tokens remaining, one is a comma and the next is a string or bookmark
                // if so, then this is a list
                if (More(2) &&
                    Peek().Type == HDSLTokenTypes.Comma &&
                    (Peek(1).Type == HDSLTokenTypes.String || Peek(1).Type == HDSLTokenTypes.BookmarkReference))
                {
                    // strip the comma so the loop continues
                    Pop();
                }
            }

            results = PathHelper.EnsurePath(results).ToList();
            if (failOnNone && results.Count == 0)
            {
                if (first != null)
                {
                    _errors.Add(new HDSLLogBase(first.Column, first.Row, $"No valid paths found."));
                }
            }
            return results;
        }

        /// <summary>
        /// Interprets the next token to get the display mode
        /// </summary>
        /// <param name="defaultDisplayMode">The default if none is found</param>
        /// <returns>The resulting display mode</returns>
        private Scanning.EventWrapperDisplayModes GetDisplayMode(Scanning.EventWrapperDisplayModes defaultDisplayMode = Scanning.EventWrapperDisplayModes.Text)
        {
            var displayMode = defaultDisplayMode;
            if (Peek().Type == HDSLTokenTypes.ProgressMode ||
                Peek().Type == HDSLTokenTypes.QuietMode ||
                Peek().Type == HDSLTokenTypes.SpinnerMode ||
                Peek().Type == HDSLTokenTypes.TextMode)
            {
                switch (Pop().Type)
                {
                    case HDSLTokenTypes.ProgressMode:
                        displayMode = Scanning.EventWrapperDisplayModes.ProgressBar;
                        break;
                    case HDSLTokenTypes.QuietMode:
                        displayMode = Scanning.EventWrapperDisplayModes.Displayless;
                        break;
                    case HDSLTokenTypes.SpinnerMode:
                        displayMode = Scanning.EventWrapperDisplayModes.Spinner;
                        break;
                    case HDSLTokenTypes.TextMode:
                        displayMode = Scanning.EventWrapperDisplayModes.Text;
                        break;
                }
            }

            return displayMode;
        }

        #endregion

        #region Statement Handlers

        /// <summary>
        /// Allows the execution of an integrity check through HDSL script
        /// 
        /// Purpose:
        /// Allows scripts to run integrity scan
        /// 
        /// Syntax:
        /// check [spinner|progress|text|quiet - defaults to text] [file pattern] [in/within/under [path[, path, path]] - defaults to current] [where clause];
        /// </summary>
        private HDSLQueryOutcome HandleIntegrityCheck()
        {
            if (More() && Peek().Type == HDSLTokenTypes.Check)
            {
                Pop();

                var displayMode = GetDisplayMode();
                var findResult = HandleFindStatement();

                if (findResult.Items.Length > 0)
                {
                    var scan = new Scanning.IntegrityScanEventWrapper(_dh, findResult.Items, true, displayMode);
                    if (scan.Go())
                    {
                        return scan.Result;
                    }
                }
                else
                {
                    _errors.Add(new HDSLLogBase(Peek().Column, Peek().Row, $"The integrity check's feeder query returned no results."));
                }

            }
            else
            {
                _errors.Add(new HDSLLogBase(Peek().Column, Peek().Row, $"'check' expected."));
            }

            return new Scanning.IntegrityScanResultSet(new DiskItem[] { }, new DiskItem[] { });
        }

        /// <summary>
        /// Handles the interpretation of a region exclusion declaration
        /// 
        /// Purpose:
        /// Makes an entry that ensuring that subsequent scans will ignore that path and its contents
        /// 
        /// Syntax:
        /// exclude [dynamic] path[, path, path];
        /// </summary>
        private void HandleExcludeStatement()
        {
            if (More() && Peek().Type == HDSLTokenTypes.Exclude)
            {
                Pop();

                // if the optional 'dynamic' keyword is present, then store exclusions with their bookmarks intact and expand them during scanning.
                // this will make changes to a bookmark's meaning automatically be taken into account.
                // It also means that deleting a bookmark that a dynamic exclusion references will make it "dead", resulting in it being ignored
                var expand = (More() && Peek().Type == HDSLTokenTypes.Dynamic);
                if (expand)
                {
                    Pop(); // get rid of the token
                }

                // get the list of excluded locations
                var paths = GetPathList(expandBookmarks: expand);
                if (paths.Count > 0)
                {
                    // Create exclusion instances for paths that are not already excluded
                    var exclusions = (from p in paths
                                      where
                                        _dh.GetExclusions().Where(e => e.Region == p).Any() == false
                                      select
                                          new ExclusionItem()
                                          {
                                              Id = Guid.NewGuid(),
                                              Region = p
                                          }).ToArray();
                    _dh.Insert(exclusions);
                    _dh.WriteExclusions();
                }
                else
                {
                    _errors.Add(new HDSLLogBase(Peek().Column, Peek().Row, $"Path string(s) expected."));
                }
            }
            else
            {
                _errors.Add(new HDSLLogBase(Peek().Column, Peek().Row, $"'exclude' expected."));
            }
        }

        /// <summary>
        /// Handles the interpretation of an exclusion removal statement
        /// 
        /// Purpose:
        /// Removes an exclusion, returning the excluded region to eligibility for subsequent scans
        /// 
        /// Syntax:
        /// include path[, path, path];
        /// </summary>
        private void HandleIncludeStatement()
        {
            if (More() && Peek().Type == HDSLTokenTypes.Include)
            {
                Pop();

                // get the list of excluded locations
                var paths = GetPathList();
                if (paths.Count > 0)
                {
                    // Retrieve the exclusions in the list that actually exist
                    var exclusions = (from e in _dh.GetExclusions()
                                      where
                                        paths.Where(p => p.Equals(e.Region, StringComparison.InvariantCultureIgnoreCase)).Any() == true
                                      select e).ToArray();
                    _dh.Delete(exclusions);
                    _dh.WriteExclusions();
                }
                else
                {
                    _errors.Add(new HDSLLogBase(Peek().Column, Peek().Row, $"Path string(s) expected."));
                }
            }
            else
            {
                _errors.Add(new HDSLLogBase(Peek().Column, Peek().Row, $"'include' expected."));
            }
        }

        /// <summary>
        /// Handles the interpretation of a code-based scan call
        /// 
        /// Purpose:
        /// Allows scripts to run scans
        /// 
        /// Syntax:
        /// scan [spinner|progress|text|quiet - defaults to text] [path[, path, path]];
        /// </summary>
        private HDSLQueryOutcome HandleScanStatement()
        {
            if (More() && Peek().Type == HDSLTokenTypes.Scan)
            {
                Pop();

                var displayMode = GetDisplayMode();
                var scanPaths = GetPathList();
                if (scanPaths.Count > 0)
                {
                    var dsw = new Scanning.DiskScanEventWrapper(_dh, scanPaths, true, displayMode);
                    if (dsw.Go())
                    {
                        return dsw.Result;
                    }
                }
                else
                {
                    _errors.Add(new HDSLLogBase(Peek().Column, Peek().Row, $"No paths were provided for scanning.  Please provide at least one location for the scan to explore."));
                }
            }
            else
            {
                _errors.Add(new HDSLLogBase(Peek().Column, Peek().Row, $"'scan' expected."));
            }

            return new Scanning.DiskScanResultSet(-1, -1, -1, new Scanning.Timings());
        }

        /// <summary>
        /// Handles the interpretation of a bookmark definition statement
        /// 
        /// Purpose:
        /// Bookmarks cannot be used until they have been defined
        /// 
        /// Syntax:
        /// bookmark = path;
        /// 
        /// Examples:
        /// [homeDir] = 'C:\Users\SWDev';
        /// </summary>
        private void HandleBookmarkDefinitionStatement()
        {
            if (More() && Peek().Type == HDSLTokenTypes.BookmarkReference)
            {
                var markName = Pop().Literal;
                if (More() && Peek().Type == HDSLTokenTypes.Equal)
                {
                    Pop();
                    if (More(1) &&
                        Peek().Type == HDSLTokenTypes.String &&
                        (Peek(1).Type == HDSLTokenTypes.EndOfLine || Peek(1).Type == HDSLTokenTypes.EndOfFile))
                    {
                        var markValueToken = Pop();
                        var markValue = PathHelper.EnsurePath(markValueToken.Literal);
                        if (markValue != null)
                        {
                            var bm = new BookmarkItem()
                            {
                                Id = Guid.NewGuid(),
                                ItemName = markName
                            };

                            // determine what type of bookmark was defined
                            if (markValue.Where(c => c == ':').Count() == 1)
                            {
                                try
                                {
                                    if (Directory.Exists(markValue))
                                    {
                                        bm.Target = markValue;
                                    }
                                    else
                                    {
                                        _errors.Add(new HDSLLogBase(markValueToken.Column, markValueToken.Row, $"Full file or directory path expected."));
                                    }
                                }
                                catch (Exception ex)
                                {
                                    _errors.Add(new HDSLLogBase(markValueToken.Column, markValueToken.Row, $"Valid full file or directory path expected."));
                                }
                            }
                            else
                            {
                                _errors.Add(new HDSLLogBase(markValueToken.Column, markValueToken.Row, $"Valid full file or directory path expected."));
                            }

                            if (!string.IsNullOrWhiteSpace(bm.Target))
                            {
                                if (!_dh.GetBookmarks().Where(b => b.ItemName == bm.ItemName).Any())
                                {
                                    _dh.Insert(bm);
                                    _dh.WriteBookmarks();
                                }
                                else
                                {
                                    _dh.Update(bm);
                                    _dh.WriteBookmarks();
                                }
                            }
                        }
                        else
                        {
                            _errors.Add(new HDSLLogBase(Peek().Column, Peek().Row, $"Path '{markValueToken.Literal}' does not exist."));
                        }
                    }
                    else
                    {
                        _errors.Add(new HDSLLogBase(Peek().Column, Peek().Row, $"Unknown bookmark definition type."));
                    }
                }
                else
                {
                    _errors.Add(new HDSLLogBase(Peek().Column, Peek().Row, $"'=' expected."));
                }
            }
            else
            {
                _errors.Add(new HDSLLogBase(Peek().Column, Peek().Row, $"Bookmark expected."));
            }
        }

        /// <summary>
        /// Handles interpretation of a purge statement
        /// 
        /// Purpose:
        /// A purge statement removes entries from the database (it does not delete actual files)
        /// 
        /// Syntax:
        /// purge [bookmarks | exclusions | path[, path, path] [where clause]];
        /// </summary>
        private void HandlePurgeStatement()
        {
            if (More() && Peek().Type == HDSLTokenTypes.Purge)
            {
                // eat the purge
                Pop();

                if (More())
                {
                    if (Peek().Type == HDSLTokenTypes.Exclusions)
                    {
                        Pop();
                        _dh.ClearExclusions();
                    }
                    else if (Peek().Type == HDSLTokenTypes.Bookmarks)
                    {
                        Pop();
                        _dh.ClearBookmarks();
                    }
                    else
                    {
                        var targets = GetPathList();
                        if (_errors.Count == 0)
                        {
                            // the where clause is optional.
                            // If present, it further filters the files selected from the path
                            var queryDetail = string.Empty;
                            if (Peek().Type == HDSLTokenTypes.Where)
                            {
                                queryDetail = OperatorBase.ConvertClause(_tokens).ToString();
                            }

                            // execute the purge
                            try
                            {
                                _dh.PurgeQueried(queryDetail, targets);
                            }
                            catch (Exception ex)
                            {
                                _errors.Add(new HDSLLogBase(Peek().Column, Peek().Row, $"Exception encountered: '{ex}'."));
                            }
                        }
                    }
                }
            }
            else
            {
                _errors.Add(new HDSLLogBase(Peek().Column, Peek().Row, $"Keyword expected: 'purge'."));
            }
        }

        /// <summary>
        /// Handles interpretation of a find statement
        /// 
        /// Purpose:
        /// Find statements query the database for files and return them
        /// 
        /// Syntax:
        /// find <file regular expression> in <path> where *stuffs*
        /// 
        /// find [file pattern] [in/within/under [path[, path, path]] - defaults to current] [where clause];
        /// </summary>
        /// <returns>The results find statement</returns>
        private FindQueryResultSet HandleFindStatement()
        {
            if (More() && Peek().Type == HDSLTokenTypes.Find)
            {
                Pop();
            }

            // the wildcard expression defaults to "*.*".  Defining it explicitly is optional
            var wildcardExpression = "*.*";
            if (More() && Peek().Type == HDSLTokenTypes.String)
            {
                wildcardExpression = Pop().Literal;
            }

            // the depth clause is optional, and can take a comma seperated list of paths.
            // if left out then the system assumes the current directory.
            HDSLTokenTypes op = HDSLTokenTypes.Within;
            var targetPaths = new List<string>();
            if (More() &&
                (Peek().Type == HDSLTokenTypes.In ||
                Peek().Type == HDSLTokenTypes.Under ||
                Peek().Type == HDSLTokenTypes.Within))
            {
                op = Pop().Type;
            }

            if (More() && 
                (Peek().Type == HDSLTokenTypes.String || Peek().Type == HDSLTokenTypes.BookmarkReference))
            {
                targetPaths = GetPathList();
                if (_errors.Count > 0)
                {
                    return new FindQueryResultSet(new DiskItem[] { });
                }
            }
            else
            {
                targetPaths.Add(Environment.CurrentDirectory);
            }

            var results = new List<DiskItem>();

            // the where clause is optional.
            // If present, it further filters the files selected from the path
            var queryDetail = string.Empty;
            if (More() && Peek().Type == HDSLTokenTypes.Where)
            {
                var savePoint = Peek();
                queryDetail = OperatorBase.ConvertClause(_tokens)?.ToSQL();
                if (string.IsNullOrWhiteSpace(queryDetail))
                {
                    _errors.Add(new HDSLLogBase(savePoint.Column, savePoint.Row, $"Bad where clause."));
                }
            }

            if (_errors.Count == 0)
            {
                // execute the query and get the records
                try
                {
                    switch (op)
                    {
                        case HDSLTokenTypes.In:
                            results.AddRange(_dh.GetFilteredDiskItemsByIn(queryDetail, wildcardExpression, targetPaths));
                            break;
                        case HDSLTokenTypes.Within:
                            results.AddRange(_dh.GetFilteredDiskItemsByWithin(queryDetail, wildcardExpression, targetPaths));
                            break;
                        case HDSLTokenTypes.Under:
                            results.AddRange(_dh.GetFilteredDiskItemsByUnder(queryDetail, wildcardExpression, targetPaths));
                            break;
                    }
                }
                catch (Exception ex)
                {
                    _errors.Add(new HDSLLogBase(Peek().Column, Peek().Row, $"Exception encountered: '{ex}'."));
                }

                // Done
                return new FindQueryResultSet(results);
            }

            return new FindQueryResultSet(new DiskItem[] { });
        }

        #endregion
    }
}
