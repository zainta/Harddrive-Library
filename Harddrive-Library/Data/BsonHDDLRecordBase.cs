using LiteDB;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HDDL.Data
{
    /// <summary>
    /// Base class for all Bson data item types used in this system
    /// </summary>
    public abstract class BsonHDDLRecordBase
    {
        /// <summary>
        /// The unique identified
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Create a default base instance
        /// </summary>
        protected BsonHDDLRecordBase()
        {
        }

        /// <summary>
        /// Creates an instance from the current record in the data reader
        /// </summary>
        /// <param name="record"></param>
        /// <param name="callingType"></param>
        protected BsonHDDLRecordBase(BsonValue record)
        {
            // Use reflection to copy the values over
            // first for this class
            foreach (var prop in GetType().GetProperties())
            {
                prop.SetValue(this, record[prop.Name == "Id" ? "_id" : prop.Name].RawValue);
            }
        }
    }
}
