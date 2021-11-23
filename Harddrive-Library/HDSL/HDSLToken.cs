// Copyright (c) Zain Al-Ahmary.  All rights reserved.
// Licensed under the MIT License, (the "License"); you may not use this file except in compliance with the License. 
// You may obtain a copy of the License at https://mit-license.org/

namespace HDDL.HDSL
{
    /// <summary>
    /// Represents a piece of HDSL code
    /// </summary>
    class HDSLToken
    {
        private HDSLTokenTypes type;
        /// <summary>
        /// The token's type
        /// </summary>
        public HDSLTokenTypes Type 
        { 
            get
            {
                return type;
            }
            private set
            {
                type = value;
                switch (type)
                {
                    case HDSLTokenTypes.BookmarkReference:
                    case HDSLTokenTypes.String:
                    case HDSLTokenTypes.WholeNumber:
                    case HDSLTokenTypes.RealNumber:
                    case HDSLTokenTypes.DateTime:
                        Family = HDSLTokenFamilies.DataTypes;
                        break;
                    case HDSLTokenTypes.Whitespace:
                        Family = HDSLTokenFamilies.Whitespace;
                        break;
                    case HDSLTokenTypes.Colon:
                    case HDSLTokenTypes.Comma:
                    case HDSLTokenTypes.Dot:
                        Family = HDSLTokenFamilies.StructuralOperators;
                        break;
                    case HDSLTokenTypes.AttributeLiteral:
                        Family = HDSLTokenFamilies.AttributeLiterals;
                        break;
                    case HDSLTokenTypes.Alias:
                    case HDSLTokenTypes.Span:
                    case HDSLTokenTypes.FileSystem:
                    case HDSLTokenTypes.ColumnMappings:
                    case HDSLTokenTypes.GroupBy:
                    case HDSLTokenTypes.OrderBy:
                    case HDSLTokenTypes.Columns:
                    case HDSLTokenTypes.HashLogs:
                    case HDSLTokenTypes.Error:
                    case HDSLTokenTypes.Standard:
                    case HDSLTokenTypes.Reset:
                    case HDSLTokenTypes.Set:
                    case HDSLTokenTypes.Out:
                    case HDSLTokenTypes.Force:
                    case HDSLTokenTypes.Passive:
                    case HDSLTokenTypes.Wards:
                    case HDSLTokenTypes.Watches:
                    case HDSLTokenTypes.Ward:
                    case HDSLTokenTypes.Watch:
                    case HDSLTokenTypes.Bookmarks:
                    case HDSLTokenTypes.Exclusions:
                    case HDSLTokenTypes.Dynamic:
                    case HDSLTokenTypes.Exclude:
                    case HDSLTokenTypes.Include:
                    case HDSLTokenTypes.TextMode:
                    case HDSLTokenTypes.SpinnerMode:
                    case HDSLTokenTypes.QuietMode:
                    case HDSLTokenTypes.ProgressMode:
                    case HDSLTokenTypes.In:
                    case HDSLTokenTypes.Under:
                    case HDSLTokenTypes.Within:
                    case HDSLTokenTypes.Scan:
                    case HDSLTokenTypes.Check:
                    case HDSLTokenTypes.Find:
                    case HDSLTokenTypes.Asc:
                    case HDSLTokenTypes.Dsc:
                    case HDSLTokenTypes.Purge:
                    case HDSLTokenTypes.Where:
                        Family = HDSLTokenFamilies.LanguageKeywords;
                        break;
                    case HDSLTokenTypes.ColumnName:
                    case HDSLTokenTypes.Now:
                        Family = HDSLTokenFamilies.ValueKeywords;
                        break;
                    case HDSLTokenTypes.And:
                    case HDSLTokenTypes.Or:
                        Family = HDSLTokenFamilies.LogicalOperators;
                        break;
                    case HDSLTokenTypes.Like:
                    case HDSLTokenTypes.GreaterThan:
                    case HDSLTokenTypes.LessThan:
                    case HDSLTokenTypes.Equal:
                    case HDSLTokenTypes.NotEqual:
                    case HDSLTokenTypes.GreaterOrEqual:
                    case HDSLTokenTypes.LessOrEqual:
                        Family = HDSLTokenFamilies.RelativeOperators;
                        break;
                    case HDSLTokenTypes.Has:
                    case HDSLTokenTypes.HasNot:
                        Family = HDSLTokenFamilies.StateOperators;
                        break;
                    case HDSLTokenTypes.MultiLineComment:
                    case HDSLTokenTypes.Comment:
                        Family = HDSLTokenFamilies.Comment;
                        break;
                    case HDSLTokenTypes.EndOfFile:
                    case HDSLTokenTypes.EndOfLine:
                        Family = HDSLTokenFamilies.Metadata;
                        break;
                    default:
                        Family = HDSLTokenFamilies.Unknown;
                        break;
                }
            }
        }

        /// <summary>
        /// The token's type family
        /// </summary>
        public HDSLTokenFamilies Family { get; private set; }

        /// <summary>
        /// The text represented
        /// </summary>
        public string Code { get; private set; }

        /// <summary>
        /// The row where this token begins
        /// </summary>
        public int Row { get; private set; }

        /// <summary>
        /// The column where this token begins
        /// </summary>
        public int Column { get; private set; }

        /// <summary>
        /// The text's rendered equivalent
        /// </summary>
        public string Literal { get; private set; }

        /// <summary>
        /// Creates a HDSLToken with the given Type and Code
        /// </summary>
        /// <param name="type">The type of code represented</param>
        /// <param name="code">The code</param>
        /// <param name="row">The starting row</param>
        /// <param name="column">The starting column</param>
        /// <param name="literal">The text's rendered equivalent</param>
        public HDSLToken(HDSLTokenTypes type, string code, int row, int column, string literal)
        {
            Type = type;
            Code = code;
            Row = row;
            Column = column - Code.Length;
            Literal = literal;
        }

        /// <summary>
        /// Creates a HDSLToken with the given Type and Code
        /// </summary>
        /// <param name="type">The type of code represented</param>
        /// <param name="code">The code</param>
        /// <param name="row">The starting row</param>
        /// <param name="column">The starting column</param>
        /// <param name="literal">The text's rendered equivalent</param>
        public HDSLToken(HDSLTokenTypes type, char code, int row, int column, string literal)
        {
            Type = type;
            Code = code.ToString();
            Row = row;
            Column = column - Code.Length;
            Literal = literal;
        }

        public override string ToString()
        {
            return string.Format("[{0} : '{1}' @ {2}, {3}]", Type, Code, Column, Row);
        }
    }
}
