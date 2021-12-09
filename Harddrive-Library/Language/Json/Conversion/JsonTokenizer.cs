// Copyright (c) Zain Al-Ahmary.  All rights reserved.
// Licensed under the MIT License, (the "License"); you may not use this file except in compliance with the License. 
// You may obtain a copy of the License at https://mit-license.org/

using HDDL.Collections;
using HDDL.Language;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        /// The json to tokenizer
        /// </summary>
        /// <param name="json"></param>
        /// <returns></returns>
        public string[] Tokenizer(string json)
        {
            return null;
        }

        #region Utility



        #endregion
    }
}
