// Copyright (c) Zain Al-Ahmary.  All rights reserved.
// Licensed under the MIT License, (the "License"); you may not use this file except in compliance with the License. 
// You may obtain a copy of the License at https://mit-license.org/

using System;
using System.Data;
using System.Data.SQLite;
using System.Text;
using System.Linq;
using HDDL.Language.Json;

namespace HDDL.Data
{
    /// <summary>
    /// Associates an arbitrary value with an actual columns name
    /// </summary>
    public class ColumnNameMappingItem : HDDLRecordBase
    {
        private string _name;
        /// <summary>
        /// The original column's name
        /// </summary>
        internal string Name 
        { 
            get
            {
                return _name;
            }
            set
            {
                _name = value;
                GetDataType();
            }
        }

        /// <summary>
        /// The new alias for the original name
        /// </summary>
        public string Alias { get; set; }

        /// <summary>
        /// If the alias is currently in use
        /// </summary>
        public bool IsActive { get; set; }

        private string _hostType;
        /// <summary>
        /// A type reference for the class this column is pulled from
        /// </summary>
        public string HostType 
        { 
            get
            {
                return _hostType;
            }
            set
            {
                _hostType = value;
                GetDataType();
            }
        }

        /// <summary>
        /// The column's datatype
        /// </summary>
        [JsonIgnore]
        public Type DataType { get; private set; }

        /// <summary>
        /// Whether or not the column is returned by default
        /// </summary>
        public bool IsDefault { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public int DisplayWidth { get; set; }

        /// <summary>
        /// Creates an instance from the current record in the data reader
        /// </summary>
        /// <param name="row"></param>
        /// <param name="di"></param>
        public ColumnNameMappingItem(SQLiteDataReader row) : base(row)
        {
            HostType = row.GetString("type");
            Name = row.GetString("name");
            Alias = row.GetString("alias");
            IsActive = row.GetBoolean("isActive");
            IsDefault = row.GetBoolean("isDefault");
            DisplayWidth = row.GetInt32("width");
        }

        /// <summary>
        /// Default constructor
        /// </summary>
        public ColumnNameMappingItem() : base()
        {
        }

        /// <summary>
        /// Populates the DataType field with the type of the referenced property
        /// </summary>
        private void GetDataType()
        {
            if (DataType == null)
            {
                if (!string.IsNullOrWhiteSpace(_name) && !string.IsNullOrWhiteSpace(HostType))
                {
                    var props = Type.GetType(HostType).GetProperties();
                    DataType = (from p in props
                                where p.Name.Equals(_name, StringComparison.InvariantCultureIgnoreCase)
                                select p.PropertyType).SingleOrDefault();
                }
            }
        }

        /// <summary>
        /// Generates and returns a SQLite Insert statement for this record
        /// </summary>
        /// <returns>The line of SQL</returns>
        public override string ToInsertStatement()
        {
            return $@"insert into columnnamemappings 
                        (id, name, alias, isActive, type, isDefault, width) 
                      values 
                        ('{Id}', '{DataHelper.Sanitize(Name)}', '{DataHelper.Sanitize(Alias)}', {IsActive}, '{HostType}', {IsDefault}, {DisplayWidth});";
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
                            isActive = {IsActive},
                            type = '{HostType}',
                            isDefault = {IsDefault},
                            width = {DisplayWidth}
                        where id = '{Id}';";
        }

        public override string ToString()
        {
            var state = IsActive ? "on" : "off";
            var isDefault = IsDefault ? "def" : "opt";
            return $"[Map ({state}, {isDefault}) ({Id}): {HostType}.'{Name}' to '{Alias}']";
        }
    }
}
