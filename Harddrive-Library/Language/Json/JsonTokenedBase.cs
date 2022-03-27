// Copyright (c) Zain Al-Ahmary.  All rights reserved.
// Licensed under the MIT License, (the "License"); you may not use this file except in compliance with the License. 
// You may obtain a copy of the License at https://mit-license.org/

using HDDL.Language.Json.Conversion;

namespace HDDL.Language.Json
{
    /// <summary>
    /// Basic base implementation of JsonToken-based manipulator
    /// </summary>
    abstract class JsonTokenedBase
    {
        protected JsonTokenizer _jt;

        public JsonTokenedBase()
        {
            _jt = new JsonTokenizer();
        }

        /// <summary>
        /// Tokenizes the provided json string
        /// </summary>
        /// <param name="json">The string to tokenize</param>
        /// <returns>Any issues encountered along the way</returns>
        protected LogItemBase[] Process(string json)
        {
            return _jt.Tokenize(json, false);
        }

        /// <summary>
		/// Look to see what is at the given index
		/// </summary>
		/// <param name="index">The index to look at</param>
		/// <returns>The item at the given index</returns>
		protected JsonToken Peek(int index = 0)
        {
            return _jt.Tokens.Peek(index);
        }

        /// <summary>
		/// Pops the next token
		/// </summary>
		/// <returns>The item at the given index</returns>
		protected JsonToken Pop()
        {
            return _jt.Tokens.Pop();
        }

        /// <summary>
        /// Checks to see if there are more than <paramref name="count"/> tokens remaining
        /// </summary>
        /// <param name="count"></param>
        /// <returns></returns>
        protected bool More(int count = 0)
        {
            return _jt.Tokens.Count > count;
        }
    }
}
