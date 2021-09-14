using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HDDL.HDSL
{
    /// <summary>
    /// Defines the general families all token types belong to
    /// </summary>
    enum HDSLTokenFamilies
    {
        Unknown,
        DataTypes,
        Whitespace,
        LanguageKeywords,
        ValueKeywords,
        RelativeOperators, // Greater Than, Less Than, Equal, Not Equal
        LogicalOperators, // And, Or
        Metadata
    }
}
