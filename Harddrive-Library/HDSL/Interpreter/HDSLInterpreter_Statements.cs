// Copyright (c) Zain Al-Ahmary.  All rights reserved.
// Licensed under the MIT License, (the "License"); you may not use this file except in compliance with the License. 
// You may obtain a copy of the License at https://mit-license.org/

using HDDL.Data;
using HDDL.HDSL.Logging;
using HDDL.HDSL.Results;
using HDDL.HDSL.Where;
using HDDL.HDSL.Where.Exceptions;
using HDDL.IO.Disk;
using HDDL.Scanning.Results;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace HDDL.HDSL.Interpreter
{
    /// <summary>
    /// Contains the HDSLInterpreter class' statement interpretation methods
    /// </summary>
    partial class HDSLInterpreter
    {
        /// <summary>
        /// Resets the output back to the default
        /// 
        /// Purpose:
        /// Resets where either, or both, of the console output streams write to
        /// Also handles resetting the column header set back to default
        /// 
        /// Syntax:
        /// Reset out | standard | error | columnmappings;
        /// </summary>
        private void HandleResetStatement()
        {
            if (Peek().Type == HDSLTokenTypes.Reset)
            {
                Pop();
                if (More())
                {
                    if (Peek().Type == HDSLTokenTypes.Out)
                    {
                        Pop();
                        // check for EoF / EoL
                        if (Peek().Type == HDSLTokenTypes.EndOfLine ||
                        Peek().Type == HDSLTokenTypes.EndOfFile)
                        {
                            ResetStandardOutputToDefault();
                            ResetStandardErrorToDefault();
                        }
                        else
                        {
                            _errors.Add(new HDSLLogBase(Peek().Column, Peek().Row, $"';' or end of file expected."));
                        }
                    }
                    else if (Peek().Type == HDSLTokenTypes.Standard)
                    {
                        Pop();
                        // check for EoF / EoL
                        if (Peek().Type == HDSLTokenTypes.EndOfLine ||
                        Peek().Type == HDSLTokenTypes.EndOfFile)
                        {
                            ResetStandardOutputToDefault();
                        }
                        else
                        {
                            _errors.Add(new HDSLLogBase(Peek().Column, Peek().Row, $"';' or end of file expected."));
                        }
                    }
                    else if (Peek().Type == HDSLTokenTypes.Error)
                    {
                        Pop();
                        // check for EoF / EoL
                        if (Peek().Type == HDSLTokenTypes.EndOfLine ||
                        Peek().Type == HDSLTokenTypes.EndOfFile)
                        {
                            ResetStandardErrorToDefault();
                        }
                        else
                        {
                            _errors.Add(new HDSLLogBase(Peek().Column, Peek().Row, $"';' or end of file expected."));
                        }
                    }
                    else if (Peek().Type == HDSLTokenTypes.ColumnMappings)
                    {
                        Pop();
                        // check for EoF / EoL
                        if (Peek().Type == HDSLTokenTypes.EndOfLine ||
                        Peek().Type == HDSLTokenTypes.EndOfFile)
                        {
                            _dh.ResetColumnNameMappingTable();
                        }
                        else
                        {
                            _errors.Add(new HDSLLogBase(Peek().Column, Peek().Row, $"';' or end of file expected."));
                        }
                    }
                    else
                    {
                        _errors.Add(new HDSLLogBase(Peek().Column, Peek().Row, $"'out', 'standard', 'error', or 'columnmappings' expected."));
                    }
                }
            }
            else
            {
                _errors.Add(new HDSLLogBase(Peek().Column, Peek().Row, $"'reset' expected."));
            }
        }

        /// <summary>
        /// Allows the modification of various language and system settings through HDSL
        /// 
        /// Purpose:
        /// Changing where "text" mode of certain statement's send their output
        /// 
        /// Note that not providing a path will result in the behavior of "Reset out;"
        /// Syntax:
        /// set out | standard | error path | alias [filesystem | wards | watches | hashlogs] columnref,  span (width in characters) | alias [filesystem | wards | watches | hashlogs] columnref 'alias string';
        /// </summary>
        private void HandleSetStatement()
        {
            if (Peek().Type == HDSLTokenTypes.Set)
            {
                Pop();

                if (More())
                {
                    bool error = false;
                    bool standard = false;
                    switch (Peek().Type)
                    {
                        case HDSLTokenTypes.Out:
                            Pop();

                            error = true;
                            standard = true;
                            break;
                        case HDSLTokenTypes.Error:
                            Pop();

                            error = true;
                            break;
                        case HDSLTokenTypes.Standard:
                            Pop();

                            standard = true;
                            break;
                        case HDSLTokenTypes.Alias:
                            Pop();

                            HandleSetAliasStatement();
                            break;
                        default:
                            _errors.Add(new HDSLLogBase(Peek().Column, Peek().Row, $"'out', 'standard', 'error', or 'alias' expected."));
                            break;
                    }

                    if (_errors.Count > 0) return;

                    // check for EoF / EoL
                    if (Peek().Type == HDSLTokenTypes.EndOfLine ||
                    Peek().Type == HDSLTokenTypes.EndOfFile)
                    {
                        if (error || standard)
                        {
                            var path = GetPath(true);
                            if (!string.IsNullOrWhiteSpace(path))
                            {
                                try
                                {
                                    var strm = new StreamWriter(File.OpenWrite(path));
                                    strm.AutoFlush = true;

                                    if (standard)
                                    {
                                        Console.SetOut(strm);
                                    }

                                    if (error)
                                    {
                                        Console.SetError(strm);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    _errors.Add(new HDSLLogBase(Peek().Column, Peek().Row, $"Error attempting to setup new console stream during 'set out' statement.\n{ex}"));
                                }
                            }
                            else
                            {
                                if (standard)
                                {
                                    ResetStandardOutputToDefault();
                                }

                                if (error)
                                {
                                    ResetStandardErrorToDefault();
                                }
                            }
                        }
                    }
                    else
                    {
                        _errors.Add(new HDSLLogBase(Peek().Column, Peek().Row, $"';' or end of file expected."));
                    }
                }
                else
                {
                    _errors.Add(new HDSLLogBase(Peek().Column, Peek().Row, $"'out', 'standard', 'error', or 'alias' expected."));
                }
            }
            else
            {
                _errors.Add(new HDSLLogBase(Peek().Column, Peek().Row, $"'set' expected."));
            }
        }

        /// <summary>
        /// Handles the Set Alias sub-statement type
        /// 
        /// Purpose:
        /// Allows the alteration of aliases via HDSL code and setting the width of columns
        /// 
        /// Syntax:
        /// set alias [filesystem | wards | watches | hashlogs] columnref, span (width in characters);
        /// set alias [filesystem | wards | watches | hashlogs] columnref, 'new alias string';
        /// </summary>
        private void HandleSetAliasStatement()
        {
            var typeContext = GetTypeContext();

            if (Peek().Type == HDSLTokenTypes.ColumnName)
            {
                var column = Pop().Code;
                if (Peek().Type == HDSLTokenTypes.Comma)
                {
                    Pop();
                    if (Peek().Type == HDSLTokenTypes.String)
                    {
                        var newAlias = Pop().Literal;
                        var target = _dh.GetMappingByNameAndType(column, typeContext);

                        // make sure it isn't taken
                        var dupe = _dh.GetMappingByNameAndType(newAlias, typeContext);
                        if (dupe == null)
                        {
                            target.Alias = newAlias;
                            _dh.Update(target);
                            _dh.WriteColumnNameMappings();
                        }
                        else
                        {
                            if (dupe.Id != target.Id)
                            {
                                _errors.Add(new HDSLLogBase(Peek().Column, Peek().Row, $"Column '{dupe.Name}' already has the alias '{dupe.Alias}'."));
                            }
                        }
                    }
                    else if (Peek().Type == HDSLTokenTypes.Span)
                    {
                        Pop();
                        if (Peek().Type == HDSLTokenTypes.WholeNumber)
                        {
                            var target = _dh.GetMappingByNameAndType(column, typeContext);
                            target.DisplayWidth = int.Parse(Pop().Literal);
                            _dh.Update(target);
                            _dh.WriteColumnNameMappings();
                        }
                        else
                        {
                            _errors.Add(new HDSLLogBase(Peek().Column, Peek().Row, $"Whole number expected."));
                        }
                    }
                    else
                    {
                        _errors.Add(new HDSLLogBase(Peek().Column, Peek().Row, $"'span' or new alias string expected."));
                    }
                }
                else
                {
                    _errors.Add(new HDSLLogBase(Peek().Column, Peek().Row, $"Comma (,) expected."));
                }
            }
            else
            {
                _errors.Add(new HDSLLogBase(Peek().Column, Peek().Row, $"Column reference expected."));
            }
        }

        /// <summary>
        /// Immediately performs a disk scan followed by entering passive mode and watching for changes in the given area, while respecting exclusions
        /// 
        /// Purpose:
        /// Allows setting up monitoring of disk areas
        /// 
        /// Syntax:
        /// watch [passive] [path[, path, path] - defaults to current];
        /// </summary>
        private void HandleWatchDefinition()
        {
            if (Peek().Type == HDSLTokenTypes.Watch)
            {
                Pop();

                bool initiallyPassive = false;
                if (Peek().Type == HDSLTokenTypes.Passive)
                {
                    Pop();
                    initiallyPassive = true;
                }

                // the paths are optional, defaulting to the current directory
                var scanPaths = GetPathList();
                if (scanPaths.Count == 0)
                {
                    scanPaths.Add(Environment.CurrentDirectory);
                }

                if (_errors.Count == 0)
                {
                    foreach (var path in scanPaths)
                    {
                        var watch = new WatchItem()
                        {
                            Id = Guid.NewGuid(),
                            InPassiveMode = initiallyPassive,
                            Path = path,
                            Target = null
                        };

                        var dupe = _dh.GetWatches().Where(w => w.Path == watch.Path).SingleOrDefault();
                        if (dupe == null)
                        {
                            _dh.Insert(watch);
                        }
                        else
                        {
                            dupe.InPassiveMode = false;
                            _dh.Update(dupe);
                        }
                    }

                    // check for EoF / EoL
                    if (Peek().Type == HDSLTokenTypes.EndOfLine ||
                    Peek().Type == HDSLTokenTypes.EndOfFile)
                    {
                        _dh.WriteWatches();
                    }
                    else
                    {
                        _errors.Add(new HDSLLogBase(Peek().Column, Peek().Row, $"';' or end of file expected."));
                    }
                }
            }
            else
            {
                _errors.Add(new HDSLLogBase(Peek().Column, Peek().Row, $"'watch' expected."));
            }
        }

        /// <summary>
        /// Allows scheduling of a periodic integrity check through HDSL
        /// 
        /// Purpose:
        /// Allows scheduling of a periodic integrity check
        /// 
        /// Syntax:
        /// ward (time interval) [in/within/under [path[, path, path]] - defaults to current] [where clause] [group clause] [order clause];
        /// </summary>
        private void HandleWardDefinition()
        {
            if (Peek().Type == HDSLTokenTypes.Ward)
            {
                Pop();

                var interval = GetTimeSpan();
                if (interval.HasValue)
                {
                    var details = GetFindDetails(typeof(DiskItem));
                    if (_errors.Count == 0 && !details.ResultsEmpty)
                    {
                        // wards are single target, in that each one represents a single path that should receive an integrity check
                        // generate one ward record for each of the supplied paths
                        foreach (var path in details.Paths)
                        {
                            var whereKeyword = details.FurtherDetails == null ? string.Empty : "where ";
                            var ward = new WardItem()
                            {
                                Id = Guid.NewGuid(),
                                HDSL = $"check quiet {details.Method.ToString().ToLower()} '{path.Replace(@"\", @"\\")}' {whereKeyword}{details.FurtherDetails};",
                                NextScan = DateTime.Now,
                                Path = path,
                                Target = null,
                                Interval = interval.Value
                            };

                            // check for EoF / EoL
                            if (Peek().Type == HDSLTokenTypes.EndOfLine ||
                            Peek().Type == HDSLTokenTypes.EndOfFile)
                            {
                                var dupe = _dh.GetWards().Where(w => w.Path == ward.Path).SingleOrDefault();
                                if (dupe == null)
                                {
                                    _dh.Insert(ward);
                                }
                                else
                                {
                                    ward.Id = dupe.Id;
                                    _dh.Update(ward);
                                }
                            }
                            else
                            {
                                _errors.Add(new HDSLLogBase(Peek().Column, Peek().Row, $"';' or end of file expected."));
                            }

                        }

                        _dh.WriteWards();
                    }
                }
            }
            else
            {
                _errors.Add(new HDSLLogBase(Peek().Column, Peek().Row, $"'ward' expected."));
            }
        }

        /// <summary>
        /// Allows the execution of an integrity check through HDSL script
        /// 
        /// Purpose:
        /// Allows scripts to run integrity scan
        /// 
        /// Syntax:
        /// check [spinner|progress|text|quiet - defaults to text] [columns columnref[, columnref]] [in/within/under [path[, path, path]] - defaults to current] [where clause] [group clause] [order clause];
        /// </summary>
        private HDSLIntegrityOutcome HandleIntegrityCheck()
        {
            if (More() && Peek().Type == HDSLTokenTypes.Check)
            {
                Pop();

                var displayMode = GetDisplayMode();
                var findResult = HandleFindStatement(typeof(DiskItem));

                if (findResult != null)
                {
                    // don't integrity scan directories.
                    var files = findResult.Records.Where(di => di is DiskItem && ((DiskItem)di).IsFile).Select(di => (DiskItem)di).ToList();
                    if (files.Count > 0)
                    {
                        var scan = new Scanning.IntegrityScanEventWrapper(_dh, files, true, displayMode, findResult.Columns);
                        if (scan.Go())
                        {
                            return AddStatement(scan.Result);
                        }
                    }
                }
            }
            else
            {
                _errors.Add(new HDSLLogBase(Peek().Column, Peek().Row, $"'check' expected."));
            }

            return new HDSLIntegrityOutcome(new DiskItem[] { }, new DiskItem[] { }, new ColumnHeaderSet(_dh, typeof(DiskItem)), _currentStatement.ToString());
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
                                        _dh.GetExclusions().Where(e => e.Path == p).Any() == false
                                      select
                                          new ExclusionItem()
                                          {
                                              Id = Guid.NewGuid(),
                                              Path = p
                                          }).ToArray();

                    // check for EoF / EoL
                    if (Peek().Type == HDSLTokenTypes.EndOfLine ||
                    Peek().Type == HDSLTokenTypes.EndOfFile)
                    {
                        _dh.Insert(exclusions);
                        _dh.WriteExclusions();
                    }
                    else
                    {
                        _errors.Add(new HDSLLogBase(Peek().Column, Peek().Row, $"';' or end of file expected."));
                    }
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
                                        paths.Where(p => p.Equals(e.Path, StringComparison.InvariantCultureIgnoreCase)).Any() == true
                                      select e).ToArray();

                    // check for EoF / EoL
                    if (Peek().Type == HDSLTokenTypes.EndOfLine ||
                    Peek().Type == HDSLTokenTypes.EndOfFile)
                    {
                        _dh.Delete(exclusions);
                        _dh.WriteExclusions();
                    }
                    else
                    {
                        _errors.Add(new HDSLLogBase(Peek().Column, Peek().Row, $"';' or end of file expected."));
                    }
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
        private HDSLScanOutcome HandleScanStatement()
        {
            if (More() && Peek().Type == HDSLTokenTypes.Scan)
            {
                Pop();

                var displayMode = GetDisplayMode();
                var scanPaths = GetPathList();
                if (scanPaths.Count > 0)
                {
                    var dsw = new Scanning.DiskScanEventWrapper(_dh, scanPaths, true, displayMode, new ColumnHeaderSet(_dh, typeof(DiskItem)));
                    if (dsw.Go())
                    {
                        return AddStatement(dsw.Result);
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

            return new HDSLScanOutcome(new DiskItem[] { }, -1, -1, -1, new Scanning.Timings(), new ColumnHeaderSet(_dh, typeof(DiskItem)), _currentStatement.ToString());
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
                                // check for EoF / EoL
                                if (Peek().Type == HDSLTokenTypes.EndOfLine ||
                                Peek().Type == HDSLTokenTypes.EndOfFile)
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
                                else
                                {
                                    _errors.Add(new HDSLLogBase(Peek().Column, Peek().Row, $"';' or end of file expected."));
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
        /// purge [bookmarks | exclusions | watches | wards | hashlogs] | [path[, path, path] [where clause]];
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

                        // check for EoF / EoL
                        if (Peek().Type == HDSLTokenTypes.EndOfLine ||
                        Peek().Type == HDSLTokenTypes.EndOfFile)
                        {
                            _dh.ClearExclusions();
                        }
                        else
                        {
                            _errors.Add(new HDSLLogBase(Peek().Column, Peek().Row, $"';' or end of file expected."));
                        }
                    }
                    else if (Peek().Type == HDSLTokenTypes.Bookmarks)
                    {
                        Pop();

                        // check for EoF / EoL
                        if (Peek().Type == HDSLTokenTypes.EndOfLine ||
                        Peek().Type == HDSLTokenTypes.EndOfFile)
                        {
                            _dh.ClearBookmarks();
                        }
                        else
                        {
                            _errors.Add(new HDSLLogBase(Peek().Column, Peek().Row, $"';' or end of file expected."));
                        }
                    }
                    else if (Peek().Type == HDSLTokenTypes.Watches)
                    {
                        Pop();

                        // check for EoF / EoL
                        if (Peek().Type == HDSLTokenTypes.EndOfLine ||
                        Peek().Type == HDSLTokenTypes.EndOfFile)
                        {
                            _dh.ClearWatches();
                        }
                        else
                        {
                            _errors.Add(new HDSLLogBase(Peek().Column, Peek().Row, $"';' or end of file expected."));
                        }
                    }
                    else if (Peek().Type == HDSLTokenTypes.Wards)
                    {
                        Pop();

                        // check for EoF / EoL
                        if (Peek().Type == HDSLTokenTypes.EndOfLine ||
                        Peek().Type == HDSLTokenTypes.EndOfFile)
                        {
                            _dh.ClearWards();
                        }
                        else
                        {
                            _errors.Add(new HDSLLogBase(Peek().Column, Peek().Row, $"';' or end of file expected."));
                        }
                    }
                    else if (Peek().Type == HDSLTokenTypes.HashLogs)
                    {
                        Pop();

                        // check for EoF / EoL
                        if (Peek().Type == HDSLTokenTypes.EndOfLine ||
                        Peek().Type == HDSLTokenTypes.EndOfFile)
                        {
                            _dh.ClearDiskItemHashLogs();
                        }
                        else
                        {
                            _errors.Add(new HDSLLogBase(Peek().Column, Peek().Row, $"';' or end of file expected."));
                        }
                    }
                    else
                    {
                        var targets = GetPathList();
                        if (_errors.Count == 0)
                        {
                            // the where clause is optional.
                            // If present, it further filters the files selected from the path
                            OperatorBase queryDetail = null;
                            if (Peek().Type == HDSLTokenTypes.Where)
                            {
                                try
                                {
                                    queryDetail = OperatorBase.ConvertClause(_tokens, new ClauseContext(_dh, typeof(DiskItem)), _currentStatement);
                                    if (queryDetail != null)
                                    {
                                        ValidateWhereExpression(queryDetail);
                                    }
                                }
                                catch (WhereClauseException ex)
                                {
                                    _errors.Add(ex.AsHDSLLog());
                                }
                            }

                            if (_errors.Count == 0)
                            {
                                // execute the purge
                                try
                                {
                                    // check for EoF / EoL
                                    if (Peek().Type == HDSLTokenTypes.EndOfLine ||
                                    Peek().Type == HDSLTokenTypes.EndOfFile)
                                    {
                                        _dh.PurgeQueried(queryDetail.ToSQL(), targets);
                                    }
                                    else
                                    {
                                        _errors.Add(new HDSLLogBase(Peek().Column, Peek().Row, $"';' or end of file expected."));
                                    }
                                }
                                catch (Exception ex)
                                {
                                    _errors.Add(new HDSLLogBase(Peek().Column, Peek().Row, $"Exception encountered: '{ex}'."));
                                }
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
        /// find [filesystem - default] [columns columnref[, columnref]] [in/within/under [path[, path, path]] - defaults to current] [where clause] [group clause] [order clause];
        /// find [wards | watches | hashlogs] [columns columnref[, columnref]] [path[, path, path] - defaults to current] [where clause] [group clause] [order clause];
        /// </summary>
        /// <param name="forcedTypeContext">Preassigns the type context, skipping that step</param>
        /// <returns>The results find statement</returns>
        private HDSLResultBag HandleFindStatement(Type? forcedTypeContext = null)
        {
            if (More() && Peek().Type == HDSLTokenTypes.Find)
            {
                Pop();
            }

            var details = GetFindDetails(forcedTypeContext);
            if (_errors.Count == 0 && !details.ResultsEmpty)
            {
                HDSLResultBag result = null;
                if (details.TableContext == typeof(DiskItem))
                {
                    // execute the query and get the records
                    var results = new List<DiskItem>();
                    try
                    {
                        switch (details.Method)
                        {
                            case FindQueryDepths.In:
                                results.AddRange(_dh.GetFilteredDiskItemsByIn(details.FurtherDetails?.ToSQL(), details.GroupSortDetails?.ToSQL(), details.Paths));
                                break;
                            case FindQueryDepths.Within:
                                results.AddRange(_dh.GetFilteredDiskItemsByWithin(details.FurtherDetails?.ToSQL(), details.GroupSortDetails?.ToSQL(), details.Paths));
                                break;
                            case FindQueryDepths.Under:
                                results.AddRange(_dh.GetFilteredDiskItemsByUnder(details.FurtherDetails?.ToSQL(), details.GroupSortDetails?.ToSQL(), details.Paths));
                                break;
                        }
                    }
                    catch (Exception ex)
                    {
                        _errors.Add(new HDSLLogBase(Peek().Column, Peek().Row, $"Exception encountered: '{ex}'."));
                    }

                    // Done
                    result = new HDSLResultBag(results, details.Columns, details.TableContext, _currentStatement.ToString());
                }
                else if (details.TableContext == typeof(WardItem))
                {
                    var results = new List<WardItem>();
                    results.AddRange(_dh.GetFilteredWards(details.FurtherDetails?.ToSQL(), details.GroupSortDetails?.ToSQL(), details.Paths));
                    result = new HDSLResultBag(results, details.Columns, details.TableContext, _currentStatement.ToString());
                }
                else if (details.TableContext == typeof(WatchItem))
                {
                    var results = new List<WatchItem>();
                    results.AddRange(_dh.GetFilteredWatches(details.FurtherDetails?.ToSQL(), details.GroupSortDetails?.ToSQL(), details.Paths));
                    result = new HDSLResultBag(results, details.Columns, details.TableContext, _currentStatement.ToString());
                }
                else if (details.TableContext == typeof(DiskItemHashLogItem))
                {
                    var results = new List<DiskItemHashLogItem>();
                    results.AddRange(_dh.GetFilteredHashLogs(details.FurtherDetails?.ToSQL(), details.GroupSortDetails?.ToSQL(), details.Paths));
                    result = new HDSLResultBag(results, details.Columns, details.TableContext, _currentStatement.ToString());
                }

                // check for EoF / EoL
                if (Peek().Type == HDSLTokenTypes.EndOfLine ||
                Peek().Type == HDSLTokenTypes.EndOfFile)
                {
                    return result;
                }
                else
                {
                    _errors.Add(new HDSLLogBase(Peek().Column, Peek().Row, $"';' or end of file expected."));
                }
            }

            return null;
        }
    }
}
