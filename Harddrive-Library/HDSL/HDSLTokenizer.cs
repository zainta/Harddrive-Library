// Copyright (c) Zain Al-Ahmary.  All rights reserved.
// Licensed under the MIT License, (the "License"); you may not use this file except in compliance with the License. 
// You may obtain a copy of the License at https://mit-license.org/

using HDDL.Collections;
using HDDL.HDSL.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using HDDL.Extensions;
using System.IO;

namespace HDDL.HDSL
{
    /// <summary>
    /// Handles breaking code strings down into their individual smallest parts and stores them in a publicly available ListStack<HDSLToken> property
    /// </summary>
    class HDSLTokenizer
    {
        private const int Minimum_Column = 1;
        private const int Minimum_Row = 1;

        /// <summary>
        /// The currently targeted HDSL code
        /// </summary>
        private ListStack<char> buffer;

        /// <summary>
        /// The current coordinates in the code
        /// </summary>
        private int col, row;

        /// <summary>
        /// Whether or not to tokenize whitespace
        /// </summary>
        private bool ignoreWhitespace;

        /// <summary>
        /// The resulting Tokens from the operation
        /// </summary>
        public ListStack<HDSLToken> Tokens { get; private set; }

        /// <summary>
        /// Any errors generated during the tokenization process
        /// </summary>
        public List<HDSLLogBase> Outcome { get; private set; }

        /// <summary>
        /// A list of tokens that cannot be generated
        /// </summary>
        public string[] Blacklist { get; private set; }

        /// <summary>
        /// Create a tokenizer for use
        /// </summary>
        /// <param name="ignoreWhitespace">Whether or not to generate whitespace tokens</param>
        public HDSLTokenizer(bool ignoreWhitespace)
        {
            this.ignoreWhitespace = ignoreWhitespace;
            Tokens = new ListStack<HDSLToken>();
            Outcome = new List<HDSLLogBase>();
        }

        /// <summary>
        /// Tokenizes the given code and stores the result in the Tokens class property
        /// </summary>
        /// <param name="code">The code to tokenize</param>
        /// <param name="blacklistedTokens">A set of tokens that cannot be generated and will result in an error if encountered</param>
        /// <returns>A result output log detailing any errors encountered</returns>
        public HDSLLogBase[] Tokenize(string code, params string[] blacklistedTokens)
        {
            Blacklist = blacklistedTokens;

            Outcome.Clear();
            buffer = new ListStack<char>(code);
            col = Minimum_Column;
            row = Minimum_Row;

            // Loop through the code and pick out the tokens one by one, in order of discovery
            while (!buffer.Empty)
            {
                if (More(1) && PeekStr(0, 2) == "--" && GetLineComment())
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
                else if((More() && Peek() == '\'') || (More(1) && PeekStr(length:2) == "@'"))
                {
                    if (GetString())
                    {
                        continue;
                    }
                }
                else if (More() && Peek() == ',') // Comma
                {
                    Add(new HDSLToken(HDSLTokenTypes.Comma, Pop(), row, col, ","));
                    continue;
                }
                else if (More() && Peek() == ':') // Colon
                {
                    Add(new HDSLToken(HDSLTokenTypes.Colon, Pop(), row, col, ":"));
                    continue;
                }
                else if (More() && char.IsDigit(Peek()) && GetNumbers()) // Whole and real numbers
                {
                    continue;
                }
                else if (More() && char.IsLetter(Peek()) && GetKeywords())
                {
                    continue;
                }
                else if (More() && GetOperators())
                {
                    continue;
                }
                else if (More() && Peek() == ';'  && GetEoL())
                {
                    continue;
                }
                else
                {
                    if (More())
                    {
                        Outcome.Add(new HDSLLogBase(col, row, $"Unknown character '{Peek()}'."));
                        break;
                    }
                }
            }

            Add(new HDSLToken(HDSLTokenTypes.EndOfLine, ';', col, row, ";"));
            Add(new HDSLToken(HDSLTokenTypes.EndOfFile, string.Empty, col, row, string.Empty));

            return Outcome.ToArray();
        }

        #region Utility Methods

        /// <summary>
        /// Returns a value indicating whether or not there are more characters beyond the given minimum
        /// </summary>
        /// <param name="min">The minimum number of characters to test for</param>
        /// <returns>True if there are more than min, false otherwise</returns>
        private bool More(int min = 0)
        {
            return buffer.Count > min;
        }

        /// <summary>
        /// Returns the given index in the buffer
        /// </summary>
        /// <param name="offset">The index</param>
        /// <returns>The character found at the given location</returns>
        /// <exception cref="IndexOutOfRangeException"/>
        char Peek(int offset = 0)
        {
            return buffer.Peek(offset);
        }

        /// <summary>
        /// Returns the range of characters starting at the given index
        /// </summary>
        /// <param name="offset">The start index</param>
        /// <param name="length">The number of characters</param>
        /// <returns>The characters (as a string) found at the given location</returns>
        /// <exception cref="IndexOutOfRangeException"/>
        string PeekStr(int offset = 0, int length = 1)
        {
            StringBuilder sb = new StringBuilder();
            var terminationPoint = offset + (length - 1);
            for (int i = offset; i <= terminationPoint; i++)
            {
                sb.Append(buffer.Peek(i));
            }

            return sb.ToString();
        }

        /// <summary>
        /// Removes the 0-index character and returns it
        /// </summary>
        /// <returns>The character</returns>
        /// <exception cref="IndexOutOfRangeException"/>
        char Pop()
        {
            Step(Peek());
            return buffer.Pop();
        }

        /// <summary>
        /// Removes length characters, beginning at the offset, and returns them
        /// </summary>
        /// <returns>The characters</returns>
        /// <param name="length">The number of characters to return</param>
        /// <param name="offset">The index to start from</param>
        /// <exception cref="IndexOutOfRangeException"/>
        string PopStr(int offset = 0, int length = 1)
        {
            StringBuilder sb = new StringBuilder();
            var terminationPoint = offset + (length - 1); ;
            for (int i = offset; i <= terminationPoint; i++)
            {
                Step(Peek());
                sb.Append(buffer.Pop());
            }

            return sb.ToString();
        }

        /// <summary>
        /// Removes the given number of characters and trashes them
        /// </summary>
        /// <param name="count">The number of characters to remove</param>
        /// <exception cref="IndexOutOfRangeException"/>
        void Eat(int count = 1)
        {
            for (int i = 0; i < count; i++)
            {
                Step(Peek());
                Eat();
            }
        }

        /// <summary>
        /// Advances the current coordinates taking into account the character skipped over
        /// </summary>
        /// <param name="item">The character advanced past</param>
        void Step(char item)
        {
            if (item == '\n')
            {
                row++;
            }
            else if (item == '\r')
            {
                col = Minimum_Column;
            }
            else
            {
                col++;
            }
        }

        /// <summary>
        /// Retrieves and returns everything between the first occurrance of start and end, taking into account escapes
        /// </summary>
        /// <param name="start">The starting character</param>
        /// <param name="end">The ending character</param>
        /// <param name="escape">The character used to escape start and end to allow them inside of the run</param>
        /// <returns>An array containing the literal of the paired set in the first slot and the encoded paired set with escapes in the second</returns>
        string[] GetPairedSet(char start, char end, char? escape = null)
        {
            var literal = new StringBuilder();
            var encoded = new StringBuilder();
            if (More() && Peek() == start)
            {
                encoded.Append(Pop());

                bool done = false;
                while (!done)
                {
                    if (More() == false)
                    {
                        Outcome.Add(new HDSLLogBase(col, row, string.Format("End of file before paired set closed.  '{0}' expected.", end)));
                        return null;
                    }

                    if (escape.HasValue &&
                        Peek() == escape.Value) // found the escape character
                    {
                        // check to see if the character after the escape is the ending character
                        if (More(1) && Peek(1) == end) 
                        { 
                            // yes
                            literal.Append(Peek());
                            encoded.Append(Pop());
                            encoded.Append(Pop());
                        }
                        else if (More(1) && Peek(1) == escape.Value)
                        {
                            // if this is an escaped escape character then we have to copy it over
                            // and remove the escape
                            Pop();

                            literal.Append(Peek());
                            encoded.Append(Pop());
                        }
                        else
                        { 
                            // no
                            literal.Append(Peek());
                            literal.Append(Peek(1));
                            encoded.Append(Pop());
                            encoded.Append(Pop());
                        }
                    }
                    else if (Peek() == end) // this is a non-escaped end.  we're done
                    {
                        done = true;
                        encoded.Append(Pop());
                    }
                    else // just copy everything else over
                    {
                        literal.Append(Peek());
                        encoded.Append(Pop());
                    }
                }

                return new string[] { literal.ToString(), encoded.ToString() };
            }
            else
            {
                Outcome.Add(new HDSLLogBase(col, row, string.Format("Unexpected character found.  Expected '{1}'.", start)));
                return null;
            }
        }

        /// <summary>
        /// Checks the text against all available file / directory attributes
        /// </summary>
        /// <param name="text">The text to check</param>
        /// <returns></returns>
        private bool IsDiskAttributeName(string text)
        {
            var attributes = FileAttributes.Normal.Enumerate<FileAttributes>();
            if (attributes.Select(a => a.ToString()).Where(a => a.Equals(text, StringComparison.InvariantCultureIgnoreCase)).Any())
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Gets the properly cased version of the provided file / directory attribute
        /// </summary>
        /// <param name="text">the name of an attribute, in any case</param>
        /// <returns></returns>
        private string GetDiskAttributeName(string text)
        {
            var attributes = FileAttributes.Normal.Enumerate<FileAttributes>();
            var selection = attributes.Select(a => a.ToString()).Where(a => a.Equals(text, StringComparison.InvariantCultureIgnoreCase)).SingleOrDefault()?.ToString();

            return selection;
        }

        /// <summary>
        /// Adds the given token to the list if it is not in the forbidden set, adds an error message if it is
        /// </summary>
        /// <param name="token">The token to add</param>
        /// <returns>The unmodified token</returns>
        private HDSLToken Add(HDSLToken token)
        {
            if ((from b in Blacklist 
                 where 
                    token.Type.ToString().StartsWith(b, StringComparison.InvariantCultureIgnoreCase) ||
                    b.Equals(token.Literal, StringComparison.InvariantCultureIgnoreCase)
                 select b).Any())
            {
                Outcome.Add(new HDSLLogBase(token.Column, token.Row, $"Token '{token.Literal}' is disallowed."));
            }
            else
            {
                Tokens.Add(token);
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
                Add(new HDSLToken(HDSLTokenTypes.BookmarkReference, bookmark[1], row, col, bookmark[0]));
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
                Add(new HDSLToken(HDSLTokenTypes.String, bookmark[1], row, col, bookmark[0]));
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
                else if (buffer.Empty)
                {
                    if (number.Length > 0)
                    {
                        done = true;
                    }
                    else
                    {
                        Outcome.Add(new HDSLLogBase(col, row, "End of file instead of digit."));
                        return false;
                    }
                }
                else if (More() && Peek() == '.')
                {
                    if (!decimaled)
                    {
                        decimaled = true;
                        if (buffer.Count > 1 && char.IsDigit(Peek(1))) // make sure there is a number after the decimal point
                        {
                            number.Append(PopStr(0, 2));
                        }
                        else if (buffer.Count == 1) // there is a just a deicmal point with nothing after it
                        {
                            number.Append(Pop());
                            number.Append('0'); // add a free 0 after the decimal point
                        }
                    }
                    else
                    {
                        Outcome.Add(new HDSLLogBase(col, row, "Duplicate decimal point."));
                        return false;
                    }
                }
                else if (char.IsDigit(Peek()))
                {
                    number.Append(Pop());
                }
            }

            Add(new HDSLToken(decimaled ? HDSLTokenTypes.RealNumber : HDSLTokenTypes.WholeNumber, number.ToString(), row, col, number.ToString()));
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
                Add(new HDSLToken(HDSLTokenTypes.DateTime, bookmark[1], row, col, bookmark[0]));
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
                    Add(new HDSLToken(HDSLTokenTypes.Whitespace, ws.ToString(), row, col, ws.ToString()));
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

                Add(new HDSLToken(HDSLTokenTypes.Comment, sb.ToString(), row, col, sb.ToString()));
            }
            else
            {
                Outcome.Add(new HDSLLogBase(col, row, "Comment declaration expected."));
                return false;
            }

            return true;
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
                    token = new HDSLToken(HDSLTokenTypes.Now, keyword.ToString(), row, col, text);
                }
                else if (text == "in")
                {
                    token = new HDSLToken(HDSLTokenTypes.In, keyword.ToString(), row, col, text);
                }
                else if (text == "find")
                {
                    token = new HDSLToken(HDSLTokenTypes.Find, keyword.ToString(), row, col, text);
                }
                else if (text == "asc")
                {
                    token = new HDSLToken(HDSLTokenTypes.Asc, keyword.ToString(), row, col, text);
                }
                else if (text == "dsc")
                {
                    token = new HDSLToken(HDSLTokenTypes.Dsc, keyword.ToString(), row, col, text);
                }
                else if (text == "purge")
                {
                    token = new HDSLToken(HDSLTokenTypes.Purge, keyword.ToString(), row, col, text);
                }
                else if (text == "within")
                {
                    token = new HDSLToken(HDSLTokenTypes.Within, keyword.ToString(), row, col, text);
                }
                else if (text == "where")
                {
                    token = new HDSLToken(HDSLTokenTypes.Where, keyword.ToString(), row, col, text);
                }
                else if (text == "size")
                {
                    token = new HDSLToken(HDSLTokenTypes.Size, keyword.ToString(), row, col, text);
                }
                else if (text == "written")
                {
                    token = new HDSLToken(HDSLTokenTypes.Written, keyword.ToString(), row, col, text);
                }
                else if (text == "accessed")
                {
                    token = new HDSLToken(HDSLTokenTypes.Accessed, keyword.ToString(), row, col, text);
                }
                else if (text == "created")
                {
                    token = new HDSLToken(HDSLTokenTypes.Created, keyword.ToString(), row, col, text);
                }
                else if (text == "extension")
                {
                    token = new HDSLToken(HDSLTokenTypes.Extension, keyword.ToString(), row, col, text);
                }
                else if (text == "last")
                {
                    token = new HDSLToken(HDSLTokenTypes.LastScan, keyword.ToString(), row, col, text);
                }
                else if (text == "first")
                {
                    token = new HDSLToken(HDSLTokenTypes.FirstScan, keyword.ToString(), row, col, text);
                }
                else if (text == "name")
                {
                    token = new HDSLToken(HDSLTokenTypes.Name, keyword.ToString(), row, col, text);
                }
                else if (text == "and")
                {
                    token = new HDSLToken(HDSLTokenTypes.And, keyword.ToString(), row, col, text);
                }
                else if (text == "or")
                {
                    token = new HDSLToken(HDSLTokenTypes.Or, keyword.ToString(), row, col, text);
                }
                else if (text == "sort")
                {
                    token = new HDSLToken(HDSLTokenTypes.Sort, keyword.ToString(), row, col, text);
                }
                else if (text == "by")
                {
                    token = new HDSLToken(HDSLTokenTypes.By, keyword.ToString(), row, col, text);
                }
                else if (text == "under")
                {
                    token = new HDSLToken(HDSLTokenTypes.Under, keyword.ToString(), row, col, text);
                }
                else if (text == "scan")
                {
                    token = new HDSLToken(HDSLTokenTypes.Scan, keyword.ToString(), row, col, text);
                }
                else if (text == "check")
                {
                    token = new HDSLToken(HDSLTokenTypes.Check, keyword.ToString(), row, col, text);
                }
                else if (text == "quiet")
                {
                    token = new HDSLToken(HDSLTokenTypes.QuietMode, keyword.ToString(), row, col, text);
                }
                else if (text == "spinner")
                {
                    token = new HDSLToken(HDSLTokenTypes.SpinnerMode, keyword.ToString(), row, col, text);
                }
                else if (text == "text")
                {
                    token = new HDSLToken(HDSLTokenTypes.TextMode, keyword.ToString(), row, col, text);
                }
                else if (text == "progress")
                {
                    token = new HDSLToken(HDSLTokenTypes.ProgressMode, keyword.ToString(), row, col, text);
                }
                else if (text == "include")
                {
                    token = new HDSLToken(HDSLTokenTypes.Include, keyword.ToString(), row, col, text);
                }
                else if (text == "exclude")
                {
                    token = new HDSLToken(HDSLTokenTypes.Exclude, keyword.ToString(), row, col, text);
                }
                else if (text == "dynamic")
                {
                    token = new HDSLToken(HDSLTokenTypes.Dynamic, keyword.ToString(), row, col, text);
                }
                else if (text == "exclusions")
                {
                    token = new HDSLToken(HDSLTokenTypes.Exclusions, keyword.ToString(), row, col, text);
                }
                else if (text == "bookmarks")
                {
                    token = new HDSLToken(HDSLTokenTypes.Bookmarks, keyword.ToString(), row, col, text);
                }
                else if (text == "ward")
                {
                    token = new HDSLToken(HDSLTokenTypes.Ward, keyword.ToString(), row, col, text);
                }
                else if (text == "watch")
                {
                    token = new HDSLToken(HDSLTokenTypes.Watch, keyword.ToString(), row, col, text);
                }
                else if (text == "wards")
                {
                    token = new HDSLToken(HDSLTokenTypes.Wards, keyword.ToString(), row, col, text);
                }
                else if (text == "watches")
                {
                    token = new HDSLToken(HDSLTokenTypes.Watches, keyword.ToString(), row, col, text);
                }
                else if (text == "passive")
                {
                    token = new HDSLToken(HDSLTokenTypes.Passive, keyword.ToString(), row, col, text);
                }
                else if (text == "force")
                {
                    token = new HDSLToken(HDSLTokenTypes.Force, keyword.ToString(), row, col, text);
                }
                else if (text == "set")
                {
                    token = new HDSLToken(HDSLTokenTypes.Set, keyword.ToString(), row, col, text);
                }
                else if (text == "out")
                {
                    token = new HDSLToken(HDSLTokenTypes.Out, keyword.ToString(), row, col, text);
                }
                else if (text == "reset")
                {
                    token = new HDSLToken(HDSLTokenTypes.Reset, keyword.ToString(), row, col, text);
                }
                else if (text == "error")
                {
                    token = new HDSLToken(HDSLTokenTypes.Error, keyword.ToString(), row, col, text);
                }
                else if (text == "standard")
                {
                    token = new HDSLToken(HDSLTokenTypes.Standard, keyword.ToString(), row, col, text);
                }
                else if (text == "hashlogs")
                {
                    token = new HDSLToken(HDSLTokenTypes.HashLogs, keyword.ToString(), row, col, text);
                }
                else if (IsDiskAttributeName(text))
                {
                    token = new HDSLToken(HDSLTokenTypes.AttributeLiteral, keyword.ToString(), row, col, GetDiskAttributeName(text));
                }
                else
                {
                    Outcome.Add(new HDSLLogBase(col, row, string.Format("Unknown keyword: '{0}'", keyword.ToString())));
                    return false;
                }
            }
            else
            {
                Outcome.Add(new HDSLLogBase(col, row, string.Format("Unknown keyword: '{0}'", keyword.ToString())));
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

            if (buffer.Count > 0)
            {
                if (Peek() == '=')
                {
                    token = new HDSLToken(HDSLTokenTypes.Equal, Pop(), row, col, "=");
                }
                else if (Peek() == '>')
                {
                    token = new HDSLToken(HDSLTokenTypes.GreaterThan, Pop(), row, col, ">");
                }
                else if (Peek() == '<')
                {
                    token = new HDSLToken(HDSLTokenTypes.LessThan, Pop(), row, col, "<");
                }
                else if (Peek() == '+')
                {
                    token = new HDSLToken(HDSLTokenTypes.Has, Pop(), row, col, "+");
                }
                else if (Peek() == '-')
                {
                    token = new HDSLToken(HDSLTokenTypes.HasNot, Pop(), row, col, "-");
                }
                else if (Peek() == '.')
                {
                    token = new HDSLToken(HDSLTokenTypes.Dot, Pop(), row, col, ".");
                }

                if (buffer.Count > 1)
                {
                    if (PeekStr(0, 2) == "!=")
                    {
                        token = new HDSLToken(HDSLTokenTypes.NotEqual, PopStr(0, 2), row, col, "!=");
                    }
                    else if (PeekStr(0, 2) == ">=")
                    {
                        token = new HDSLToken(HDSLTokenTypes.GreaterOrEqual, PopStr(0, 2), row, col, ">=");
                    }
                    else if (PeekStr(0, 2) == "<=")
                    {
                        token = new HDSLToken(HDSLTokenTypes.LessOrEqual, PopStr(0, 2), row, col, "<=");
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
                Add(new HDSLToken(HDSLTokenTypes.EndOfLine, Pop(), row, col, ";"));
                return true;
            }

            return false;
        }
    }
}
