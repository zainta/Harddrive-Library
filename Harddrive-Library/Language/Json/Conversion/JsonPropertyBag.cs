// Copyright (c) Zain Al-Ahmary.  All rights reserved.
// Licensed under the MIT License, (the "License"); you may not use this file except in compliance with the License. 
// You may obtain a copy of the License at https://mit-license.org/

namespace HDDL.Language.Json.Conversion
{
    /// <summary>
    /// Encapsulates a property name with its content
    /// </summary>
    class JsonPropertyBag
    {
        /// <summary>
        /// The property's name
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// The property's content
        /// </summary>
        public JsonBase Content { get; private set; }

        /// <summary>
        /// The column where this property was declared
        /// </summary>
        public int Column { get; private set; }

        /// <summary>
        /// The row where this property was declared
        /// </summary>
        public int Row { get; private set; }

        /// <summary>
        /// Creates a JsonPropertyBag to contain the given content
        /// </summary>
        /// <param name="token">The token containing location and name information</param>
        /// <param name="content">The property's content</param>
        public JsonPropertyBag(JsonToken token, object content)
        {
            Column = token.Column;
            Row = token.Row;
            Name = token.Literal;

            if (content is JsonBase)
            {
                Content = (JsonBase)content;
            }
            else
            {
                Content = new ValueTypeQuantity(content);
            }
        }
    }
}
