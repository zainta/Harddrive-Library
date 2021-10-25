// Copyright (c) Zain Al-Ahmary.  All rights reserved.
// Licensed under the MIT License, (the "License"); you may not use this file except in compliance with the License. 
// You may obtain a copy of the License at https://mit-license.org/

using HDDL.Data;
using System;
using System.IO;

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
            else if (token.Family == HDSLTokenFamilies.AttributeLiterals)
            {
                IsSlug = true;
                Keyword = token.Type;
                ValueType = WhereValueTypes.AttributeLiteral;
                _actual = Enum.Parse<FileAttributes>(token.Literal);
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
                        result = item.SizeInBytes;
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
                    case HDSLTokenTypes.AttributeLiteral:
                        result = Convert.ToInt32((FileAttributes)_actual);
                        break;
                }
            }
            else
            {
                result = _actual;
            }

            return (T)result;
        }

        public string ToSQL()
        {
            var result = string.Empty;
            if (IsSlug)
            {
                switch (Keyword)
                {
                    case HDSLTokenTypes.Size:
                        result = "size";
                        break;
                    case HDSLTokenTypes.Written:
                        result = "lastWritten";
                        break;
                    case HDSLTokenTypes.Accessed:
                        result = "lastAccessed";
                        break;
                    case HDSLTokenTypes.Created:
                        result = "created";
                        break;
                    case HDSLTokenTypes.Extension:
                        result = "extension";
                        break;
                    case HDSLTokenTypes.LastScan:
                        result = "lastScanned";
                        break;
                    case HDSLTokenTypes.FirstScan:
                        result = "firstScanned";
                        break;
                    case HDSLTokenTypes.Name:
                        result = "itemName";
                        break;
                    case HDSLTokenTypes.Now:
                        result = DateTimeDataHelper.ConvertToString(Now);
                        break;
                    case HDSLTokenTypes.AttributeLiteral:
                        result = _actual.ToString();
                        break;
                }
            }
            else if (ValueType == WhereValueTypes.AttributeLiteral)
            {
                result = _actual.ToString();
            }
            else
            {
                switch (ValueType)
                {
                    case WhereValueTypes.DateTime:
                        result = $"'{DateTimeDataHelper.ConvertToString((DateTime)_actual)}'";
                        break;
                    case WhereValueTypes.String:
                        result = $"'{_actual}'";
                        break;
                    case WhereValueTypes.RealNumber:
                    case WhereValueTypes.WholeNumber:
                        result = _actual.ToString();
                        break;

                }
            }

            return result;
        }

        public override string ToString()
        {
            var result = string.Empty;
            if (IsSlug)
            {
                switch (Keyword)
                {
                    case HDSLTokenTypes.Size:
                        result = "size";
                        break;
                    case HDSLTokenTypes.Written:
                        result = "written";
                        break;
                    case HDSLTokenTypes.Accessed:
                        result = "accessed";
                        break;
                    case HDSLTokenTypes.Created:
                        result = "created";
                        break;
                    case HDSLTokenTypes.Extension:
                        result = "extension";
                        break;
                    case HDSLTokenTypes.LastScan:
                        result = "last";
                        break;
                    case HDSLTokenTypes.FirstScan:
                        result = "first";
                        break;
                    case HDSLTokenTypes.Name:
                        result = "name";
                        break;
                    case HDSLTokenTypes.Now:
                        result = DateTimeDataHelper.ConvertToString(Now);
                        break;
                    case HDSLTokenTypes.AttributeLiteral:
                        result = _actual.ToString();
                        break;
                }
            }
            else if (ValueType == WhereValueTypes.AttributeLiteral)
            {
                result = _actual.ToString();
            }
            else
            {
                switch (ValueType)
                {
                    case WhereValueTypes.DateTime:
                        result = $"#{DateTimeDataHelper.ConvertToString((DateTime)_actual)}#";
                        break;
                    case WhereValueTypes.String:
                        result = $"'{_actual}'";
                        break;
                    case WhereValueTypes.RealNumber:
                    case WhereValueTypes.WholeNumber:
                        result = _actual.ToString();
                        break;

                }
            }

            return result;
        }
    }
}
