// Copyright (c) Zain Al-Ahmary.  All rights reserved.
// Licensed under the MIT License, (the "License"); you may not use this file except in compliance with the License. 
// You may obtain a copy of the License at https://mit-license.org/

using System;

namespace HDDL.Language.Json.Conversion
{
    /// <summary>
    /// Represents a JSON string token
    /// </summary>
    class JsonToken
    {
        /// <summary>
        /// The token text as it appears in the JSOn
        /// </summary>
        public string Code { get; set; }

        /// <summary>
        /// The token's meaning (quote-less string value, etc)
        /// </summary>
        public string Literal { get; set; }

        private JsonTokenTypes _type;
        /// <summary>
        /// The type of token
        /// </summary>
        public JsonTokenTypes Type 
        { 
            get
            {
                return _type;
            }
            set
            {
                _type = value;
                switch (_type)
                {
                    case JsonTokenTypes.Boolean:
                    case JsonTokenTypes.RealNumber:
                    case JsonTokenTypes.String:
                    case JsonTokenTypes.WholeNumber:
                    case JsonTokenTypes.Null:
                        Family = JsonTokenFamilies.Value;
                        break;
                    case JsonTokenTypes.Comma:
                        Family = JsonTokenFamilies.Comma;
                        break;
                    case JsonTokenTypes.Colon:
                        Family = JsonTokenFamilies.ValueStructural;
                        break;
                    case JsonTokenTypes.CurlyOpen:
                    case JsonTokenTypes.SquareOpen:
                        Family = JsonTokenFamilies.StructuralOpening;
                        break;
                    case JsonTokenTypes.CurlyClose:
                    case JsonTokenTypes.SquareClose:
                        Family = JsonTokenFamilies.StructuralClosing;
                        break;
                    case JsonTokenTypes.TypeAnnotation:
                    case JsonTokenTypes.EndOfJSON:
                        Family = JsonTokenFamilies.Metadata;
                        break;
                    default:
                        throw new Exception("Unknown token family.");
                }
            }
        }

        /// <summary>
        /// The token type's family
        /// </summary>
        public JsonTokenFamilies Family { get; set; }

        /// <summary>
        /// The textual column where the token originated
        /// </summary>
        public int Column { get; set; }

        /// <summary>
        /// The textual row where the token originated
        /// </summary>
        public int Row { get; set; }

        /// <summary>
        /// Create a JSON token
        /// </summary>
        /// <param name="type"></param>
        /// <param name="code"></param>
        /// <param name="literal"></param>
        /// <param name="column"></param>
        /// <param name="row"></param>
        public JsonToken(JsonTokenTypes type, string code, string literal, int column, int row)
        {
            Code = code;
            Literal = literal;
            Type = type;
            Column = column;
            Row = row;
        }

        /// <summary>
        /// Create a JSON token
        /// </summary>
        /// <param name="type"></param>
        /// <param name="code"></param>
        /// <param name="literal"></param>
        /// <param name="column"></param>
        /// <param name="row"></param>
        public JsonToken(JsonTokenTypes type, char code, char literal, int column, int row)
        {
            Code = code.ToString();
            Literal = literal.ToString();
            Type = type;
            Column = column;
            Row = row;
        }

        public override string ToString()
        {
            return $"['{Code}', '{Literal}', {Type} - ({Column}, {Row})]";
        }
    }
}
