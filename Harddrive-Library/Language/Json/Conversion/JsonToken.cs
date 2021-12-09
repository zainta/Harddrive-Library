// Copyright (c) Zain Al-Ahmary.  All rights reserved.
// Licensed under the MIT License, (the "License"); you may not use this file except in compliance with the License. 
// You may obtain a copy of the License at https://mit-license.org/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        /// <summary>
        /// The type of token
        /// </summary>
        public JsonTokenTypes Type { get; set; }

        /// <summary>
        /// Create a JSON token
        /// </summary>
        /// <param name="type"></param>
        /// <param name="code"></param>
        /// <param name="literal"></param>
        public JsonToken(JsonTokenTypes type, string code, string literal)
        {
            Code = code;
            Literal = literal;
            Type = type;
        }
    }
}
