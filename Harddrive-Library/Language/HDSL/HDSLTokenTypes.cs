// Copyright (c) Zain Al-Ahmary.  All rights reserved.
// Licensed under the MIT License, (the "License"); you may not use this file except in compliance with the License. 
// You may obtain a copy of the License at https://mit-license.org/

namespace HDDL.Language.HDSL
{
    /// <summary>
    /// Defines the types of tokens used by Hard Drive Search Language (HDSL)
    /// </summary>
    public enum HDSLTokenTypes
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

        In,
        Find,
        Asc,
        Desc,
        Purge,
        Within,
        Where,
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
        Wards,
        Watches,
        Passive,
        Force,
        Reset,
        Set,
        Out,
        Error,
        Standard,
        HashLogs,
        GroupBy,
        OrderBy,
        ColumnMappings,
        FileSystem,
        Alias,
        Span,
        Default,
        PageIndex,

        // value keywords
        Now,
        ColumnName,

        // column headers
        Columns,

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
        Like,

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
