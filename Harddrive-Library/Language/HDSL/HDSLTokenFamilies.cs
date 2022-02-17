// Copyright (c) Zain Al-Ahmary.  All rights reserved.
// Licensed under the MIT License, (the "License"); you may not use this file except in compliance with the License. 
// You may obtain a copy of the License at https://mit-license.org/

namespace HDDL.Language.HDSL
{
    /// <summary>
    /// Defines the general families all token types belong to
    /// </summary>
    public enum HDSLTokenFamilies
    {
        Unknown,
        DataTypes,
        Whitespace,
        LanguageKeywords,
        ValueKeywords,
        AttributeLiterals,
        RelativeOperators, // Greater Than, Less Than, Equal, Not Equal
        LogicalOperators, // And, Or
        StateOperators, // + and -
        Metadata,
        StructuralOperators,
        Comment
    }
}
