// Copyright (c) Zain Al-Ahmary.  All rights reserved.
// Licensed under the MIT License, (the "License"); you may not use this file except in compliance with the License. 
// You may obtain a copy of the License at https://mit-license.org/

using System;
using System.Data;
using System.Data.SQLite;

namespace HDDL.Data
{
    /// <summary>
    /// Base class for all Bson data item types used in this system
    /// </summary>
    public abstract class HDDLRecordBase
    {
        /// <summary>
        /// The unique identified
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Create a default base instance
        /// </summary>
        protected HDDLRecordBase()
        {
        }

        /// <summary>
        /// Creates an instance from the current record in the data reader
        /// </summary>
        /// <param name="row"></param>
        /// <param name="callingType"></param>
        protected HDDLRecordBase(SQLiteDataReader row)
        {
            Id = row.GetGuid("id");
        }

        /// <summary>
        /// Generates and returns a SQLite Insert statement for this record
        /// </summary>
        /// <returns>The line of SQL</returns>
        public virtual string ToInsertStatement()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Generates and returns a SQLite Update statement for this record
        /// </summary>
        /// <returns>The line of SQL</returns>
        public virtual string ToUpdateStatement()
        {
            throw new NotImplementedException();
        }
    }
}
