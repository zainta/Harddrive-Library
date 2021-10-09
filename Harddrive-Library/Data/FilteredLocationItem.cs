// Copyright(c) Zain Al-Ahmary.  All rights reserved.
// Licensed under the MIT License, (the "License"); you may not use this file except in compliance with the License. 
// You may obtain a copy of the License at https://mit-license.org/

using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using System.IO;
using HDDL.IO.Disk;
using Microsoft.VisualBasic.CompilerServices;

namespace HDDL.Data
{
    /// <summary>
    /// Represents a filter reference item record, a bookmark with a wildcard filter and/or an attribute filter
    /// </summary>
    public class FilteredLocationItem : HDDLRecordBase
    {
        /// <summary>
        /// The item's target directory path
        /// </summary>
        public string Target { get; set; }

        /// <summary>
        /// A filter name / extension filter in the wildcard format
        /// </summary>
        public string Filter { get; set; }

        /// <summary>
        /// Whether or not this filter expects files that match to be read only
        /// </summary>
        public bool? ExpectsReadOnly { get; set; }

        /// <summary>
        /// Whether or not this filter expects files that match to be archives
        /// </summary>
        public bool? ExpectsArchive { get; set; }

        /// <summary>
        /// Whether or not this filter expects files that match to be system files
        /// </summary>
        public bool? ExpectsSystem { get; set; }

        /// <summary>
        /// Whether or not this filter expects files that match to be hidden files
        /// </summary>
        public bool? ExpectsHidden { get; set; }

        /// <summary>
        /// Whether or not this filter expects files that match to be non-indexed files
        /// </summary>
        public bool? ExpectsNonIndexed { get; set; }

        /// <summary>
        /// The name of the item
        /// </summary>
        public string ItemName { get; set; }

        /// <summary>
        /// The exploration mode used with this item
        /// </summary>
        public FilteredLocationExplorationMethod ExplorationMode { get; set; }

        /// <summary>
        /// Creates an instance from the current record in the data reader
        /// </summary>
        /// <param name="row"></param>
        public FilteredLocationItem(SQLiteDataReader row) : base(row)
        {
            Target = row.GetString("target");
            ItemName = row.GetString("itemName");
            ExplorationMode = Enum.Parse<FilteredLocationExplorationMethod>(row.GetString("explorationMode"));
            Filter = row["filter"] is DBNull ? null : row.GetString("filter");
            ExpectsReadOnly = row["expectsReadOnly"] is DBNull ? null : row.GetBoolean("expectsReadOnly");
            ExpectsArchive = row["expectsArchive"] is DBNull ? null : row.GetBoolean("expectsArchive");
            ExpectsSystem = row["expectsSystem"] is DBNull ? null : row.GetBoolean("expectsSystem");
            ExpectsHidden = row["expectsHidden"] is DBNull ? null : row.GetBoolean("expectsHidden");
            ExpectsNonIndexed = row["expectsNonIndexed"] is DBNull ? null : row.GetBoolean("expectsNonIndexed");
        }

        /// <summary>
        /// Default constructor
        /// </summary>
        public FilteredLocationItem() : base()
        {
        }

        /// <summary>
        /// Generates and returns a SQLite Insert statement for this record
        /// </summary>
        /// <returns>The line of SQL</returns>
        public override string ToInsertStatement()
        {
            StringBuilder cols = new StringBuilder(), vals = new StringBuilder();

            // first one, no need to worry about appending logic
            if (!string.IsNullOrWhiteSpace(Filter))
            {
                cols.Append("filter");
                vals.Append($"'{Filter}'");
            }
            if (ExpectsReadOnly.HasValue)
            {
                AppendSafely(cols, "expectsReadOnly", ", ");
                AppendSafely(vals, BoolToStr(ExpectsReadOnly), ", ");
            }
            if (ExpectsArchive.HasValue)
            {
                AppendSafely(cols, "expectsArchive", ", ");
                AppendSafely(vals, BoolToStr(ExpectsArchive), ", ");
            }
            if (ExpectsSystem.HasValue)
            {
                AppendSafely(cols, "expectsSystem", ", ");
                AppendSafely(vals, BoolToStr(ExpectsSystem), ", ");
            }
            if (ExpectsHidden.HasValue)
            {
                AppendSafely(cols, "expectsHidden", ", ");
                AppendSafely(vals, BoolToStr(ExpectsHidden), ", ");
            }
            if (ExpectsNonIndexed.HasValue)
            {
                AppendSafely(cols, "expectsNonIndexed", ", ");
                AppendSafely(vals, BoolToStr(ExpectsNonIndexed), ", ");
            }

            var sql = $@"insert into filteredlocations 
                        (id, target, itemName, explorationMode[additionsColumns]) 
                      values 
                        ('{Id}', 
                        '{DataHelper.Sanitize(Target)}', 
                        '{DataHelper.Sanitize(ItemName)}',
                        '{ExplorationMode}'[additionsValues]);";

            if (cols.Length > 0)
            {
                sql = sql.Replace("[additionsColumns]", $", {cols}");
            }
            else
            {
                sql = sql.Replace("[additionsColumns]", string.Empty);
            }

            if (vals.Length > 0)
            {
                sql = sql.Replace("[additionsValues]", $", {vals}");
            }
            else
            {
                sql = sql.Replace("[additionsValues]", string.Empty);
            }

            return sql;
        }

        /// <summary>
        /// Generates and returns a SQLite Update statement for this record
        /// </summary>
        /// <returns>The line of SQL</returns>
        public override string ToUpdateStatement()
        {
            StringBuilder upds = new StringBuilder();

            // first one, no need to worry about appending logic
            if (!string.IsNullOrWhiteSpace(Filter))
            {
                upds.Append($"set filter = '{Filter}'");
            }
            if (ExpectsReadOnly.HasValue)
            {
                AppendSafely(upds, $"expectsReadOnly = {BoolToStr(ExpectsReadOnly)}", ", ");
            }
            if (ExpectsArchive.HasValue)
            {
                AppendSafely(upds, $"expectsArchive = {BoolToStr(ExpectsArchive)}", ", ");
            }
            if (ExpectsSystem.HasValue)
            {
                AppendSafely(upds, $"expectsSystem = {BoolToStr(ExpectsSystem)}", ", ");
            }
            if (ExpectsHidden.HasValue)
            {
                AppendSafely(upds, $"expectsHidden = {BoolToStr(ExpectsHidden)}", ", ");
            }
            if (ExpectsNonIndexed.HasValue)
            {
                AppendSafely(upds, $"expectsNonIndexed = {BoolToStr(ExpectsNonIndexed)}", ", ");
            }

            var sql = $@"update filteredlocations 
                        set target = '{DataHelper.Sanitize(Target)}',
                            explorationMode = '{ExplorationMode}'[additionsUpdates]
                        where itemName = '{ItemName}';";

            if (upds.Length > 0)
            {
                sql = sql.Replace("[additionsUpdates]", $", {upds}");
            }
            else
            {
                sql = sql.Replace("[additionsUpdates]", string.Empty);
            }

            return sql;
        }

        /// <summary>
        /// Converts a nullable bool into a string as follows:
        /// true = 1,
        /// false = 0,
        /// null = empty string
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        private string BoolToStr(bool? value)
        {
            return value.HasValue ? (value == true ? "1" : "0") : string.Empty;
        }

        /// <summary>
        /// Appends a string to a string builder.  
        /// Only uses the additional seperator if there are already items in the string builder.
        /// 
        /// Warning: this is a dumb method, it does not parse the string, it checks length > 0.
        /// </summary>
        /// <param name="sb"></param>
        /// <param name="str"></param>
        /// <param name="additionalSeperator"></param>
        /// <returns></returns>
        private StringBuilder AppendSafely(StringBuilder sb, string str, string additionalSeperator)
        {
            if (sb.Length > 0)
            {
                sb.Append(additionalSeperator);
            }
            sb.Append(str);

            return sb;
        }

        /// <summary>
        /// Checks to see if the given path is a match to the criteria defined within this Filtered Location Item
        /// </summary>
        /// <param name="path">A valid file path to test</param>
        /// <returns></returns>
        public bool IsMatch(string path)
        {
            var result = false;

            // is it a proper file path and does it exist?
            if (File.Exists(path))
            {
                // make sure it's in the right location
                if (PathHelper.IsWithinPath(path, Target))
                {
                    var fi = new FileInfo(path);
                    var attributes = fi.Attributes;
                    result = true;

                    // compare the filter
                    if (!string.IsNullOrWhiteSpace(Filter) &&
                        !LikeOperator.LikeString(fi.Name, Filter, Microsoft.VisualBasic.CompareMethod.Binary))
                    {
                        result = false;
                    }

                    // compare the attributes

                    // readonly
                    if (ExpectsReadOnly.HasValue)
                    {
                        if (ExpectsReadOnly.Value &&
                            ExpectsReadOnly != ((attributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly))
                        {
                            result = false;
                        }
                    }

                    // archive
                    if (ExpectsArchive.HasValue)
                    {
                        if (ExpectsArchive.Value &&
                            ExpectsArchive != ((attributes & FileAttributes.Archive) == FileAttributes.Archive))
                        {
                            result = false;
                        }
                    }

                    // system
                    if (ExpectsSystem.HasValue)
                    {
                        if (ExpectsSystem.Value &&
                            ExpectsSystem != ((attributes & FileAttributes.System) == FileAttributes.System))
                        {
                            result = false;
                        }
                    }

                    // hidden
                    if (ExpectsHidden.HasValue)
                    {
                        if (ExpectsHidden.Value &&
                            ExpectsHidden != ((attributes & FileAttributes.Hidden) == FileAttributes.Hidden))
                        {
                            result = false;
                        }
                    }

                    // not content indexed
                    if (ExpectsNonIndexed.HasValue)
                    {
                        if (ExpectsNonIndexed.Value &&
                            ExpectsNonIndexed != ((attributes & FileAttributes.NotContentIndexed) == FileAttributes.NotContentIndexed))
                        {
                            result = false;
                        }
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Creates a deep clone of the instance
        /// </summary>
        /// <returns></returns>
        internal FilteredLocationItem Copy()
        {
            var result = new FilteredLocationItem()
            {
                Id = Guid.NewGuid(),
                ItemName = ItemName,
                Filter = Filter,
                Target = Target,
                ExplorationMode = ExplorationMode,
                ExpectsReadOnly = ExpectsReadOnly,
                ExpectsArchive = ExpectsArchive,
                ExpectsSystem = ExpectsSystem,
                ExpectsHidden = ExpectsHidden,
                ExpectsNonIndexed = ExpectsNonIndexed
            };
            return result;
        }

        public override string ToString()
        {
            var sb = new StringBuilder(@" - {");
            if (!string.IsNullOrWhiteSpace(Filter))
            {
                if (sb.Length > 4)
                {
                    sb.Append(", ");
                }

                sb.Append($"filter: '{Filter}'");
            }

            if (ExpectsReadOnly.HasValue)
            {
                if (sb.Length > 4)
                {
                    sb.Append(", ");
                }

                sb.Append($"readonly: {(ExpectsReadOnly == true ? "Yes" : "No")}");
            }

            if (ExpectsArchive.HasValue)
            {
                if (sb.Length > 4)
                {
                    sb.Append(", ");
                }

                sb.Append($"archive: {(ExpectsArchive == true ? "Yes" : "No")}");
            }

            if (ExpectsSystem.HasValue)
            {
                if (sb.Length > 4)
                {
                    sb.Append(", ");
                }

                sb.Append($"system: {(ExpectsSystem == true ? "Yes" : "No")}");
            }

            if (ExpectsHidden.HasValue)
            {
                if (sb.Length > 4)
                {
                    sb.Append(", ");
                }

                sb.Append($"hidden: {(ExpectsHidden == true ? "Yes" : "No")}");
            }

            if (ExpectsNonIndexed.HasValue)
            {
                if (sb.Length > 4)
                {
                    sb.Append(", ");
                }

                sb.Append($"non indexed: {(ExpectsNonIndexed == true ? "Yes" : "No")}");
            }

            sb.Append("}");

            if (sb.Length > 5)
            {
                return $"['{ItemName}': {ExplorationMode} {Target}{sb}]";
            }
            else
            {
                return $"['{ItemName}': {ExplorationMode} {Target}]";
            }
        }
    }
}
