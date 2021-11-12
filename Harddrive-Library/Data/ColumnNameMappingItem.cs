// Copyright (c) Zain Al-Ahmary.  All rights reserved.
// Licensed under the MIT License, (the "License"); you may not use this file except in compliance with the License. 
// You may obtain a copy of the License at https://mit-license.org/

using System.Data;
using System.Data.SQLite;
using System.Text;

namespace HDDL.Data
{
    /// <summary>
    /// Associates an arbitrary value with an actual columns name
    /// </summary>
    public class ColumnNameMappingItem : HDDLRecordBase
    {
        /// <summary>
        /// The original column's name
        /// </summary>
        internal string Name { get; set; }

        /// <summary>
        /// The new alias for the original name
        /// </summary>
        public string Alias { get; set; }

        /// <summary>
        /// If the alias is currently in use
        /// </summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// Creates an instance from the current record in the data reader
        /// </summary>
        /// <param name="row"></param>
        /// <param name="di"></param>
        public ColumnNameMappingItem(SQLiteDataReader row) : base(row)
        {
            Name = row.GetString("name");
            Alias = row.GetString("alias");
            IsActive = row.GetBoolean("isActive");
        }

        /// <summary>
        /// Default constructor
        /// </summary>
        public ColumnNameMappingItem() : base()
        {
        }

        /// <summary>
        /// Generates and returns a SQLite Insert statement for this record
        /// </summary>
        /// <returns>The line of SQL</returns>
        public override string ToInsertStatement()
        {
            return $@"insert into columnnamemappings 
                        (id, name, alias, isActive) 
                      values 
                        ('{Id}', '{DataHelper.Sanitize(Name)}', '{DataHelper.Sanitize(Alias)}', {IsActive});";
        }

        /// <summary>
        /// Generates and returns a SQLite Update statement for this record
        /// </summary>
        /// <returns>The line of SQL</returns>
        public override string ToUpdateStatement()
        {
            return $@"update columnnamemappings 
                        set name = '{DataHelper.Sanitize(Name)}',
                            alias = '{DataHelper.Sanitize(Alias)}',
                            isActive = {IsActive}
                        where id = '{Id}';";
        }

        public override string ToString()
        {
            var state = IsActive ? "on" : "off";
            return $"[Map ({state}) ({Id}): '{Name}' to '{Alias}']";
        }
    }
}
