// Copyright (c) Zain Al-Ahmary.  All rights reserved.
// Licensed under the MIT License, (the "License"); you may not use this file except in compliance with the License. 
// You may obtain a copy of the License at https://mit-license.org/

using HDDL.Collections;
using System;
using System.Collections.Generic;
using System.Text;

namespace HDDL.Language
{
    /// <summary>
    /// Base tokenizer class implementation to alleviate copy duplication across tokenizers
    /// </summary>
    /// <typeparam name="TokenType">The type of token produced and stored by the process (e.g the actual tokens)</typeparam>
    abstract class TokenizerBase<TokenType>
    {
        protected int Minimum_Column = 1;
        protected int Minimum_Row = 1;

        /// <summary>
        /// The currently targeted HDSL code
        /// </summary>
        protected ListStack<char> _buffer;

        /// <summary>
        /// The current coordinates in the code
        /// </summary>
        protected int _col, _row;

        /// <summary>
        /// The resulting Tokens from the operation
        /// </summary>
        public ListStack<TokenType> Tokens { get; private set; }

        /// <summary>
        /// Any errors generated during the tokenization process
        /// </summary>
        public List<LogItemBase> Outcome { get; private set; }

        /// <summary>
        /// Create a base tokenizer instance
        /// </summary>
        public TokenizerBase()
        {
            Tokens = new ListStack<TokenType>();
            _buffer = new ListStack<char>();
            Outcome = new List<LogItemBase>();
            _col = Minimum_Column;
            _row = Minimum_Row;
        }

        #region Utility

        /// <summary>
        /// Returns a value indicating whether or not there are more characters beyond the given minimum
        /// </summary>
        /// <param name="min">The minimum number of characters to test for</param>
        /// <returns>True if there are more than min, false otherwise</returns>
        protected bool More(int min = 0)
        {
            return _buffer.Count > min;
        }

        /// <summary>
        /// Returns the given index in the buffer
        /// </summary>
        /// <param name="offset">The index</param>
        /// <returns>The character found at the given location</returns>
        /// <exception cref="IndexOutOfRangeException"/>
        protected char Peek(int offset = 0)
        {
            return _buffer.Peek(offset);
        }

        /// <summary>
        /// Returns the range of characters starting at the given index
        /// </summary>
        /// <param name="offset">The start index</param>
        /// <param name="length">The number of characters</param>
        /// <returns>The characters (as a string) found at the given location</returns>
        /// <exception cref="IndexOutOfRangeException"/>
        protected string PeekStr(int offset = 0, int length = 1)
        {
            StringBuilder sb = new StringBuilder();
            var terminationPoint = offset + (length - 1);
            for (int i = offset; i <= terminationPoint; i++)
            {
                sb.Append(_buffer.Peek(i));
            }

            return sb.ToString();
        }

        /// <summary>
        /// Removes the 0-index character and returns it
        /// </summary>
        /// <returns>The character</returns>
        /// <exception cref="IndexOutOfRangeException"/>
        protected char Pop()
        {
            Step(Peek());
            return _buffer.Pop();
        }

        /// <summary>
        /// Removes length characters, beginning at the offset, and returns them
        /// </summary>
        /// <returns>The characters</returns>
        /// <param name="length">The number of characters to return</param>
        /// <param name="offset">The index to start from</param>
        /// <exception cref="IndexOutOfRangeException"/>
        protected string PopStr(int offset = 0, int length = 1)
        {
            StringBuilder sb = new StringBuilder();
            var terminationPoint = offset + (length - 1); ;
            for (int i = offset; i <= terminationPoint; i++)
            {
                Step(Peek());
                sb.Append(_buffer.Pop());
            }

            return sb.ToString();
        }

        /// <summary>
        /// Removes the given number of characters and trashes them
        /// </summary>
        /// <param name="count">The number of characters to remove</param>
        /// <exception cref="IndexOutOfRangeException"/>
        protected void Eat(int count = 1)
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
        protected void Step(char item)
        {
            if (item == '\n')
            {
                _row++;
            }
            else if (item == '\r')
            {
                _col = Minimum_Column;
            }
            else
            {
                _col++;
            }
        }

        /// <summary>
        /// Retrieves and returns everything between the first occurrance of start and end, taking into account escapes
        /// </summary>
        /// <param name="start">The starting characters</param>
        /// <param name="end">The ending characters</param>
        /// <param name="escape">The character used to escape start and end to allow them inside of the run (escape only works if end is a single character)</param>
        /// <returns>An array containing the literal of the paired set in the first slot and the encoded paired set with escapes in the second</returns>
        protected string[] GetPairedSet(string start, string end, char? escape = null)
        {
            string[] result = null;
            var literal = new StringBuilder();
            var encoded = new StringBuilder();
            if (More(start.Length) && PeekStr(length: start.Length).Equals(start, StringComparison.InvariantCultureIgnoreCase))
            {
                encoded.Append(PopStr(length: start.Length));

                var done = false;
                while (!done)
                {
                    if (More(end.Length) == false)
                    {
                        Outcome.Add(new LogItemBase(_col, _row, string.Format("End of file before paired set closed.  '{0}' expected.", end)));
                        return null;
                    }

                    if (escape.HasValue &&
                        Peek() == escape.Value) // found the escape character
                    {
                        // check to see if the character after the escape is the ending character
                        if (More(end.Length + 1) && PeekStr(1, end.Length) == end)
                        {
                            // yes
                            literal.Append(Peek(1));
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
                    else if (PeekStr(length: end.Length) == end) // this is a non-escaped end.  we're done
                    {
                        done = true;
                        encoded.Append(PopStr(length: end.Length));
                    }
                    else // copy one character and compare again
                    {
                        literal.Append(Peek());
                        encoded.Append(Pop());
                    }
                }

                result = new string[] { literal.ToString(), encoded.ToString() };
            }
            else
            {
                Outcome.Add(new LogItemBase(_col, _row, $"Unexpected character found.  Expected '{start}'."));
            }

            return result;
        }

        /// <summary>
        /// Retrieves and returns everything between the first occurrance of start and end, taking into account escapes
        /// </summary>
        /// <param name="start">The starting character</param>
        /// <param name="end">The ending character</param>
        /// <param name="escape">The character used to escape start and end to allow them inside of the run</param>
        /// <returns>An array containing the literal of the paired set in the first slot and the encoded paired set with escapes in the second</returns>
        protected string[] GetPairedSet(char start, char end, char? escape = null)
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
                        Outcome.Add(new LogItemBase(_col, _row, string.Format("End of file before paired set closed.  '{0}' expected.", end)));
                        return null;
                    }

                    if (escape.HasValue &&
                        Peek() == escape.Value) // found the escape character
                    {
                        // check to see if the character after the escape is the ending character
                        if (More(1) && Peek(1) == end)
                        {
                            // yes
                            literal.Append(Peek(1));
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
                Outcome.Add(new LogItemBase(_col, _row, $"Unexpected character found.  Expected '{start}'."));
                return null;
            }
        }

        #endregion
    }
}
