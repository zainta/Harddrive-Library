using HDDL.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
        public List<string> Columns { get; private set; }

        /// <summary>
        /// Creates a column header set with the given columns
        /// </summary>
        /// <param name="columnNames"></param>
        public ColumnHeaderSet(IEnumerable<string> columnNames)
        {
            Columns = new List<string>(columnNames);
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
            foreach (var column in Columns.Reverse<string>())
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
            foreach (var column in Columns.Reverse<string>())
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
