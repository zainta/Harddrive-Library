// Copyright (c) Zain Al-Ahmary.  All rights reserved.
// Licensed under the MIT License, (the "License"); you may not use this file except in compliance with the License. 
// You may obtain a copy of the License at https://mit-license.org/

using System;
using System.Text;

namespace HDDL.Language.Json.Conversion
{
    /// <summary>
    /// Breaks a JSON string down into its component tokens
    /// </summary>
    class JsonTokenizer : TokenizerBase<JsonToken>
    {
        /// <summary>
        /// Create a JsonTokenizer
        /// </summary>
        public JsonTokenizer() : base()
        {
        }

        /// <summary>
        /// The tokenize the json
        /// </summary>
        /// <param name="json">The json string to tokenize</param>
        /// <returns></returns>
        public LogItemBase[] Tokenize(string json)
        {
            Tokens.Clear();
            Outcome.Clear();
            _buffer.Clear();
            _buffer.AddRange(json);
            _col = Minimum_Column;
            _row = Minimum_Row;

            // Loop through the code and pick out the tokens one by one, in order of discovery
            while (!_buffer.Empty)
            {
                if (GetJsonFraming())
                {
                    continue;
                }
                else if (GetString())
                {
                    continue;
                }
                else if (GetNumbers())
                {
                    continue;
                }
                else if (GetBoolean())
                {
                    continue;
                }
                else
                {
                    Outcome.Add(new LogItemBase(_col, _row, $"Unknown character '{Peek()}'."));
                }
            }

            Tokens.Add(new JsonToken(JsonTokenTypes.EndOfJSON, ' ', ' ', _col, _row));

            return Outcome.ToArray();
        }

        /// <summary>
        /// Retrieves all Json framing (characters that describe the structure of the document and now what's in it)
        /// </summary>
        /// <returns></returns>
        private bool GetJsonFraming()
        {
            if (Peek() == '{')
            {
                Tokens.Add(new JsonToken(JsonTokenTypes.CurlyOpen, Peek(), Pop(), _col, _row));
            }
            else if (Peek() == '}')
            {
                Tokens.Add(new JsonToken(JsonTokenTypes.CurlyClose, Peek(), Pop(), _col, _row));
            }
            else if (Peek() == '[')
            {
                Tokens.Add(new JsonToken(JsonTokenTypes.SquareOpen, Peek(), Pop(), _col, _row));
            }
            else if (Peek() == ']')
            {
                Tokens.Add(new JsonToken(JsonTokenTypes.SquareClose, Peek(), Pop(), _col, _row));
            }
            else if (Peek() == ':')
            {
                Tokens.Add(new JsonToken(JsonTokenTypes.Colon, Peek(), Pop(), _col, _row));
            }
            else if (Peek() == ',')
            {
                Tokens.Add(new JsonToken(JsonTokenTypes.Comma, Peek(), Pop(), _col, _row));
            }
            else if (char.IsWhiteSpace(Peek()))
            {
                StringBuilder sb = new StringBuilder();
                while (char.IsWhiteSpace(Peek()))
                {
                    sb.Append(Pop());
                }

                // We don't care about whitespace.
                // This is JSON and we aren't expected to reproduce the exact formatting.
                // just trash it.
            }
            else
            {
                return false;
            }

            return true;
        }
        
        /// <summary>
        /// Gathers a string token and add it to the list
        /// </summary>
        /// <returns>Whether or not a token was generated (if not, implies an error)</returns>
        bool GetString()
        {
            string[] str;
            switch (Peek())
            {
                case '\'':
                    str = GetPairedSet('\'', '\'', null);
                    if (str != null)
                    {
                        Tokens.Add(new JsonToken(JsonTokenTypes.String, str[1], str[0], _row, _col));
                        return true;
                    }
                    break;
                case '"':
                    str = GetPairedSet('"', '"', null);
                    if (str != null)
                    {
                        Tokens.Add(new JsonToken(JsonTokenTypes.String, str[1], str[0], _row, _col));
                        return true;
                    }
                    break;
            }
            return false;
        }

        /// <summary>
        /// Gathers a boolean token and add it to the list
        /// </summary>
        /// <returns>Whether or not a token was generated (if not, implies an error)</returns>
        bool GetBoolean()
        {
            if (PeekStr(0, true.ToString().Length).Equals("true", StringComparison.InvariantCultureIgnoreCase))
            {
                var origCol = _col;
                var origRow = _row;
                var val = PopStr(length: true.ToString().Length);
                Tokens.Add(new JsonToken(JsonTokenTypes.Boolean, val, val, origCol, origRow));
                return true;
            }
            else if (PeekStr(0, false.ToString().Length).Equals("false", StringComparison.InvariantCultureIgnoreCase))
            {
                var origCol = _col;
                var origRow = _row;
                var val = PopStr(length: false.ToString().Length);
                Tokens.Add(new JsonToken(JsonTokenTypes.Boolean, val, val, origCol, origRow));
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

            if (number.Length > 0)
            {
                Tokens.Add(new JsonToken(decimaled ? JsonTokenTypes.RealNumber : JsonTokenTypes.WholeNumber, number.ToString(), number.ToString(), _row, _col));
                return true;
            }
            return false;
        }
    }
}
