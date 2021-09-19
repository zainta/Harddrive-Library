﻿// Copyright (c) Zain Al-Ahmary.  All rights reserved.
// Licensed under the MIT License, (the "License"); you may not use this file except in compliance with the License. 
// You may obtain a copy of the License at https://mit-license.org/

using HDDL.Collections;
using HDDL.Data;
using HDDL.HDSL.Logging;
using HDDL.HDSL.Where;
using LiteDB;
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
            var results = new List<DiskItem>();

            try
            {
                var done = false;

                while (!_tokens.Empty && _errors.Count == 0 && !done)
                {
                    switch (Peek().Type)
                    {
                        case HDSLTokenTypes.BookmarkReference:
                            HandleBookmarkDefinitionStatement();
                            break;
                        case HDSLTokenTypes.Purge:
                            HandlePurgeStatement();
                            break;
                        case HDSLTokenTypes.Find:
                            results.AddRange(HandleFindStatement());
                            break;
                        case HDSLTokenTypes.EndOfFile:
                            Pop();
                            done = true;
                            break;
                        case HDSLTokenTypes.EndOfLine:
                            Pop();
                            break;
                        default:
                            _errors.Add(new HDSLLogBase(Peek().Column, Peek().Row, $"Unexpected token '{Peek().Code}'."));
                            done = true;
                            break;
                    }

                    // Get rid of semicolons
                    if (More() && Peek().Type == HDSLTokenTypes.EndOfLine)
                    {
                        Pop();
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

        #endregion

        #region Statement Handlers

        /// <summary>
        /// Handles the interpretation of a bookmark definition statement
        /// 
        /// Purpose:
        /// Bookmarks cannot be used until they have been defined
        /// 
        /// Syntax:
        /// bookmark = path / full file path / path:wildcard pair;
        /// 
        /// Examples:
        /// [homeDir] = 'C:\Users\SWDev';
        /// [homeDirPng] = 'C:\Users\SWDev:*.png';
        /// [homeDirFavImg] = 'C:\Users\SWDev\fav.png';
        /// </summary>
        private void HandleBookmarkDefinitionStatement()
        {
            if (More() && Peek().Type == HDSLTokenTypes.BookmarkReference)
            {
                var markName = Pop().Literal;
                if (More() && Peek().Type == HDSLTokenTypes.Equal)
                {
                    Pop();
                    if (More() && Peek().Type == HDSLTokenTypes.String)
                    {
                        var markValueToken = Pop();
                        var markValue = markValueToken.Literal;
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
                                _dh.InsertBookmarks(bm);
                                _dh.WriteBookmarks();
                            }
                            else
                            {
                                _dh.UpdateBookmarks(bm);
                                _dh.WriteBookmarks();
                            }
                        }
                    }
                    else
                    {
                        _errors.Add(new HDSLLogBase(Peek().Column, Peek().Row, $"String expected."));
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
        /// purge [where clause];
        /// </summary>
        private void HandlePurgeStatement()
        {
            // eat the purge
            Pop();

            IEnumerable<DiskItem> targets;
            // the where clause is optional.
            // If pressent, it further filters the files selected from the path
            if (More() && Peek().Type == HDSLTokenTypes.Where)
            {
                targets = HandleWhereClause(null);
                _dh.DeleteDiskItems(targets);
            }
            else
            {
                _dh.DeleteAllDiskItems();
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
        /// find [file pattern] [in [path[, path, path]] - defaults to current] [where clause];
        /// </summary>
        /// <returns>The results find statement</returns>
        private DiskItem[] HandleFindStatement()
        {
            if (More() && Peek().Type == HDSLTokenTypes.Find)
            {
                Pop();
                // the wildcard expression defaults to "*.*".  Defining it explicitly is optional
                var wildcardExpression = "*.*";
                if (More() && Peek().Type == HDSLTokenTypes.String)
                {
                    wildcardExpression = Pop().Literal;
                }

                // the in clause is optional, and can take a comma seperated list of paths.
                // if left out then the system assumes the current directory.
                var targetPaths = new List<string>();
                if (More() && Peek().Type == HDSLTokenTypes.In)
                {
                    Pop();
                    // get the list of paths
                    while (More() && Peek().Type == HDSLTokenTypes.String || Peek().Type == HDSLTokenTypes.BookmarkReference)
                    {
                        if (Peek().Type == HDSLTokenTypes.BookmarkReference)
                        {
                            targetPaths.Add(_dh.ApplyBookmarks(Pop().Code));
                        }
                        else
                        {
                            targetPaths.Add(Pop().Literal);
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

                    // validate the list of paths to ensure they exist
                    foreach (var target in targetPaths)
                    {
                        try
                        {
                            if (!Directory.Exists(target))
                            {
                                _errors.Add(new HDSLLogBase(Peek().Column, Peek().Row, $"Invalid path: '{target}'."));
                            }
                        }
                        catch (IOException ex)
                        {
                            _errors.Add(new HDSLLogBase(Peek().Column, Peek().Row, $"Bad path: '{target}'."));
                        }
                    }

                    if (_errors.Count > 0)
                    {
                        return new DiskItem[] { };
                    }
                }

                var results = new List<DiskItem>();
                try
                {
                    results.AddRange(_dh.GetFilteredDiskItemsByPath(wildcardExpression, targetPaths));
                }
                catch (Exception ex)
                {
                    _errors.Add(new HDSLLogBase(Peek().Column, Peek().Row, $"Exception encountered: '{ex}'."));
                }

                // the where clause is optional.
                // If pressent, it further filters the files selected from the path
                if (More() && Peek().Type == HDSLTokenTypes.Where)
                {
                    results = HandleWhereClause(results).ToList();
                }

                // Done
                return results.ToArray();
            }
            else
            {
                _errors.Add(new HDSLLogBase(Peek().Column, Peek().Row, "'find' expected."));
            }

            return new DiskItem[] { };
        }

        /// <summary>
        /// Consumes a where clause, filtering the provided disk items
        /// 
        /// If the items set is null, then it will query the entire database directly
        /// </summary>
        /// <param name="items">The disk items to filter</param>
        private IEnumerable<DiskItem> HandleWhereClause(IEnumerable<DiskItem> items)
        {
            var clause = OperatorBase.ConvertClause(_tokens);
            return (from item in items where clause.Evaluate(item) select item);
        }

        #endregion
    }
}
