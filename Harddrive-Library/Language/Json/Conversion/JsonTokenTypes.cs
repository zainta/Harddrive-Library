// Copyright (c) Zain Al-Ahmary.  All rights reserved.
// Licensed under the MIT License, (the "License"); you may not use this file except in compliance with the License. 
// You may obtain a copy of the License at https://mit-license.org/

namespace HDDL.Language.Json.Conversion
{
    /// <summary>
    /// Defines the types of tokens found in a json string
    /// </summary>
    enum JsonTokenTypes
    {
        /// <summary>
        /// object start
        /// </summary>
        CurlyOpen,

        /// <summary>
        /// object end
        /// </summary>
        CurlyClose,

        /// <summary>
        /// array start
        /// </summary>
        SquareOpen, 

        /// <summary>
        /// array end
        /// </summary>
        SquareClose,

        /// <summary>
        /// series seperator
        /// </summary>
        Comma,

        /// <summary>
        /// string datatype
        /// </summary>
        String,

        /// <summary>
        /// value indicator
        /// </summary>
        Colon,

        /// <summary>
        /// numbers without decimal points
        /// </summary>
        WholeNumber,

        /// <summary>
        /// boolean true or false
        /// </summary>
        Boolean,

        /// <summary>
        /// numbers with decimal points
        /// </summary>
        RealNumber,

        /// <summary>
        /// Represents a null value
        /// </summary>
        Null,

        /// <summary>
        /// Represents the end of the json
        /// </summary>
        EndOfJSON,

        /// <summary>
        /// Stores the containing object's actual type
        /// </summary>
        TypeAnnotation
    }
}
