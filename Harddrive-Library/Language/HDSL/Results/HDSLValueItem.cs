// Copyright (c) Zain Al-Ahmary.  All rights reserved.
// Licensed under the MIT License, (the "License"); you may not use this file except in compliance with the License. 
// You may obtain a copy of the License at https://mit-license.org/

namespace HDDL.Language.HDSL.Results
{
    /// <summary>
    /// Stores a value, its column, and its datatype for json transportation
    /// </summary>
    public class HDSLValueItem
    {
        /// <summary>
        /// The value represented
        /// </summary>
        public object Value { get; private set; }

        /// <summary>
        /// The column the value comes from
        /// </summary>
        public string Column { get; private set; }

        /// <summary>
        /// The column's datatype
        /// </summary>
        public string ColumnType { get; private set; }

        /// <summary>
        /// Json support constructor
        /// </summary>
        public HDSLValueItem()
        {
            Column = null;
            ColumnType = null;
            Value = null;
        }

        /// <summary>
        /// Create an HDSLValueItem
        /// </summary>
        /// <param name="column"></param>
        /// <param name="type"></param>
        /// <param name="value"></param>
        public HDSLValueItem(string column, string type, object value)
        {
            Column = column;
            ColumnType = type;
            Value = value;
        }

        public override string ToString()
        {
            return $"[{Column}, '{ColumnType}', '{Value}']";
        }
    }
}
