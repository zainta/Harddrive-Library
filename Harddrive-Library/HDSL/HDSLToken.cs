using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
                    case HDSLTokenTypes.In:
                    case HDSLTokenTypes.Find:
                    case HDSLTokenTypes.Asc:
                    case HDSLTokenTypes.Dsc:
                    case HDSLTokenTypes.Purge:
                    case HDSLTokenTypes.Within:
                        Family = HDSLTokenFamilies.LanguageKeywords;
                        break;
                    case HDSLTokenTypes.Written:
                    case HDSLTokenTypes.Accessed:
                    case HDSLTokenTypes.Created:
                    case HDSLTokenTypes.Extension:
                    case HDSLTokenTypes.LastScan:
                    case HDSLTokenTypes.FirstScan:
                    case HDSLTokenTypes.Name:
                    case HDSLTokenTypes.Now:
                    case HDSLTokenTypes.Size:
                        Family = HDSLTokenFamilies.ValueKeywords;
                        break;
                    case HDSLTokenTypes.Comma:
                    case HDSLTokenTypes.And:
                    case HDSLTokenTypes.Or:
                        Family = HDSLTokenFamilies.LogicalOperators;
                        break;
                    case HDSLTokenTypes.GreaterThan:
                    case HDSLTokenTypes.LessThan:
                    case HDSLTokenTypes.Equal:
                    case HDSLTokenTypes.NotEqual:
                    case HDSLTokenTypes.GreaterOrEqual:
                    case HDSLTokenTypes.LessOrEqual:
                        Family = HDSLTokenFamilies.RelativeOperators;
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
