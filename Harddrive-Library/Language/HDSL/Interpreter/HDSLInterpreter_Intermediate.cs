// Copyright (c) Zain Al-Ahmary.  All rights reserved.
// Licensed under the MIT License, (the "License"); you may not use this file except in compliance with the License. 
// You may obtain a copy of the License at https://mit-license.org/

using HDDL.Data;
using HDDL.Language.HDSL.Where;
using HDDL.Language.HDSL.Where.Exceptions;
using HDDL.IO.Disk;
using System;
using System.Collections.Generic;
using System.Linq;

namespace HDDL.Language.HDSL.Interpreter
{
    /// <summary>
    /// Contains the HDSLInterpreter class' intermediate interpretation methods
    /// </summary>
    partial class HDSLInterpreter
    {
        private const long Default_Page_Index = 0;

        /// <summary>
        /// Interprets and returns a single path
        /// </summary>
        /// <param name="expandBookmarks">Whether or not to automatically expand bookmarks</param>
        /// <returns></returns>
        private string GetPath(bool expandBookmarks = true)
        {
            var forced = false;
            // if the force keyword shows up and the next token is a string or bookmark then continue
            if (More(1) &&
                Peek().Type == HDSLTokenTypes.Force &&
                (Peek(1).Type == HDSLTokenTypes.String || Peek(1).Type == HDSLTokenTypes.BookmarkReference))
            {
                Pop();
                forced = true;
            }

            // get the value in its proper form
            string path = null;
            if (Peek().Type == HDSLTokenTypes.BookmarkReference)
            {
                if (expandBookmarks)
                {
                    path = ApplyBookmarks(Pop());
                }
                else
                {
                    path = Pop().Code;
                }
            }
            else
            {
                path = Pop().Literal;
            }

            // process the value and ensure it is a proper path
            string result = null;
            if (BookmarkItem.HasBookmark(path))
            {
                // if there is a bookmark then expand it to properly test
                var expanded = ApplyBookmarks(path);
                result = PathHelper.EnsurePath(expanded, forced);

                // if we are not expanding the bookmarks then
                // we have to keep the non-expanded one after ensuring that the test succeeded.
                if (!expandBookmarks && !string.IsNullOrWhiteSpace(result))
                {
                    result = path;
                }
            }
            else
            {
                result = PathHelper.EnsurePath(path, forced);
            }

            if (!string.IsNullOrWhiteSpace(result))
            {
                return result;
            }

            return null;
        }

        /// <summary>
        /// Interprets and returns a comma seperated list of strings
        /// </summary>
        /// <param name="failOnNone">Whether or not the method should log an error if no paths are discovered</param>
        /// <param name="expandBookmarks">Whether or not to automatically expand bookmarks</param>
        /// <returns>A list containing the strings</returns>
        private List<string> GetPathList(bool failOnNone = true, bool expandBookmarks = true)
        {
            HDSLToken first = null;
            var results = new List<string>();
            // get the list of paths
            while (More() && Peek().Type == HDSLTokenTypes.String ||
                Peek().Type == HDSLTokenTypes.BookmarkReference ||
                Peek().Type == HDSLTokenTypes.Force)
            {
                // save the first one for error reporting
                if (first == null) first = Peek();

                var path = GetPath(expandBookmarks);
                if (!string.IsNullOrWhiteSpace(path))
                {
                    results.Add(path);
                }

                // check if we have at least 2 more tokens remaining, one is a comma and the next is a string or bookmark
                // if so, then this is a list
                if (More(2) &&
                    Peek().Type == HDSLTokenTypes.Comma &&
                    (Peek(1).Type == HDSLTokenTypes.String ||
                    Peek(1).Type == HDSLTokenTypes.BookmarkReference ||
                    Peek(1).Type == HDSLTokenTypes.Force))
                {
                    // strip the comma so the loop continues
                    Pop();
                }
            }

            if (failOnNone && results.Count == 0)
            {
                if (first != null)
                {
                    Report(new LogItemBase(first.Column, first.Row, $"No valid paths found."));
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

        /// <summary>
        /// Gathers the information required for a Find query and returns it
        /// 
        /// Syntax
        /// [filesystem | wards | watches | hashlogs] [columns columnref[, columnref]] [in/within/under [path[, path, path]] - defaults to current] [where clause] [group clause] [order clause] [paging clause];
        /// </summary>
        /// <param name="forcedTypeContext">Preassigns the type context, skipping that step</param>
        /// <param name="allowPaging">If true, allows the use of the paging clause.</param>
        /// <returns></returns>
        private FindQueryDetails GetFindDetails(bool allowPaging, Type? forcedTypeContext = null)
        {
            Type typeContext = GetTypeContext(forcedTypeContext);

            // the column header set is optional
            var columnHeaderSet = GetColumnHeaderSet(typeContext);
            if (!NoErrors())
            {
                return new FindQueryDetails()
                {
                    ResultsEmpty = true
                };
            }

            // the depth clause is optional, and can take a comma seperated list of paths.
            // if left out then the system assumes the current directory.
            var op = FindQueryDepths.Within;
            var targetPaths = new List<string>();
            if (More() &&
                (Peek().Type == HDSLTokenTypes.In ||
                Peek().Type == HDSLTokenTypes.Under ||
                Peek().Type == HDSLTokenTypes.Within))
            {
                if (typeContext == typeof(DiskItem))
                {
                    switch (Pop().Type)
                    {
                        case HDSLTokenTypes.In:
                            op = FindQueryDepths.In;
                            break;
                        case HDSLTokenTypes.Within:
                            op = FindQueryDepths.Within;
                            break;
                        case HDSLTokenTypes.Under:
                            op = FindQueryDepths.Under;
                            break;
                    }
                }
                else
                {
                    Report(new LogItemBase(Peek().Column, Peek().Row, $"In, Within, and Under are only valid when querying the file system."));
                    return new FindQueryDetails()
                    {
                        ResultsEmpty = true
                    };
                }
            }

            if (More() &&
                (Peek().Type == HDSLTokenTypes.String || Peek().Type == HDSLTokenTypes.BookmarkReference))
            {
                targetPaths = GetPathList();
                if (!NoErrors())
                {
                    return new FindQueryDetails()
                    {
                        ResultsEmpty = true
                    };
                }
            }
            else if (typeContext == typeof(DiskItem))
            {
                targetPaths.Add(Environment.CurrentDirectory);
            }

            // the where clause is optional.
            // If present, it further filters the files selected from the path
            OperatorBase queryDetail = null;
            if (More() && Peek().Type == HDSLTokenTypes.Where)
            {
                try
                {
                    var savePoint = Peek();
                    queryDetail = OperatorBase.ConvertClause(_tokens, new ClauseContext(_dh, typeContext), _currentStatement);
                    if (queryDetail == null)
                    {
                        Report(new LogItemBase(savePoint.Column, savePoint.Row, $"Bad where clause."));
                    }
                    else
                    {
                        ValidateWhereExpression(queryDetail, GetTestRecord(typeContext));
                    }
                }
                catch (WhereClauseException ex)
                {
                    Report(ex.AsHDSLLog());
                }
            }

            // the grouping and sorting clauses are optional
            var groupingSortingData = GetGroupingSortingSet(typeContext);
            if (!NoErrors())
            {
                return new FindQueryDetails()
                {
                    ResultsEmpty = true
                };
            }

            // the paging clause is optional
            var pagingDetails = GetPagingDetails();
            return new FindQueryDetails()
            {
                FurtherDetails = queryDetail,
                Method = op,
                Paths = targetPaths,
                ResultsEmpty = false,
                Columns = columnHeaderSet,
                TableContext = typeContext,
                GroupSortDetails = groupingSortingData,
                PageIndex = pagingDetails[0],
                RecordsPerPage = pagingDetails[1],
                AllowPaging = allowPaging
            };
        }

        /// <summary>
        /// Gets the current statement context from the next token
        /// </summary>
        /// <param name="forcedTypeContext">Preassigns the type context, skipping that step</param>
        /// <returns></returns>
        private Type GetTypeContext(Type? forcedTypeContext = null)
        {
            Type typeContext = typeof(DiskItem);
            if (forcedTypeContext == null)
            {
                if (Peek().Type == HDSLTokenTypes.FileSystem ||
                    Peek().Type == HDSLTokenTypes.Wards ||
                    Peek().Type == HDSLTokenTypes.Watches ||
                    Peek().Type == HDSLTokenTypes.HashLogs)
                {
                    switch (Pop().Type)
                    {
                        case HDSLTokenTypes.Wards:
                            typeContext = typeof(WardItem);
                            break;
                        case HDSLTokenTypes.Watches:
                            typeContext = typeof(WatchItem);
                            break;
                        case HDSLTokenTypes.HashLogs:
                            typeContext = typeof(DiskItemHashLogItem);
                            break;
                    }
                }
            }
            else
            {
                typeContext = forcedTypeContext;
            }

            return typeContext;
        }

        /// <summary>
        /// Interprets a timespan from the tokens
        /// 
        /// Format:
        /// d:h:m:s
        /// </summary>
        /// <returns></returns>
        private TimeSpan? GetTimeSpan(bool required = true)
        {
            TimeSpan? ts = null;
            const int Max_Colons = 3;

            var numbers = new Stack<HDSLToken>();
            var totalColons = 0;

            while (Peek().Type == HDSLTokenTypes.WholeNumber ||
                Peek().Type == HDSLTokenTypes.Colon)
            {
                switch (Peek().Type)
                {
                    case HDSLTokenTypes.WholeNumber:
                        numbers.Push(Pop());
                        break;
                    case HDSLTokenTypes.Colon:
                        if (totalColons > Max_Colons)
                        {
                            Report(new LogItemBase(Peek().Column, Peek().Row, $"Improper timespan format detected.  <days>:<hours>:<minutes>:<seconds> expected."));
                        }
                        numbers.Push(Pop());
                        totalColons++;
                        break;
                }
            }

            if (numbers.Count == 0 && required)
            {
                Report(new LogItemBase(Peek().Column, Peek().Row, $"Timespan expected."));
            }

            if (NoErrors())
            {
                // we need to determine which components of the timespan were supplied,
                // so that 5::30:10 will come out as 5 days, 0 hours, 30 minutes, 10 seconds and
                // 30:10 will come out as 30 minutes, 10 seconds
                var colonIndex = 0;
                var values = new int[] { 0, 0, 0, 0 };
                while (numbers.Count > 0 && colonIndex <= Max_Colons)
                {
                    if (numbers.Peek().Type == HDSLTokenTypes.WholeNumber)
                    {
                        values[colonIndex] = int.Parse(numbers.Pop().Literal);
                    }
                    else if (numbers.Peek().Type == HDSLTokenTypes.Colon)
                    {
                        numbers.Pop();
                        colonIndex++;
                    }
                    else
                    {
                        Report(new LogItemBase(numbers.Peek().Column, numbers.Peek().Row, $"Unknown token discovered. {numbers.Peek().Literal}"));
                    }
                }

                if (NoErrors())
                {
                    ts = new TimeSpan(values[3], values[2], values[1], values[0]);
                }
            }

            return ts;
        }

        /// <summary>
        /// Gathers a list of column name tokens and, with the provided type, returns the column mappings for the named tokens for that type
        /// </summary>
        /// <param name="forType">The type</param>
        /// <returns></returns>
        private ColumnNameMappingItem[] GetColumnMappingsForTypeByTokenSet(Type forType)
        {
            var tokens = new List<HDSLToken>();
            while (More() && Peek().Type == HDSLTokenTypes.ColumnName)
            {
                tokens.Add(Pop());
                if (Peek().Type == HDSLTokenTypes.Comma)
                {
                    Pop();
                }
            }

            if (tokens.Count > 0)
            {
                var columns = tokens.Select(t => _dh.GetColumnNameMappings(forType)
                                        .Where(m => m.Alias.Equals(t.Code, StringComparison.InvariantCultureIgnoreCase) ||
                                                m.Name.Equals(t.Code, StringComparison.InvariantCultureIgnoreCase)).SingleOrDefault()
                                    )
                    .Where(v => v != null)
                    .ToArray();
                if (columns.Length == tokens.Count)
                {
                    return columns.ToArray();
                }
                else
                {
                    // get the names of the missing columns and report them in an error
                    var missings =
                        (from t in tokens
                         where columns.Where(c =>
                            c.Name.Equals(t.Code, StringComparison.InvariantCultureIgnoreCase) ||
                            c.Name.Equals(t.Code, StringComparison.InvariantCultureIgnoreCase)).Any() == false
                         select t).ToArray();

                    var columnsEnding = missings.Length > 1 ? "s" : string.Empty;
                    Report(new LogItemBase(Peek().Column, Peek().Row, $"Type '{forType.Name}' does not have column{columnsEnding} '{string.Join("', '", missings.Select(m => m.Code))}'."));
                }
            }
            else
            {
                Report(new LogItemBase(Peek().Column, Peek().Row, $"Column name or alias expected."));
            }

            return new ColumnNameMappingItem[] { };
        }

        /// <summary>
        /// Converts a list of column names into a ColumnHeaderSet instance
        /// </summary>
        /// <param name="forType">The type of the header set is for (should be derived from HDDLRecordBase)</param>
        /// <returns></returns>
        private ColumnHeaderSet GetColumnHeaderSet(Type forType)
        {
            if (Peek().Type == HDSLTokenTypes.Columns ||
                Peek().Type == HDSLTokenTypes.OrderBy ||
                Peek().Type == HDSLTokenTypes.GroupBy)
            {
                Pop();

                var columns = GetColumnMappingsForTypeByTokenSet(forType);
                if (columns.Length > 0 && NoErrors())
                {
                    return new ColumnHeaderSet(columns, forType);
                }
            }

            return new ColumnHeaderSet(_dh, forType);
        }

        /// <summary>
        /// Gathers and packages grouping and sorting information for use in querying
        /// 
        /// Syntax:
        /// Group [columnref[, columnref]] Order [columnref[, columnref]]
        /// </summary>
        /// <param name="forType">The type of the header set is for (should be derived from HDDLRecordBase)</param>
        /// <returns></returns>
        private QueryGroupSortSet GetGroupingSortingSet(Type forType)
        {
            var result = new QueryGroupSortSet();

            while (
                NoErrors() &&
                More() &&
                (Peek().Type == HDSLTokenTypes.GroupBy ||
                Peek().Type == HDSLTokenTypes.OrderBy))
            {
                bool isOrderBy = Pop().Type == HDSLTokenTypes.OrderBy;
                var columns = GetColumnMappingsForTypeByTokenSet(forType);

                if (isOrderBy)
                {
                    result.OrderBy.AddRange(columns.Select(c => c.Name));

                    if (Peek().Type == HDSLTokenTypes.Asc)
                    {
                        Pop();
                    }
                    else if (Peek().Type == HDSLTokenTypes.Desc)
                    {
                        Pop();
                        result.AscendingSortOrder = false;
                    }
                }
                else
                {
                    result.GroupBy.AddRange(columns.Select(c => c.Name));
                }
            }

            // we always need to sort to allow for paging
            // default sorting is on the path field
            if (result.OrderBy.Count == 0)
            {
                var column = _dh.GetColumnNameMappings(forType).Where(cm => cm.Name.Equals("path", StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();
                result.OrderBy.Add(column.Name);
            }

            if (NoErrors())
            {
                return result;
            }

            return new QueryGroupSortSet();
        }

        /// <summary>
        /// Gathers and returns the paging information (page index and records per page) as a long[]
        /// 
        /// Syntax:
        /// Page (page index)
        /// 
        /// Page size is always fixed.
        /// </summary>
        /// <returns></returns>
        private long[] GetPagingDetails()
        {
            var result = new long[] { Default_Page_Index, HDSLConstants.Page_Size };
            if (Peek().Type == HDSLTokenTypes.PageIndex)
            {
                Pop();

                if (Peek().Type == HDSLTokenTypes.WholeNumber)
                {
                    result[0] = int.Parse(Pop().Literal);
                }
                else
                {
                    Report(new LogItemBase(Peek().Column, Peek().Row, $"Integer expected."));
                }
            }

            return result;
        }
    }
}
