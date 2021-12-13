// Copyright (c) Zain Al-Ahmary.  All rights reserved.
// Licensed under the MIT License, (the "License"); you may not use this file except in compliance with the License. 
// You may obtain a copy of the License at https://mit-license.org/

using HDDL.Data;
using HDDL.Language.HDSL.Permissions;
using System;
using System.IO;
using System.Linq;
using System.Text;

namespace HDDL.Language.HDSL
{
    /// <summary>
    /// Handles breaking code strings down into their individual smallest parts and stores them in a publicly available ListStack<HDSLToken> property
    /// </summary>
    class HDSLTokenizer : TokenizerBase<HDSLToken>
    {
        /// <summary>
        /// Whether or not to tokenize whitespace
        /// </summary>
        private bool ignoreWhitespace;

        /// <summary>
        /// A list containing the only generatable tokens
        /// </summary>
        public HDSLListManager PermittedTokenTypes { get; private set; }

        /// <summary>
        /// Used for _column header tokens
        /// </summary>
        private DataHandler _dh;

        /// <summary>
        /// Create a tokenizer for use
        /// </summary>
        /// <param name="ignoreWhitespace">Whether or not to generate whitespace tokens</param>
        /// <param name="dh">The datahandler to use</param>
        public HDSLTokenizer(bool ignoreWhitespace, DataHandler dh) : base()
        {
            this.ignoreWhitespace = ignoreWhitespace;
            _dh = dh;
            PermittedTokenTypes = null;
        }

        /// <summary>
        /// Tokenizes the given code and stores the result in the Tokens class property
        /// </summary>
        /// <param name="code">The code to tokenize</param>
        /// <param name="tokenPermissions">An optional list instance defining which tokens can be generated</param>
        /// <returns>A result output log detailing any errors encountered</returns>
        public LogItemBase[] Tokenize(string code, HDSLListManager tokenPermissions = null)
        {
            PermittedTokenTypes = tokenPermissions;

            Outcome.Clear();
            _buffer.Clear();
            _buffer.AddRange(code);
            _col = Minimum_Column;
            _row = Minimum_Row;

            // Loop through the code and pick out the tokens one by one, in order of discovery
            while (!_buffer.Empty)
            {
                if (More(1) && PeekStr(0, 2) == "--" && GetLineComment())
                {
                    continue;
                }
                else if (More(1) && PeekStr(0, 2) == "/*" && GetMultiLineComment())
                {
                    continue;
                }
                else if (More() && char.IsWhiteSpace(Peek()) && GetWhitespace())
                {
                    continue;
                }
                else if (More() && Peek() == '#' && GetDatetime()) // DateTime
                {
                    continue;
                }
                else if (More() && Peek() == '[' && GetBookmarkReference()) // Bookmark Reference
                {
                    continue;
                }
                else if ((More() && Peek() == '\'') || (More(1) && PeekStr(length: 2) == "@'"))
                {
                    if (GetString())
                    {
                        continue;
                    }
                }
                else if (More() && Peek() == ',') // Comma
                {
                    Add(new HDSLToken(HDSLTokenTypes.Comma, Pop(), _row, _col, ","));
                    continue;
                }
                else if (More() && Peek() == ':') // Colon
                {
                    Add(new HDSLToken(HDSLTokenTypes.Colon, Pop(), _row, _col, ":"));
                    continue;
                }
                else if (More() && char.IsDigit(Peek()) && GetNumbers()) // Whole and real numbers
                {
                    continue;
                }
                else if (More() && GetOperators())
                {
                    continue;
                }
                else if (More() && Peek() == ';' && GetEoL())
                {
                    continue;
                }
                else if (More() && char.IsLetter(Peek()) && Get_columnNames())
                {
                    continue;
                }
                else if (More() && char.IsLetter(Peek()) && GetKeywords())
                {
                    continue;
                }
                else
                {
                    if (More())
                    {
                        Outcome.Add(new LogItemBase(_col, _row, $"Unknown character '{Peek()}'."));
                        break;
                    }
                }
            }

            Add(new HDSLToken(HDSLTokenTypes.EndOfLine, ';', _col, _row, ";"));
            Add(new HDSLToken(HDSLTokenTypes.EndOfFile, string.Empty, _col, _row, string.Empty));

            return Outcome.ToArray();
        }

        #region Utility Methods

        /// <summary>
        /// Checks the text against all available file / directory attributes
        /// </summary>
        /// <param name="text">The text to check</param>
        /// <returns></returns>
        private bool IsDiskAttributeName(string text)
        {
            var attributes = Enum.GetValues<FileAttributes>();
            if (attributes.Select(a => a.ToString()).Where(a => a.Equals(text, StringComparison.InvariantCultureIgnoreCase)).Any())
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Retrieves the matching mapping associated with the given text, either via name or alias.
        /// </summary>
        /// <param name="text">The text to test</param>
        /// <returns>The _columnNameMappingItem instance, if found, or null</returns>
        private ColumnNameMappingItem Get_columnNameOrAlias(string text)
        {
            var matches = (from mapping in _dh.GetAllColumnNameMappings()
                           where
                                mapping.Name.Equals(text, StringComparison.InvariantCultureIgnoreCase) ||
                                mapping.Alias.Equals(text, StringComparison.InvariantCultureIgnoreCase)
                           select mapping).FirstOrDefault();

            return matches;
        }

        /// <summary>
        /// Gets the properly cased version of the provided file / directory attribute
        /// </summary>
        /// <param name="text">the name of an attribute, in any case</param>
        /// <returns></returns>
        private string GetDiskAttributeName(string text)
        {
            var attributes = Enum.GetValues<FileAttributes>();
            var selection = attributes.Select(a => a.ToString()).Where(a => a.Equals(text, StringComparison.InvariantCultureIgnoreCase)).SingleOrDefault()?.ToString();

            return selection;
        }

        /// <summary>
        /// Adds the given token to the list if it is allowed
        /// </summary>
        /// <param name="token">The token to add</param>
        /// <returns>The unmodified token</returns>
        private HDSLToken Add(HDSLToken token)
        {
            if (PermittedTokenTypes == null)
            {
                Tokens.Add(token);
            }
            else
            {
                if (PermittedTokenTypes.Graylist.Where(a => a == token.Type.ToString().ToLower()).Any())
                {
                    Tokens.Add(token);
                }
                else
                {
                    Outcome.Add(new LogItemBase(token.Column, token.Row, $"Token '{token.Literal}' is disallowed."));
                }
            }

            return token;
        }

        #endregion

        #region Datatypes

        /// <summary>
        /// Gathers a bookmark reference token and add it to the list
        /// </summary>
        /// <returns>Whether or not a token was generated (if not, implies an error)</returns>
        bool GetBookmarkReference()
        {
            var bookmark = GetPairedSet('[', ']', '\\');
            if (bookmark != null)
            {
                Add(new HDSLToken(HDSLTokenTypes.BookmarkReference, bookmark[1], _row, _col, bookmark[0]));
                return true;
            }
            return false;
        }

        /// <summary>
        /// Gathers a string token and add it to the list
        /// </summary>
        /// <returns>Whether or not a token was generated (if not, implies an error)</returns>
        bool GetString()
        {
            bool cantEscape = (Peek() == '@');
            if (cantEscape)
            {
                Pop();
            }

            char? escape = '\\';
            if (cantEscape) escape = null;

            var bookmark = GetPairedSet('\'', '\'', escape);
            if (bookmark != null)
            {
                Add(new HDSLToken(HDSLTokenTypes.String, bookmark[1], _row, _col, bookmark[0]));
                return true;
            }
            return false;
        }

        /// <summary>
        /// Gathers a numerical (whole and real) token and add it to the list
        /// </summary>
        /// <returns>Whether or not a token was generated (if not, implies an error)</returns>
        bool GetNumbers()
        {
            var number = new StringBuilder();
            var done = false;
            var decimaled = false;
            while (!done)
            {
                if (More() && !char.IsDigit(Peek()))
                {
                    done = true;
                }
                else if (_buffer.Empty)
                {
                    if (number.Length > 0)
                    {
                        done = true;
                    }
                    else
                    {
                        Outcome.Add(new LogItemBase(_col, _row, "End of file instead of digit."));
                        return false;
                    }
                }
                else if (More() && Peek() == '.')
                {
                    if (!decimaled)
                    {
                        decimaled = true;
                        if (_buffer.Count > 1 && char.IsDigit(Peek(1))) // make sure there is a number after the decimal point
                        {
                            number.Append(PopStr(0, 2));
                        }
                        else if (_buffer.Count == 1) // there is a just a deicmal point with nothing after it
                        {
                            number.Append(Pop());
                            number.Append('0'); // add a free 0 after the decimal point
                        }
                    }
                    else
                    {
                        Outcome.Add(new LogItemBase(_col, _row, "Duplicate decimal point."));
                        return false;
                    }
                }
                else if (char.IsDigit(Peek()))
                {
                    number.Append(Pop());
                }
            }

            Add(new HDSLToken(decimaled ? HDSLTokenTypes.RealNumber : HDSLTokenTypes.WholeNumber, number.ToString(), _row, _col, number.ToString()));
            return true;
        }

        /// <summary>
        /// Gathers a datetime token and add it to the list
        /// </summary>
        /// <returns>Whether or not a token was generated (if not, implies an error)</returns>
        bool GetDatetime()
        {
            var bookmark = GetPairedSet('#', '#');
            if (bookmark != null)
            {
                Add(new HDSLToken(HDSLTokenTypes.DateTime, bookmark[1], _row, _col, bookmark[0]));
                return true;
            }
            return false;
        }

        #endregion

        /// <summary>
        /// Gathers a whitespace token and add it to the list
        /// 
        /// This method is greedy, in that it will gather complete whitespace strings
        /// rather than having multiple adjacent whitespace character tokens
        /// </summary>
        /// <returns>Whether or not a token was generated (if not, implies an error)</returns>
        bool GetWhitespace()
        {
            var ws = new StringBuilder();
            while (More() && char.IsWhiteSpace(Peek()))
            {
                ws.Append(Pop());
            }

            if (ws.Length > 0)
            {
                if (!ignoreWhitespace)
                {
                    Add(new HDSLToken(HDSLTokenTypes.Whitespace, ws.ToString(), _row, _col, ws.ToString()));
                }
                return true;
            }

            return false;
        }

        /// <summary>
        /// Gathers a single line comment token and add it to the list
        /// 
        /// Single Line Comments run from their inception (the -- starting them) to the end of the line.
        /// </summary>
        /// <returns>Whether or not a token was generated (if not, implies an error)</returns>
        private bool GetLineComment()
        {
            if (More(1) && PeekStr(0, 2) == "--")
            {
                // remove the comment start
                PopStr(0, 2);

                // run until we reach the end of the line or the file
                var sb = new StringBuilder();
                while (More() && Peek() != '\n')
                {
                    sb.Append(Pop());
                }

                if (More() && Peek() == '\n')
                {
                    sb.Append(Pop());
                }

                Add(new HDSLToken(HDSLTokenTypes.Comment, sb.ToString(), _row, _col, sb.ToString()));
            }
            else
            {
                Outcome.Add(new LogItemBase(_col, _row, "Comment declaration expected."));
                return false;
            }

            return true;
        }

        /// <summary>
        /// Gathers a multi line comment token and add it to the list
        /// 
        /// Multi line comments run until they are terminated, starting with a /* and ending with */.
        /// </summary>
        /// <returns>Whether or not a token was generated (if not, implies an error)</returns>
        private bool GetMultiLineComment()
        {
            var comment = GetPairedSet("/*", "*/", '\\');
            if (comment != null)
            {
                Add(new HDSLToken(HDSLTokenTypes.MultiLineComment, comment[1], _row, _col, comment[0]));
                return true;
            }
            return false;
        }

        /// <summary>
        /// Gathers a keyword token and add it to the list
        /// </summary>
        /// <returns>Whether or not a token was generated (if not, implies an error)</returns>
        bool GetKeywords()
        {
            var keyword = new StringBuilder();
            while (More() && char.IsLetter(Peek()))
            {
                keyword.Append(Pop());
            }

            HDSLToken token = null;
            if (keyword.Length > 1)
            {
                var text = keyword.ToString().ToLower();
                if (text == "now")
                {
                    token = new HDSLToken(HDSLTokenTypes.Now, keyword.ToString(), _row, _col, text);
                }
                else if (text == "in")
                {
                    token = new HDSLToken(HDSLTokenTypes.In, keyword.ToString(), _row, _col, text);
                }
                else if (text == "find")
                {
                    token = new HDSLToken(HDSLTokenTypes.Find, keyword.ToString(), _row, _col, text);
                }
                else if (text == "asc")
                {
                    token = new HDSLToken(HDSLTokenTypes.Asc, keyword.ToString(), _row, _col, text);
                }
                else if (text == "desc")
                {
                    token = new HDSLToken(HDSLTokenTypes.Desc, keyword.ToString(), _row, _col, text);
                }
                else if (text == "purge")
                {
                    token = new HDSLToken(HDSLTokenTypes.Purge, keyword.ToString(), _row, _col, text);
                }
                else if (text == "within")
                {
                    token = new HDSLToken(HDSLTokenTypes.Within, keyword.ToString(), _row, _col, text);
                }
                else if (text == "where")
                {
                    token = new HDSLToken(HDSLTokenTypes.Where, keyword.ToString(), _row, _col, text);
                }
                else if (text == "and")
                {
                    token = new HDSLToken(HDSLTokenTypes.And, keyword.ToString(), _row, _col, text);
                }
                else if (text == "or")
                {
                    token = new HDSLToken(HDSLTokenTypes.Or, keyword.ToString(), _row, _col, text);
                }
                else if (text == "under")
                {
                    token = new HDSLToken(HDSLTokenTypes.Under, keyword.ToString(), _row, _col, text);
                }
                else if (text == "scan")
                {
                    token = new HDSLToken(HDSLTokenTypes.Scan, keyword.ToString(), _row, _col, text);
                }
                else if (text == "check")
                {
                    token = new HDSLToken(HDSLTokenTypes.Check, keyword.ToString(), _row, _col, text);
                }
                else if (text == "quiet")
                {
                    token = new HDSLToken(HDSLTokenTypes.QuietMode, keyword.ToString(), _row, _col, text);
                }
                else if (text == "spinner")
                {
                    token = new HDSLToken(HDSLTokenTypes.SpinnerMode, keyword.ToString(), _row, _col, text);
                }
                else if (text == "text")
                {
                    token = new HDSLToken(HDSLTokenTypes.TextMode, keyword.ToString(), _row, _col, text);
                }
                else if (text == "progress")
                {
                    token = new HDSLToken(HDSLTokenTypes.ProgressMode, keyword.ToString(), _row, _col, text);
                }
                else if (text == "include")
                {
                    token = new HDSLToken(HDSLTokenTypes.Include, keyword.ToString(), _row, _col, text);
                }
                else if (text == "exclude")
                {
                    token = new HDSLToken(HDSLTokenTypes.Exclude, keyword.ToString(), _row, _col, text);
                }
                else if (text == "dynamic")
                {
                    token = new HDSLToken(HDSLTokenTypes.Dynamic, keyword.ToString(), _row, _col, text);
                }
                else if (text == "exclusions")
                {
                    token = new HDSLToken(HDSLTokenTypes.Exclusions, keyword.ToString(), _row, _col, text);
                }
                else if (text == "bookmarks")
                {
                    token = new HDSLToken(HDSLTokenTypes.Bookmarks, keyword.ToString(), _row, _col, text);
                }
                else if (text == "ward")
                {
                    token = new HDSLToken(HDSLTokenTypes.Ward, keyword.ToString(), _row, _col, text);
                }
                else if (text == "watch")
                {
                    token = new HDSLToken(HDSLTokenTypes.Watch, keyword.ToString(), _row, _col, text);
                }
                else if (text == "wards")
                {
                    token = new HDSLToken(HDSLTokenTypes.Wards, keyword.ToString(), _row, _col, text);
                }
                else if (text == "watches")
                {
                    token = new HDSLToken(HDSLTokenTypes.Watches, keyword.ToString(), _row, _col, text);
                }
                else if (text == "passive")
                {
                    token = new HDSLToken(HDSLTokenTypes.Passive, keyword.ToString(), _row, _col, text);
                }
                else if (text == "force")
                {
                    token = new HDSLToken(HDSLTokenTypes.Force, keyword.ToString(), _row, _col, text);
                }
                else if (text == "set")
                {
                    token = new HDSLToken(HDSLTokenTypes.Set, keyword.ToString(), _row, _col, text);
                }
                else if (text == "out")
                {
                    token = new HDSLToken(HDSLTokenTypes.Out, keyword.ToString(), _row, _col, text);
                }
                else if (text == "reset")
                {
                    token = new HDSLToken(HDSLTokenTypes.Reset, keyword.ToString(), _row, _col, text);
                }
                else if (text == "error")
                {
                    token = new HDSLToken(HDSLTokenTypes.Error, keyword.ToString(), _row, _col, text);
                }
                else if (text == "standard")
                {
                    token = new HDSLToken(HDSLTokenTypes.Standard, keyword.ToString(), _row, _col, text);
                }
                else if (text == "hashlogs")
                {
                    token = new HDSLToken(HDSLTokenTypes.HashLogs, keyword.ToString(), _row, _col, text);
                }
                else if (text == "_columns")
                {
                    token = new HDSLToken(HDSLTokenTypes.Columns, keyword.ToString(), _row, _col, text);
                }
                else if (text == "group")
                {
                    token = new HDSLToken(HDSLTokenTypes.GroupBy, keyword.ToString(), _row, _col, text);
                }
                else if (text == "order")
                {
                    token = new HDSLToken(HDSLTokenTypes.OrderBy, keyword.ToString(), _row, _col, text);
                }
                else if (text == "_columnmappings")
                {
                    token = new HDSLToken(HDSLTokenTypes.ColumnMappings, keyword.ToString(), _row, _col, text);
                }
                else if (text == "filesystem")
                {
                    token = new HDSLToken(HDSLTokenTypes.FileSystem, keyword.ToString(), _row, _col, text);
                }
                else if (text == "alias")
                {
                    token = new HDSLToken(HDSLTokenTypes.Alias, keyword.ToString(), _row, _col, text);
                }
                else if (text == "span")
                {
                    token = new HDSLToken(HDSLTokenTypes.Span, keyword.ToString(), _row, _col, text);
                }
                else if (text == "default")
                {
                    token = new HDSLToken(HDSLTokenTypes.Default, keyword.ToString(), _row, _col, text);
                }
                else if (IsDiskAttributeName(text))
                {
                    token = new HDSLToken(HDSLTokenTypes.AttributeLiteral, keyword.ToString(), _row, _col, GetDiskAttributeName(text));
                }
                else
                {
                    Outcome.Add(new LogItemBase(_col, _row, string.Format("Unknown keyword: '{0}'", keyword.ToString())));
                    return false;
                }
            }
            else
            {
                Outcome.Add(new LogItemBase(_col, _row, string.Format("Unknown keyword: '{0}'", keyword.ToString())));
                return false;
            }

            Add(token);
            return true;
        }

        /// <summary>
        /// Gathers an operator token and add it to the list
        /// </summary>
        /// <returns>Whether or not a token was generated (if not, implies an error)</returns>
        bool GetOperators()
        {
            HDSLToken token = null;

            if (_buffer.Count > 0)
            {
                if (Peek() == '=')
                {
                    token = new HDSLToken(HDSLTokenTypes.Equal, Pop(), _row, _col, "=");
                }
                else if (Peek() == '>')
                {
                    token = new HDSLToken(HDSLTokenTypes.GreaterThan, Pop(), _row, _col, ">");
                }
                else if (Peek() == '<')
                {
                    token = new HDSLToken(HDSLTokenTypes.LessThan, Pop(), _row, _col, "<");
                }
                else if (Peek() == '+')
                {
                    token = new HDSLToken(HDSLTokenTypes.Has, Pop(), _row, _col, "+");
                }
                else if (Peek() == '-')
                {
                    token = new HDSLToken(HDSLTokenTypes.HasNot, Pop(), _row, _col, "-");
                }
                else if (Peek() == '.')
                {
                    token = new HDSLToken(HDSLTokenTypes.Dot, Pop(), _row, _col, ".");
                }
                else if (Peek() == '~')
                {
                    token = new HDSLToken(HDSLTokenTypes.Like, Pop(), _row, _col, "~");
                }

                if (_buffer.Count > 1)
                {
                    if (PeekStr(0, 2) == "!=")
                    {
                        token = new HDSLToken(HDSLTokenTypes.NotEqual, PopStr(0, 2), _row, _col, "!=");
                    }
                    else if (PeekStr(0, 2) == ">=")
                    {
                        token = new HDSLToken(HDSLTokenTypes.GreaterOrEqual, PopStr(0, 2), _row, _col, ">=");
                    }
                    else if (PeekStr(0, 2) == "<=")
                    {
                        token = new HDSLToken(HDSLTokenTypes.LessOrEqual, PopStr(0, 2), _row, _col, "<=");
                    }
                }
            }

            if (token != null)
            {
                Add(token);
            }
            return token != null;
        }

        /// <summary>
        /// Gathers an End of Line token and add it to the list
        /// </summary>
        /// <returns>Whether or not a token was generated (if not, implies an error)</returns>
        private bool GetEoL()
        {
            if (Peek() == ';')
            {
                Add(new HDSLToken(HDSLTokenTypes.EndOfLine, Pop(), _row, _col, ";"));
                return true;
            }

            return false;
        }

        /// <summary>
        /// Gathers case-insensitive tokens of the _column names (either exact or using their aliases)
        /// </summary>
        /// <returns></returns>
        private bool Get_columnNames()
        {
            var keyword = new StringBuilder();
            var i = 0;
            while (More(i) && char.IsLetter(Peek(i)))
            {
                keyword.Append(Peek(i));
                i++;
            }

            ColumnNameMappingItem mapping = Get_columnNameOrAlias(keyword.ToString());
            if (mapping != null)
            {
                Add(new HDSLToken(HDSLTokenTypes.ColumnName, mapping.Name, _row, _col, mapping.Alias));

                // remove the characters
                PopStr(0, keyword.Length);
                return true;
            }

            return false;
        }
    }
}
