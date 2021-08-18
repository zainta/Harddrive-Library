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

        // Keywords

        Now,
        In,
        Find,
        Asc,
        Dsc,
        Purge,
        Within,
        Where,
        Size,
        All,

        // Relational Operators

        GreaterThan,
        LessThan,
        Equal,
        NotEqual,
        GreaterOrEqual,
        LessThanOrEqual,

        // Metadata
        EndOfFile,
        EndOfLine
    }
}
