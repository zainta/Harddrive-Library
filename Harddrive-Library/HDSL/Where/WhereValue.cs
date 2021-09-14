using HDDL.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HDDL.HDSL.Where
{
    /// <summary>
    /// Represents a value in a where clause of any valid type
    /// </summary>
    class WhereValue
    {
        /// <summary>
        /// The value used for the "now" keyword.
        /// </summary>
        public static DateTime Now = DateTime.Now;

        /// <summary>
        /// Whether or not this WhereValue represents a value from the database (a ValueKeyword)
        /// </summary>
        public bool IsSlug { get; set; }

        /// <summary>
        /// The Value Keyword
        /// </summary>
        public HDSLTokenTypes? Keyword { get; set; }

        /// <summary>
        /// The value's actual type
        /// </summary>
        public WhereValueTypes ValueType { get; private set; }

        /// <summary>
        /// Stores the actual type of the value
        /// </summary>
        private object _actual;

        /// <summary>
        /// Creates a value from the token
        /// </summary>
        /// <param name="token">The token to convert into the value</param>
        public WhereValue(HDSLToken token)
        {
            IsSlug = false;
            Keyword = null;
            if (token.Family == HDSLTokenFamilies.DataTypes)
            {
                switch (token.Type)
                {
                    case HDSLTokenTypes.BookmarkReference:
                        ValueType = WhereValueTypes.BookMarkReference;
                        _actual = token.Literal;
                        break;
                    case HDSLTokenTypes.String:
                        ValueType = WhereValueTypes.String;
                        _actual = token.Literal;
                        break;
                    case HDSLTokenTypes.WholeNumber:
                        ValueType = WhereValueTypes.WholeNumber;
                        _actual = long.Parse(token.Literal);
                        break;
                    case HDSLTokenTypes.RealNumber:
                        ValueType = WhereValueTypes.RealNumber;
                        _actual = double.Parse(token.Literal);
                        break;
                    case HDSLTokenTypes.DateTime:
                        ValueType = WhereValueTypes.DateTime;
                        _actual = DateTime.Parse(token.Literal);
                        break;
                }
            }
            else if (token.Family == HDSLTokenFamilies.ValueKeywords)
            {
                IsSlug = true;
                Keyword = token.Type;
                switch (token.Type)
                {
                    case HDSLTokenTypes.Size:
                        ValueType = WhereValueTypes.WholeNumber;
                        break;
                    case HDSLTokenTypes.Written:
                        ValueType = WhereValueTypes.DateTime;
                        break;
                    case HDSLTokenTypes.Accessed:
                        ValueType = WhereValueTypes.DateTime;
                        break;
                    case HDSLTokenTypes.Created:
                        ValueType = WhereValueTypes.DateTime;
                        break;
                    case HDSLTokenTypes.Extension:
                        ValueType = WhereValueTypes.String;
                        break;
                    case HDSLTokenTypes.LastScan:
                        ValueType = WhereValueTypes.DateTime;
                        break;
                    case HDSLTokenTypes.FirstScan:
                        ValueType = WhereValueTypes.DateTime;
                        break;
                    case HDSLTokenTypes.Name:
                        ValueType = WhereValueTypes.String;
                        break;
                    case HDSLTokenTypes.Now:
                        ValueType = WhereValueTypes.DateTime;
                        break;
                }
            }
            else
            {
                throw new ArgumentException();
            }
        }

        /// <summary>
        /// Returns the value
        /// </summary>
        /// <typeparam name="T">The type of the value</typeparam>
        /// <param name="item"></param>
        /// <returns></returns>
        public T Get<T>(DiskItem item)
        {
            object result = null;

            if (IsSlug)
            {
                switch (Keyword)
                {
                    case HDSLTokenTypes.Size:
                        result = item.SizeInBytes == null ? -1 : item.SizeInBytes;
                        break;
                    case HDSLTokenTypes.Written:
                        result = item.LastWritten;
                        break;
                    case HDSLTokenTypes.Accessed:
                        result = item.LastAccessed;
                        break;
                    case HDSLTokenTypes.Created:
                        result = item.CreationDate;
                        break;
                    case HDSLTokenTypes.Extension:
                        result = item.Extension;
                        break;
                    case HDSLTokenTypes.LastScan:
                        result = item.LastScanned;
                        break;
                    case HDSLTokenTypes.FirstScan:
                        result = item.FirstScanned;
                        break;
                    case HDSLTokenTypes.Name:
                        result = item.ItemName;
                        break;
                    case HDSLTokenTypes.Now:
                        result = Now;
                        break;
                }
            }
            else
            {
                result = _actual;
            }

            return (T)result;
        }

        public override string ToString()
        {
            if (IsSlug)
            {
                return Keyword.ToString();
            }
            else
            {
                return _actual.ToString();
            }

        }
    }
}
