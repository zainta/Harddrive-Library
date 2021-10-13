// Copyright (c) Zain Al-Ahmary.  All rights reserved.
// Licensed under the MIT License, (the "License"); you may not use this file except in compliance with the License. 
// You may obtain a copy of the License at https://mit-license.org/

using HDDL.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace HDDL.HDSL
{
    /// <summary>
    /// Implements a column based system that allows dynamic, table-based display of DiskItem enumerations
    /// </summary>
    public class HDSLQueryOutcome
    {
        private const int Column_Default_Width_Location = 100;
        private const int Column_Default_Width_FullPath = 100;
        private const int Column_Default_Width_ItemName = 100;
        private const int Column_Default_Width_Extension = 5;
        private const int Column_Default_Width_IsFile = 1;
        private const int Column_Default_Width_Size = 10;
        private const int Column_Default_Width_LastWrite = 23;
        private const int Column_Default_Width_LastAccess = 23;
        private const int Column_Default_Width_Creation = 23;
        private const int Column_Default_Width_Hash = 50;
        private const int Column_Default_Width_LastHashed = 23;
        private const int Column_Default_Width_Attributes = 10;
        private const int Column_Default_Width_Attributes_Extended = 12;
        private const int Column_Default_Width_Attributes_Three = 20;
        private const int Column_Default_Width_Attributes_Extended_Three = 30;

        private const string Column_Name_Location = "Location";
        private const string Column_Name_FullPath = "Path";
        private const string Column_Name_ItemName = "Name";
        private const string Column_Name_Extension = "Ext";
        private const string Column_Name_IsFile = "File?";
        private const string Column_Name_Size = "Size";
        private const string Column_Name_LastWritten = "Write";
        private const string Column_Name_LastAccessed = "Accessed";
        private const string Column_Name_Creation = "Created";
        private const string Column_Name_Hash = "Checksum Hash";
        private const string Column_Name_LastHashed = "Last Hashed";
        private const string Column_Name_Attributes = "Attributes";
        private const string Column_Name_Attributes_Extended = "Attributes ";
        private const string Column_Name_Attributes_Three = "Attributes  ";
        private const string Column_Name_Attributes_Extended_Three = "Attributes   ";

        private const int Default_Page_Row_Count = 32;
        private const int Default_Page_Index = -1;
        private const int Min_Page_Row_Count = 10;
        private const string Page_Size_Entry = "pagesize";
        private const string Page_Index = "pageindex";

        private string Default_ColumnData = "psc";
        private string Default_PagingData = "-1:-1";

        /// <summary>
        /// The result set these results are a part of
        /// </summary>
        public HDSLResult Parent { get; internal set; }

        private string _defaultColumnData;
        /// <summary>
        /// The default column data used by this instance
        /// </summary>
        public string DefaultColumnData 
        { 
            get
            {
                return _defaultColumnData;
            }
            set
            {
                // the formatting fields' values should never be null.
                // If they are, and a value is assigned, take it regardless.
                if (string.IsNullOrWhiteSpace(_defaultColumnData))
                {
                    _defaultColumnData = value;
                }
                else if (!string.IsNullOrWhiteSpace(value))
                {
                    _defaultColumnData = value;
                }
            }
        }

        private string _defaultPagingData;
        /// <summary>
        /// The default paging data used by this instance
        /// </summary>
        public string DefaultPagingData
        {
            get
            {
                return _defaultPagingData;
            }
            set
            {
                // the formatting fields' values should never be null.
                // If they are, and a value is assigned, take it regardless.
                if (string.IsNullOrWhiteSpace(_defaultPagingData))
                {
                    _defaultPagingData = value;
                }
                else if (!string.IsNullOrWhiteSpace(value))
                {
                    _defaultPagingData = value;
                }
            }
        }

        /// <summary>
        /// Creates a HDSLQueryOutcome
        /// </summary>
        public HDSLQueryOutcome()
        {
            Parent = null;
            DefaultColumnData = Default_ColumnData;
            DefaultPagingData = Default_PagingData;
        }

        #region Table Structuring

        /// <summary>
        /// Takes in a column string and returns a list containing the column identifier, the column index, and the column width
        /// </summary>
        /// <param name="columnStr">The encoded parameter information</param>
        /// <returns>The resulting information</returns>
        private List<Tuple<string, int, int>> GetColumnTable(string columnStr)
        {
            // create and setup defaults
            var results = new List<Tuple<string, int, int>>() { };

            if (columnStr.Contains(':'))
            {
                var definitions = columnStr.Split(',', ':');
                if (definitions.Length % 2 == 0)
                {
                    for (var index = 0; index < definitions.Length; index++)
                    {
                        // a note: because we are consuming this data in pairs (column -> width sets),
                        // we let the for loop increment normally and skip one manually.
                        switch (definitions[index])
                        {
                            case "l": // location column
                                if (!Contains(results, Column_Name_Location))
                                {
                                    results.Add(new Tuple<string, int, int>(Column_Name_Location, results.Count, int.Parse(definitions[index + 1])));
                                    index++;
                                }
                                break;
                            case "p": // full path column
                                if (!Contains(results, Column_Name_FullPath))
                                {
                                    results.Add(new Tuple<string, int, int>(Column_Name_FullPath, results.Count, int.Parse(definitions[index + 1])));
                                    index++;
                                }
                                break;
                            case "n": // item name column
                                if (!Contains(results, Column_Name_ItemName))
                                {
                                    results.Add(new Tuple<string, int, int>(Column_Name_ItemName, results.Count, int.Parse(definitions[index + 1])));
                                    index++;
                                }
                                break;
                            case "e": // extension column
                                if (!Contains(results, Column_Name_Extension))
                                {
                                    results.Add(new Tuple<string, int, int>(Column_Name_Extension, results.Count, int.Parse(definitions[index + 1])));
                                    index++;
                                }
                                break;
                            case "i": // is file column
                                if (!Contains(results, Column_Name_IsFile))
                                {
                                    results.Add(new Tuple<string, int, int>(Column_Name_IsFile, results.Count, int.Parse(definitions[index + 1])));
                                    index++;
                                }
                                break;
                            case "s": // size column
                                if (!Contains(results, Column_Name_Size))
                                {
                                    results.Add(new Tuple<string, int, int>(Column_Name_Size, results.Count, int.Parse(definitions[index + 1])));
                                    index++;
                                }
                                break;
                            case "w": // last written column
                                if (!Contains(results, Column_Name_LastWritten))
                                {
                                    results.Add(new Tuple<string, int, int>(Column_Name_LastWritten, results.Count, int.Parse(definitions[index + 1])));
                                    index++;
                                }
                                break;
                            case "a": // last accessed column
                                if (!Contains(results, Column_Name_LastAccessed))
                                {
                                    results.Add(new Tuple<string, int, int>(Column_Name_LastAccessed, results.Count, int.Parse(definitions[index + 1])));
                                    index++;
                                }
                                break;
                            case "c": // creation date column
                                if (!Contains(results, Column_Name_Creation))
                                {
                                    results.Add(new Tuple<string, int, int>(Column_Name_Creation, results.Count, int.Parse(definitions[index + 1])));
                                    index++;
                                }
                                break;
                            case "h": // checksum hash column
                                if (!Contains(results, Column_Name_Hash))
                                {
                                    results.Add(new Tuple<string, int, int>(Column_Name_Hash, results.Count, int.Parse(definitions[index + 1])));
                                    index++;
                                }
                                break;
                            case "d": // last checksum date column
                                if (!Contains(results, Column_Name_LastHashed))
                                {
                                    results.Add(new Tuple<string, int, int>(Column_Name_LastHashed, results.Count, int.Parse(definitions[index + 1])));
                                    index++;
                                }
                                break;
                            case "t": // simple attribute column
                                if (!Contains(results, Column_Name_Attributes))
                                {
                                    results.Add(new Tuple<string, int, int>(Column_Name_Attributes, results.Count, int.Parse(definitions[index + 1])));
                                    index++;
                                }
                                break;
                            case "T": // extended attribute column
                                if (!Contains(results, Column_Name_Attributes_Extended))
                                {
                                    results.Add(new Tuple<string, int, int>(Column_Name_Attributes_Extended, results.Count, int.Parse(definitions[index + 1])));
                                    index++;
                                }
                                break;
                            case "3": // three character simple attribute column
                                if (!Contains(results, Column_Name_Attributes_Three))
                                {
                                    results.Add(new Tuple<string, int, int>(Column_Name_Attributes_Three, results.Count, int.Parse(definitions[index + 1])));
                                    index++;
                                }
                                break;
                            case "#": // three character extended attribute column
                                if (!Contains(results, Column_Name_Attributes_Extended_Three))
                                {
                                    results.Add(new Tuple<string, int, int>(Column_Name_Attributes_Extended_Three, results.Count, int.Parse(definitions[index + 1])));
                                    index++;
                                }
                                break;
                        }
                    }
                }
            }
            else
            {
                for (var index = 0; index < columnStr.Length; index++)
                {
                    switch (columnStr[index])
                    {
                        case 'l': // location column
                            if (!Contains(results, Column_Name_Location))
                            {
                                results.Add(new Tuple<string, int, int>(Column_Name_Location, index, Column_Default_Width_Location));
                            }
                            break;
                        case 'p': // full path column
                            if (!Contains(results, Column_Name_FullPath))
                            {
                                results.Add(new Tuple<string, int, int>(Column_Name_FullPath, index, Column_Default_Width_FullPath));
                            }
                            break;
                        case 'n': // item name column
                            if (!Contains(results, Column_Name_ItemName))
                            {
                                results.Add(new Tuple<string, int, int>(Column_Name_ItemName, index, Column_Default_Width_ItemName));
                            }
                            break;
                        case 'e': // extension column
                            if (!Contains(results, Column_Name_Extension))
                            {
                                results.Add(new Tuple<string, int, int>(Column_Name_Extension, index, Column_Default_Width_Extension));
                            }
                            break;
                        case 'i': // is file column
                            if (!Contains(results, Column_Name_IsFile))
                            {
                                results.Add(new Tuple<string, int, int>(Column_Name_IsFile, index, Column_Default_Width_IsFile));
                            }
                            break;
                        case 's': // size column
                            if (!Contains(results, Column_Name_Size))
                            {
                                results.Add(new Tuple<string, int, int>(Column_Name_Size, index, Column_Default_Width_Size));
                            }
                            break;
                        case 'w': // last written column
                            if (!Contains(results, Column_Name_LastWritten))
                            {
                                results.Add(new Tuple<string, int, int>(Column_Name_LastWritten, index, Column_Default_Width_LastWrite));
                            }
                            break;
                        case 'a': // last accessed column
                            if (!Contains(results, Column_Name_LastAccessed))
                            {
                                results.Add(new Tuple<string, int, int>(Column_Name_LastAccessed, index, Column_Default_Width_LastAccess));
                            }
                            break;
                        case 'c': // creation date column
                            if (!Contains(results, Column_Name_Creation))
                            {
                                results.Add(new Tuple<string, int, int>(Column_Name_Creation, index, Column_Default_Width_Creation));
                            }
                            break;
                        case 'h': // checksum hash column
                            if (!Contains(results, Column_Name_Hash))
                            {
                                results.Add(new Tuple<string, int, int>(Column_Name_Hash, index, Column_Default_Width_Hash));
                            }
                            break;
                        case 'd': // last checksum date column
                            if (!Contains(results, Column_Name_LastHashed))
                            {
                                results.Add(new Tuple<string, int, int>(Column_Name_LastHashed, index, Column_Default_Width_LastHashed));
                            }
                            break;
                        case 't': // simple attribute column
                            if (!Contains(results, Column_Name_Attributes))
                            {
                                results.Add(new Tuple<string, int, int>(Column_Name_Attributes, index, Column_Default_Width_Attributes));
                            }
                            break;
                        case 'T': // extended attribute column
                            if (!Contains(results, Column_Name_Attributes_Extended))
                            {
                                results.Add(new Tuple<string, int, int>(Column_Name_Attributes_Extended, index, Column_Default_Width_Attributes_Extended));
                            }
                            break;
                        case '3': // three character simple attribute column
                            if (!Contains(results, Column_Name_Attributes_Three))
                            {
                                results.Add(new Tuple<string, int, int>(Column_Name_Attributes_Three, index, Column_Default_Width_Attributes_Three));
                            }
                            break;
                        case '#': // three character extended attribute column
                            if (!Contains(results, Column_Name_Attributes_Extended_Three))
                            {
                                results.Add(new Tuple<string, int, int>(Column_Name_Attributes_Extended_Three, index, Column_Default_Width_Attributes_Extended_Three));
                            }
                            break;
                    }
                }
            }

            return results;
        }

        /// <summary>
        /// Takes an encoded paging string and converts it into page index / page size
        /// Defaults to -1:-1, where those values equal unlimited / 32
        /// </summary>
        /// <param name="paging">The encoded paging string</param>
        /// <returns></returns>
        private Dictionary<string, int> GetPagingData(string paging)
        {
            var result = new Dictionary<string, int>();
            if (paging.Contains(":"))
            {
                // there are 3 possibilities that are acceptable:
                // :n, n: or n:n
                // where n is a positive integer
                var m = Regex.Match(paging, @"^(-1|\d*):(-1|[\d]*)$");
                if (m.Groups.Count == 3)
                {
                    var pageIndex = string.IsNullOrWhiteSpace(m.Groups[1].Value) || m.Groups[1].Value == "-1" ? Default_Page_Index : int.Parse(m.Groups[1].Value);
                    var rowsInPage = string.IsNullOrWhiteSpace(m.Groups[2].Value) || m.Groups[2].Value == "-1" ? Default_Page_Row_Count : int.Parse(m.Groups[2].Value);

                    result.Add(Page_Size_Entry, rowsInPage >= Min_Page_Row_Count ? rowsInPage : Min_Page_Row_Count);
                    result.Add(Page_Index, pageIndex > 0 ? pageIndex - 1 : pageIndex);
                }
                else
                {
                    Console.Write("Invalid paging string provided.  \nMust be in the form: n:n, where n is an optional integer value.  One or both must be supplied.");
                    Console.WriteLine("  The first value is the page to display, omitting it will display all pages of results.  The second value is the number of rows to display per page.");
                    return null;
                }
            }

            // Set default values if the parameter was badly formatted
            if (result.Count < 2)
            {
                result.Add(Page_Size_Entry, Default_Page_Row_Count);
                result.Add(Page_Index, Default_Page_Index);
            }

            return result;
        }

        /// <summary>
        /// Checks to see if the container already contains the column
        /// </summary>
        /// <param name="container">The columns to check</param>
        /// <param name="column">The column to check for</param>
        /// <returns>True if found, false otherwise</returns>
        private bool Contains(List<Tuple<string, int, int>> container, string column)
        {
            return (from t in container where t.Item1 == column select t).Any();
        }

        #endregion

        #region Formatting

        /// <summary>
        /// Takes a path and removes folders until it is under the given length
        /// </summary>
        /// <param name="path">The path to shorten</param>
        /// <param name="maxLength">The desired maximum length</param>
        /// <param name="delimiter">The delimiter to shorten baseed on</param>
        /// <returns>The resultant string</returns>
        private string ShortenString(string path, int maxLength = 30, char delimiter = '\\')
        {
            if (path.Length <= maxLength)
            {
                return path;
            }

            int startPartsRemoved = 1, endPartsRemoved = 1;
            var parts = path.Split(delimiter).ToList();
            int start = (parts.Count / 2), end = (parts.Count / 2);
            var pulse = string.Empty;
            var moveStart = true;
            do
            {
                pulse = string.Join(delimiter, from p in parts where parts.IndexOf(p) <= start select p);
                pulse += $"{new string(delimiter, startPartsRemoved)}...{new string(delimiter, endPartsRemoved)}";
                pulse += string.Join(delimiter, from p in parts where parts.IndexOf(p) >= end select p);

                if (pulse.Length > maxLength)
                {
                    if (moveStart && start > 0)
                    {
                        start--;
                        startPartsRemoved++;
                    }
                    else if (!moveStart && end < parts.Count)
                    {
                        end++;
                        endPartsRemoved++;
                    }
                    moveStart = !moveStart;
                }
            }
            while (pulse.Length > maxLength);

            return pulse;
        }

        /// <summary>
        /// Takes in a numerical value and reduces it to a textual representation (e.g 1.1mb)
        /// </summary>
        /// <param name="value">The value to shorten</param>
        /// <returns></returns>
        private string ShortenSize(long? value)
        {
            if (value.HasValue)
            {
                var abbreviations = new string[] { "B", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB", "XB", "SB", "DB" };
                var degrees = 1;
                long denomination = 1024;
                while (value > denomination)
                {
                    degrees++;
                    denomination *= 1024;
                }
                degrees--;
                denomination /= 1024;

                var displayValue = Math.Truncate(100 * ((double)value) / denomination) / 100;
                var result = $"{displayValue}{abbreviations[degrees]}";
                return result;
            }
            else
            {
                return "0B";
            }
        }

        /// <summary>
        /// Truncates the string if it is longer than the maximum.
        /// Uses the final 3 characters to indicate truncation with '...'
        /// </summary>
        /// <param name="value">The value to consider</param>
        /// <param name="maxlength">The maximum permitted length of the column's content</param>
        /// <returns>The resulting value</returns>
        private string EnforceLength(string value, int maxlength = 30)
        {
            if (value.Length <= maxlength) return value;
            return $"{value.Substring(0, maxlength - 3)}...";
        }

        /// <summary>
        /// Takes a file attribute value and returns the first letter of each selected attribute in a block
        /// </summary>
        /// <param name="attributes">The attributes to process</param>
        /// <param name="mode">The way to display the attributes.</param>
        /// <returns>The abbreviated string</returns>
        private string GetAttributeAbbreviation(FileAttributes attributes, AttributeDisplayMethods mode = AttributeDisplayMethods.Simple)
        {
            var results = new StringBuilder();
            var vals = Enum.GetValues<FileAttributes>();
            foreach (var v in vals)
            {
                if (attributes.HasFlag(v))
                {
                    Append(results, v, mode);
                }
            }

            return results.ToString();
        }

        /// <summary>
        /// Appends the correct thing to the string builder based on the mode and attribute
        /// </summary>
        /// <param name="sb">The string builder to append to</param>
        /// <param name="attribute">The attribute to represent</param>
        /// <param name="mode">The mode of display</param>
        private void Append(StringBuilder sb, FileAttributes attribute, AttributeDisplayMethods mode)
        {
            if (mode == AttributeDisplayMethods.ThreeCharacterExtended ||
                                mode == AttributeDisplayMethods.ThreeCharacterSimple)
            {
                if (sb.Length > 0)
                {
                    sb.Append(", ");
                }
                sb.Append(GetAbbreviation(attribute));
            }
            else if (mode == AttributeDisplayMethods.Extended)
            {
                sb.Append(GetLetter(attribute));
            }
        }

        private char GetLetter(FileAttributes attribute)
        {
            var result = ' ';
            switch (attribute)
            {
                case FileAttributes.ReadOnly:
                    result = 'r';
                    break;
                case FileAttributes.Hidden:
                    result = 'h';
                    break;
                case FileAttributes.System:
                    result = 's';
                    break;
                case FileAttributes.Archive:
                    result = 'a';
                    break;
                case FileAttributes.Directory:
                    result = 'd';
                    break;
                case FileAttributes.Normal:
                    result = 'n';
                    break;
                case FileAttributes.Device:
                    result = 'v';
                    break;
                case FileAttributes.Temporary:
                    result = 't';
                    break;
                case FileAttributes.SparseFile:
                    result = 'p';
                    break;
                case FileAttributes.ReparsePoint:
                    result = 'P';
                    break;
                case FileAttributes.Compressed:
                    result = 'c';
                    break;
                case FileAttributes.Offline:
                    result = 'o';
                    break;
                case FileAttributes.NotContentIndexed:
                    result = 'i';
                    break;
                case FileAttributes.Encrypted:
                    result = 'e';
                    break;
                case FileAttributes.IntegrityStream:
                    result = 'I';
                    break;
                case FileAttributes.NoScrubData:
                    result = 'S';
                    break;
            }

            return result;
        }

        /// <summary>
        /// Returns the three character abbreviation for the given attribute
        /// </summary>
        /// <param name="attribute">The attribute to return the abbreviation for</param>
        /// <returns></returns>
        private string GetAbbreviation(FileAttributes attribute)
        {
            var result = string.Empty;

            switch (attribute)
            {
                case FileAttributes.ReadOnly:
                    result = "rdo";
                    break;
                case FileAttributes.Hidden:
                    result = "hdn";
                    break;
                case FileAttributes.System:
                    result = "sys";
                    break;
                case FileAttributes.Archive:
                    result = "arc";
                    break;
                case FileAttributes.Directory:
                    result = "dir";
                    break;
                case FileAttributes.Normal:
                    result = "nrm";
                    break;
                case FileAttributes.Device:
                    result = "dvc";
                    break;
                case FileAttributes.Temporary:
                    result = "tmp";
                    break;
                case FileAttributes.SparseFile:
                    result = "spf";
                    break;
                case FileAttributes.ReparsePoint:
                    result = "rpc";
                    break;
                case FileAttributes.Compressed:
                    result = "cmp";
                    break;
                case FileAttributes.Offline:
                    result = "off";
                    break;
                case FileAttributes.NotContentIndexed:
                    result = "nci";
                    break;
                case FileAttributes.Encrypted:
                    result = "enc";
                    break;
                case FileAttributes.IntegrityStream:
                    result = "ist";
                    break;
                case FileAttributes.NoScrubData:
                    result = "nsd";
                    break;
            }

            return result;
        }

        #endregion

        /// <summary>
        /// Displays the appropriate information from the HDSLResult instance
        /// </summary>
        /// <param name="displayedItems">The data to display</param>
        /// <param name="pageDimensions">The paging description data</param>
        /// <param name="columnData">A character encoded column string</param>
        /// <param name="displayTableEmbelishments">Whether or not to display table lines in the results</param>
        protected void DisplayResultTable(IEnumerable<DiskItem> displayedItems, string columnData = null, string pageDimensions = null, bool displayTableEmbelishments = true)
        {
            if (string.IsNullOrWhiteSpace(columnData))
            {
                columnData = DefaultColumnData;
            }
            if (string.IsNullOrWhiteSpace(pageDimensions))
            {
                pageDimensions = DefaultPagingData;
            }

            Dictionary<string, int> paging = null;
            if (string.IsNullOrWhiteSpace(pageDimensions))
            {
                return;
            }
            else
            {
                paging = GetPagingData(pageDimensions);
            }

            if (Parent.Errors.Length == 0)
            {
                var cols = GetColumnTable(columnData);

                DiskItem[] pagedSet = null;
                if (paging[Page_Index] != -1)
                {
                    pagedSet = displayedItems.Skip(paging[Page_Index] * paging[Page_Size_Entry]).Take(paging[Page_Size_Entry]).ToArray();
                }
                else
                {
                    pagedSet = displayedItems.ToArray();
                }

                StringBuilder sb = new StringBuilder();
                for (var i = 0; i < pagedSet.Length; i++)
                {
                    sb.Clear();
                    if (displayTableEmbelishments)
                    {
                        // immediately, and at the top of each page, display the column headers
                        if (i == 0 || i % paging[Page_Size_Entry] == 0)
                        {
                            for (var j = 0; j < cols.Count; j++)
                            {
                                var col = cols[j];
                                if (sb.Length > 0)
                                {
                                    sb.Append(" | ");
                                }

                                var format = $"{{0, -{col.Item3}}}";
                                sb.Append(string.Format(format, col.Item1));
                            }

                            Console.WriteLine(sb.ToString());
                            sb.Clear();
                        }
                    }

                    var di = pagedSet[i];
                    for (var j = 0; j < cols.Count; j++)
                    {
                        var col = cols[j];
                        if (sb.Length > 0)
                        {
                            if (displayTableEmbelishments)
                            {
                                sb.Append(" | ");
                            }
                            else
                            {
                                sb.Append("\t");
                            }
                        }

                        var format = string.Empty;
                        var shortened = string.Empty;
                        switch (col.Item1) // column name
                        {
                            case Column_Name_Location:
                                format = $"{{0, -{col.Item3}}}";
                                shortened = ShortenString(di.Path, col.Item3);
                                sb.Append(string.Format(format, shortened));
                                break;
                            case Column_Name_FullPath:
                                format = $"{{0, -{col.Item3}}}";
                                shortened = ShortenString(di.Path, col.Item3);
                                sb.Append(string.Format(format, shortened));
                                break;
                            case Column_Name_ItemName:
                                format = $"{{0, -{col.Item3}}}";
                                shortened = EnforceLength(di.ItemName, col.Item3);
                                sb.Append(string.Format(format, shortened));
                                break;
                            case Column_Name_Extension:
                                format = $"{{0, -{col.Item3}}}";
                                shortened = EnforceLength(di.Extension, col.Item3);
                                sb.Append(string.Format(format, shortened));
                                break;
                            case Column_Name_IsFile:
                                format = $"{{0, -{col.Item3}}}";
                                sb.Append(string.Format(format, di.IsFile ? "y" : "n"));
                                break;
                            case Column_Name_Size:
                                format = $"{{0, {col.Item3}}}";
                                sb.Append(string.Format(format, ShortenSize(di.SizeInBytes)));
                                break;
                            case Column_Name_LastWritten:
                                format = $"{{0, -{col.Item3}}}";
                                shortened = DateTimeDataHelper.ToString(di.LastWritten);
                                sb.Append(string.Format(format, shortened));
                                break;
                            case Column_Name_LastAccessed:
                                format = $"{{0, -{col.Item3}}}";
                                shortened = DateTimeDataHelper.ToString(di.LastAccessed);
                                sb.Append(string.Format(format, shortened));
                                break;
                            case Column_Name_Creation:
                                format = $"{{0, -{col.Item3}}}";
                                shortened = DateTimeDataHelper.ToString(di.CreationDate);
                                sb.Append(string.Format(format, shortened));
                                break;
                            case Column_Name_Hash:
                                format = $"{{0, -{col.Item3}}}";
                                shortened = EnforceLength(di.FileHash, col.Item3);
                                sb.Append(string.Format(format, shortened));
                                break;
                            case Column_Name_LastHashed:
                                format = $"{{0, -{col.Item3}}}";
                                shortened = DateTimeDataHelper.ToString(di.HashTimestamp);
                                sb.Append(string.Format(format, shortened));
                                break;
                            case Column_Name_Attributes:
                                format = $"{{0, -{col.Item3}}}";
                                shortened = GetAttributeAbbreviation(di.Attributes, AttributeDisplayMethods.Simple);
                                sb.Append(string.Format(format, shortened));
                                break;
                            case Column_Name_Attributes_Extended:
                                format = $"{{0, -{col.Item3}}}";
                                shortened = GetAttributeAbbreviation(di.Attributes, AttributeDisplayMethods.Extended);
                                sb.Append(string.Format(format, shortened));
                                break;
                            case Column_Name_Attributes_Three:
                                format = $"{{0, -{col.Item3}}}";
                                shortened = GetAttributeAbbreviation(di.Attributes, AttributeDisplayMethods.ThreeCharacterSimple);
                                sb.Append(string.Format(format, shortened));
                                break;
                            case Column_Name_Attributes_Extended_Three:
                                format = $"{{0, -{col.Item3}}}";
                                shortened = GetAttributeAbbreviation(di.Attributes, AttributeDisplayMethods.ThreeCharacterExtended);
                                sb.Append(string.Format(format, shortened));
                                break;
                        }
                    }

                    Console.WriteLine(sb.ToString());
                }
            }
        }
    }
}
