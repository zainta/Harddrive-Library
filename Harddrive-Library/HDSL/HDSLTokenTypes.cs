// Copyright (c) Zain Al-Ahmary.  All rights reserved.
// Licensed under the MIT License, (the "License"); you may not use this file except in compliance with the License. 
// You may obtain a copy of the License at https://mit-license.org/

namespace HDDL.HDSL
{
    /// <summary>
    /// Defines the types of tokens used by Hard Drive Search Language (HDSL)
    /// </summary>
    enum HDSLTokenTypes
    {
        // Data Types

        BookmarkReference, // [...]
        String, // ' (also stores paths)
        WholeNumber, // a number without a decimal point
        RealNumber, // a number with a decimal point
        DateTime, // #

        // Special

        Whitespace,

        // Keywords

        Now,
        In,
        Find,
        Asc,
        Dsc,
        Purge,
        Within,
        Where,
        Sort,
        By,
        Under,
        Scan,
        Check,
        SpinnerMode,
        ProgressMode,
        TextMode,
        QuietMode,
        Exclude,
        Include,
        Dynamic,
        Bookmarks,
        Exclusions,
        Ward, // describes a periodic integrity check
        Watch, // describes an initial scan + follow up monitoring
        Reset, // used to force a watch to be reenacted as if freshly created
        Wards,
        Watches,

        // value keywords
        Size,
        Written,
        Accessed,
        Created,
        Extension,
        LastScan,
        FirstScan,
        Name,

        // attribute keywords
        AttributeLiteral,

        // Positivity and Negativity Operators
        Has,
        HasNot,

        // Structural Operators
        Dot,
        Comma,
        Colon,

        // Relational Operators

        GreaterThan,
        LessThan,
        Equal,
        NotEqual,
        GreaterOrEqual,
        LessOrEqual,

        // Logical Operators

        And,
        Or,

        // Metadata
        EndOfFile,
        EndOfLine,

        Comment, // comments start with --
        MultiLineComment
    }
}
