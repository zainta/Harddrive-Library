// Copyright (c) Zain Al-Ahmary.  All rights reserved.
// Licensed under the MIT License, (the "License"); you may not use this file except in compliance with the License. 
// You may obtain a copy of the License at https://mit-license.org/

namespace HDDL.Language.Json.Conversion
{
    /// <summary>
    /// Defines the general types of tokens
    /// </summary>
    enum JsonTokenFamilies
    {
        Comma, // the comma is special
        StructuralOpening, // all bracket types
        StructuralClosing, // all bracket types
        Value, // all datatypes, all property names, etc
        ValueStructural, // colons, etc
        Metadata
    }
}
