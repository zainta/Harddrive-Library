// Copyright (c) Zain Al-Ahmary.  All rights reserved.
// Licensed under the MIT License, (the "License"); you may not use this file except in compliance with the License. 
// You may obtain a copy of the License at https://mit-license.org/

using HDDL.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace HDDL.HDSL
{
    /// <summary>
    /// Describes a sequence of columns that are to be returned for use
    /// </summary>
    public class ColumnHeaderSet
    {
        /// <summary>
        /// The columns to display, in the order to display them
        /// </summary>
        public string[] Columns { get; private set; }

        /// <summary>
        /// The mappings for the requested columns
        /// </summary>
        public ColumnNameMappingItem[] Mappings { get; private set; }

        /// <summary>
        /// The type of the header set is for (should be derived from HDDLRecordBase)
        /// </summary>
        private Type _for;

        /// <summary>
        /// Creates a Column Header Set by filling the columns list with information from the mapping metadata
        /// </summary>
        /// <param name="mappings">The defined mappings</param>
        /// <param name="forType">The type of the header set is for (should be derived from HDDLRecordBase)</param>
        public ColumnHeaderSet(IEnumerable<ColumnNameMappingItem> mappings, Type forType)
        {
            _for = forType;
            Columns = (from m in mappings select m.Name).ToArray();
            Mappings = mappings.ToArray();
        }

        /// <summary>
        /// Creates a default Column Header Set that contains all default columns
        /// </summary>
        /// <param name="dh">The datahandler to query for the information</param>
        /// <param name="forType">The type of the header set is for (should be derived from HDDLRecordBase)</param>
        public ColumnHeaderSet(DataHandler dh, Type forType)
        {
            _for = forType;
            Mappings = dh.GetColumnNameMappings(_for)
                .Where(m => m.IsDefault)
                .ToArray();

            Columns = (from m in Mappings select m.Name).ToArray();
        }

        /// <summary>
        /// Takes an HDDLRecordBase and returns the columns defined in the column header set
        /// </summary>
        /// <param name="record">The record to pull values from</param>
        /// <returns>The values that were found</returns>
        public object[] GetValues(HDDLRecordBase record)
        {
            var foundProps = new List<PropertyInfo>();

            var props = record.GetType().GetProperties();
            foreach (var column in Columns)
            {
                var prop = (from p in props where p.Name.Equals(column, StringComparison.InvariantCultureIgnoreCase) select p).SingleOrDefault();
                if (prop != null)
                {
                    foundProps.Add(prop);
                }
            }

            return (from p in foundProps select p.GetValue(record)).Reverse().ToArray();
        }

        /// <summary>
        /// Takes an HDDLRecordBase and returns the column -> value pairs defined in the column header set
        /// </summary>
        /// <param name="record">The record to pull from</param>
        /// <returns>The values that were found</returns>
        public Dictionary<string, object> GetColumns(HDDLRecordBase record)
        {
            var results = new Dictionary<string, object>();

            var props = record.GetType().GetProperties();
            foreach (var column in Columns)
            {
                var prop = (from p in props where p.Name.Equals(column, StringComparison.InvariantCultureIgnoreCase) select p).SingleOrDefault();
                if (prop != null)
                {
                    results.Add(prop.Name, prop.GetValue(record));
                }
            }

            return results;
        }

        /// <summary>
        /// Takes an HDDLRecordBase and returns a dictionary containing the PropertyInfo (key) and their values (value) the columns defined in the column header set
        /// </summary>
        /// <param name="record">The record to pull from</param>
        /// <returns>The dictionary containing the value data</returns>
        public Dictionary<PropertyInfo, object> GetValueData(HDDLRecordBase record)
        {
            var results = new Dictionary<PropertyInfo, object>();

            var props = record.GetType().GetProperties();
            foreach (var column in Columns)
            {
                var prop = (from p in props where p.Name.Equals(column, StringComparison.InvariantCultureIgnoreCase) select p).SingleOrDefault();
                if (prop != null)
                {
                    results.Add(prop, prop.GetValue(record));
                }
            }

            return results;
        }
    }
}
