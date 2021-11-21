// Copyright (c) Zain Al-Ahmary.  All rights reserved.
// Licensed under the MIT License, (the "License"); you may not use this file except in compliance with the License. 
// You may obtain a copy of the License at https://mit-license.org/

using HDDL.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Linq;
using HDDL.HDSL.Where.Exceptions;

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
        /// The token row where the error occurred
        /// </summary>
        public int Row { get; set; }

        /// <summary>
        /// The token column where the error occurred
        /// </summary>
        public int Column { get; set; }

        /// <summary>
        /// Stores the actual type of the value
        /// </summary>
        private object _actual;

        /// <summary>
        /// The clause's execution context
        /// </summary>
        private ClauseContext _cc;

        /// <summary>
        /// Creates a value from the token
        /// </summary>
        /// <param name="token">The token to convert into the value</param>
        /// <param name="cc">The clause's execution context</param>
        public WhereValue(HDSLToken token, ClauseContext cc)
        {
            Column = token.Column;
            Row = token.Row;
            _cc = cc;
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
                    case HDSLTokenTypes.BookmarkReference:
                        ValueType = WhereValueTypes.String;
                        _actual = cc.Data.ApplyBookmarks(token.Code);
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
                    case HDSLTokenTypes.ColumnName:
                        var mappingType = _cc.Data.GetColumnType(token.Code, _cc.QueriedType);
                        if (mappingType != null)
                        {
                            _actual = token.Code;
                            if (mappingType == typeof(string))
                            {
                                ValueType = WhereValueTypes.String;
                            }
                            else if (mappingType == typeof(long))
                            {
                                ValueType = WhereValueTypes.WholeNumber;
                            }
                            else if (mappingType == typeof(double))
                            {
                                ValueType = WhereValueTypes.RealNumber;
                            }
                            else if (mappingType == typeof(DateTime))
                            {
                                ValueType = WhereValueTypes.DateTime;
                            }
                            else
                            {
                                throw new WhereClauseException(Column, Row, $"Unknown column type discovered '{mappingType.FullName}'.",  WhereClauseExceptionTypes.InvalidColumnTypeReferenced);
                            }
                        }
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
                    case HDSLTokenTypes.ColumnName:
                        var prop = item.GetType().GetProperties()
                            .Where(p => p.Name.Equals((string)_actual, StringComparison.InvariantCultureIgnoreCase))
                            .SingleOrDefault();

                        if (prop != null)
                        {
                            result = prop.GetValue(item);
                        }
                        else
                        {
                            throw new WhereClauseException(Column, Row, $"Column '{_actual}' not found on type '{item.GetType().FullName}'.", WhereClauseExceptionTypes.UnknownColumnReferenced);
                        }
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
                    case HDSLTokenTypes.ColumnName:
                        result = (string)_actual;
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
                    case HDSLTokenTypes.ColumnName:
                        result = (string)_actual;
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
