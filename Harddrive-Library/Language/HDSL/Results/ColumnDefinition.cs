// Copyright (c) Zain Al-Ahmary.  All rights reserved.
// Licensed under the MIT License, (the "License"); you may not use this file except in compliance with the License. 
// You may obtain a copy of the License at https://mit-license.org/

using HDDL.Data;
using System;

namespace HDDL.Language.HDSL.Results
{
    /// <summary>
    /// Ties a column name to its type
    /// </summary>
    public class ColumnDefinition
    {
        /// <summary>
        /// Defines a constant value that declares a column as having unrestricted width
        /// </summary>
        public const int UnrestrictedWidth = -1;

        /// <summary>
        /// The name of the column
        /// </summary>
        public string Column { get; set; }

        /// <summary>
        /// The name of the column's data type
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// The column's width
        /// </summary>
        public int Width { get; set; }

        /// <summary>
        /// Creates a column definition from the provided mapping instance
        /// </summary>
        /// <param name="mapping">The mapping</param>
        public ColumnDefinition(ColumnNameMappingItem mapping)
        {
            if (mapping == null) throw new ArgumentException("Null mapping provided!");

            Column = mapping.Name;
            Type = mapping.DataType.FullName;
            Width = mapping.DisplayWidth;
        }

        public override string ToString()
        {
            return $"Column: {Column} | {Type} | {Width}";
        }
    }
}
