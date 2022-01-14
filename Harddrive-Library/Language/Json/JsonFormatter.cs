// Copyright (c) Zain Al-Ahmary.  All rights reserved.
// Licensed under the MIT License, (the "License"); you may not use this file except in compliance with the License. 
// You may obtain a copy of the License at https://mit-license.org/

using HDDL.Language.Json.Conversion;
using System;
using System.Linq;
using System.Text;

namespace HDDL.Language.Json
{
    /// <summary>
    /// A static class that formats the provided json
    /// </summary>
    class JsonFormatter : JsonTokenedBase
    {
        /// <summary>
        /// Formats the json string for easy viewing
        /// </summary>
        /// <param name="json">The json string to format</param>
        /// <param name="indentation">The character(s) to use for indentation</param>
        /// <returns>The formatted string</returns>
        public static string Format(string json, string indentation = "  ")
        {
            JsonFormatter js = new JsonFormatter();
            return js.FormatJson(json, indentation);
        }

        private int _indentLevel;
        private string _indentation;
        private bool _newLine;

        public JsonFormatter() : base()
        {

        }

        /// <summary>
        /// Formats the json string for easy viewing
        /// </summary>
        /// <param name="json">The json string to format</param>
        /// <param name="indentation">The character(s) to use for indentation</param>
        /// <returns>The formatted string</returns>
        public string FormatJson(string json, string indentation = "  ")
        {
            _indentation = indentation;
            _indentLevel = -1;
            StringBuilder result = new StringBuilder();
            Process(json);

            if (_jt.Outcome.Count > 0)
            {
                throw new ArgumentException("Bad json string.");
            }
            else
            {
                _newLine = false;
                JsonToken previous = null;
                while (More())
                {
                    if (Peek().Type == JsonTokenTypes.EndOfJSON)
                    {
                        // we're formatting the string, so ignore all whitespace
                        Pop();
                    }
                    else
                    {
                        // All newline logic happens pre
                        DoPre(previous);

                        result.Append(GetPreface(previous));
                        result.Append(Peek().Code);

                        // All indentation logic happens post
                        DoPost(previous);

                        // update the previous token
                        previous = Pop();
                    }
                }
            }

            return result.ToString();
        }

        /// <summary>
        /// Handles newline assignments
        /// </summary>
        /// <param name="previous">The previous token</param>
        private void DoPre(JsonToken previous)
        {
            if (previous != null)
            {
                if ((Peek().Family == JsonTokenFamilies.StructuralOpening ||
                    Peek().Family == JsonTokenFamilies.StructuralClosing) &&
                    previous.Family != JsonTokenFamilies.ValueStructural)
                {
                    NewLine();
                }
                else if (previous.Family == JsonTokenFamilies.Comma)
                {
                    NewLine();
                }
                else if (
                    (previous.Family == JsonTokenFamilies.StructuralOpening ||
                    previous.Family == JsonTokenFamilies.StructuralClosing) &&
                    Peek().Family == JsonTokenFamilies.Value)
                {
                    NewLine();
                }
            }
        }

        /// <summary>
        /// Handles indentation changes
        /// </summary>
        /// <param name="previous">The previous token</param>
        private void DoPost(JsonToken previous)
        {
            if (Peek().Family == JsonTokenFamilies.StructuralOpening &&
                Peek(1).Family != JsonTokenFamilies.Comma &&
                Peek(1).Family != JsonTokenFamilies.StructuralClosing)
            {
                IndentBy(1);
            }
            if (More(1) &&
                Peek().Family == JsonTokenFamilies.StructuralClosing &&
                Peek(1).Family != JsonTokenFamilies.Comma &&
                Peek(1).Family != JsonTokenFamilies.Value)
            {
                IndentBy(-1);
            }
            else if (More(1) &&
                Peek().Family == JsonTokenFamilies.Value &&
                Peek(1).Family == JsonTokenFamilies.StructuralClosing)
            {
                IndentBy(-1);
            }
            else if (previous == null)
            {
                IndentBy(1);
            }
        }

        /// <summary>
        /// Modifies the indentation value by the given offset
        /// </summary>
        /// <param name="offset">A positive or negative number to add to the indent level</param>
        private void IndentBy(int offset = 1)
        {
            _indentLevel += offset;
        }

        /// <summary>
        /// Returns the text that precedes the token's text
        /// </summary>
        /// <param name="previous">The previous token</param>
        /// <returns></returns>
        private string GetPreface(JsonToken previous)
        {
            StringBuilder sb = new StringBuilder();
            if (_newLine)
            {
                if (previous != null)
                {
                    sb.Append("\n");
                    _newLine = false;
                }

                if (_indentLevel > 0)
                {
                    sb.Append(string.Concat(Enumerable.Repeat(_indentation, _indentLevel)));
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// Sets the next token to be placed on a new line
        /// </summary>
        private void NewLine()
        {
            _newLine = true;
        }
    }
}
