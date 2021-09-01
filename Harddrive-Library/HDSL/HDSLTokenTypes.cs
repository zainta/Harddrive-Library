using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HDDL.HDSL
{
    /// <summary>
    /// Defines the types of tokens used by Hard Drive Search Language (HDSL)
    /// </summary>
    enum HDSLTokenTypes
    {
        // Data Types

        BookmarkReference, // <...>
        String, // ' (also stores paths)
        WholeNumber, // a number without a decimal point
        RealNumber, // a number with a decimal point
        DateTime, // #

        // Special

        Whitespace,
        Comma,

        // Keywords

        Now,
        In,
        Find,
        Asc,
        Dsc,
        Purge,
        Within,
        Where,

        // * attribute keywords
        Size,
        Written,
        Accessed,
        Created,
        Extension,
        LastScan,
        FirstScan,
        Name,

        // Relational Operators

        GreaterThan,
        LessThan,
        Equal,
        NotEqual,
        GreaterOrEqual,
        LessThanOrEqual,

        // Logical Operators

        And,
        Or,

        // Metadata
        EndOfFile,
        EndOfLine
    }
}
