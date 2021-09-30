// Copyright (c) Zain Al-Ahmary.  All rights reserved.
// Licensed under the MIT License, (the "License"); you may not use this file except in compliance with the License. 
// You may obtain a copy of the License at https://mit-license.org/

using HDDL.HDSL;
using HDDL.UI;
using HDDL.IO.Parameters;
using HDDL.Scanning;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using HDDL.Data;
using HDDL.IO.Settings;
using System.Reflection;

namespace HDSL
{
    /// <summary>
    /// For now, this is test code to develop the Harddrive-Library class library
    /// </summary>
    class Program
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

        private const string Column_Name_Location = "Location";
        private const string Column_Name_FullPath = "Path";
        private const string Column_Name_ItemName = "Name";
        private const string Column_Name_Extension = "Ext";
        private const string Column_Name_IsFile = "File?";
        private const string Column_Name_Size = "Size";
        private const string Column_Name_LastWritten = "Write";
        private const string Column_Name_LastAccessed = "Accessed";
        private const string Column_Name_Creation = "Created";

        private const int Default_Page_Row_Count = 32;
        private const int Default_Page_Index = -1;
        private const int Min_Page_Row_Count = 10;
        private const string Page_Size_Entry = "pagesize";
        private const string Page_Index = "pageindex";

        private const string Ini_File_Location = "db location.ini";

        private static bool _embellish;
        private static bool _count;

        static void Main(string[] args)
        {
            var manager = IniFileManager.Explore(Ini_File_Location, true, false, false,
                new IniSubsection("HDSL_DB", null, new IniValue("DatabaseLocation", defaultValue: "file database.db") ));

            ParameterHandler ph = new ParameterHandler();
            ph.AddRules(
                new ParameterRuleOption("columns", true, true, "psc", "-"),
                new ParameterRuleOption("paging", false, true, "-1:-1", "-"),
                new ParameterRuleOption("db", false, true, manager[@"HDSL_DB>DatabaseLocation"].Value, " - "),
                new ParameterRuleOption("scan", true, true, null, "-"),
                new ParameterRuleOption("run", false, true, null, "-"),
                new ParameterRuleOption("exec", false, true, null, "-"),
                new ParameterRuleOption("dm", false, true, "t", "-"),
                new ParameterRuleShortcut("ex"),
                new ParameterRuleFlag(new FlagDefinition[] {
                    new FlagDefinition('e', true, true),
                    new FlagDefinition('c', true, true),
                    new FlagDefinition('s', true, false) }, "-")
                );
            ph.Comb(args);

            var dbPath = ph["db"];
            var scanPaths = ph.GetAllParam("scan").Where(p => !string.IsNullOrWhiteSpace(p)).ToArray();
            var runScript = ph["run"];
            var executeFile = !string.IsNullOrWhiteSpace(ph["exec"]) ? ph["exec"] : ph["ex"];
            _embellish = ph.GetFlag("e");
            _count = ph.GetFlag("c");

            DiskScanEventWrapperDisplayModes displayMode;
            switch (ph.GetParam("dm"))
            {
                case "p":
                    displayMode = DiskScanEventWrapperDisplayModes.ProgressBar;
                    break;
                case "s":
                    displayMode = DiskScanEventWrapperDisplayModes.Spinner;
                    break;
                default:
                case "t":
                    displayMode = DiskScanEventWrapperDisplayModes.Text;
                    break;
                case "q":
                    displayMode = DiskScanEventWrapperDisplayModes.Displayless;
                    break;
            }

            // the -s flag tells the system to overwrite the ini file and update it.
            // (this will use hte value stored in db, so if it is set by option then it will update)
            if (ph.GetFlag("s"))
            {
                manager[@"HDSL_DB>DatabaseLocation"].Value = dbPath;
                manager.WriteFile(Ini_File_Location, Ini_File_Location);
            }

            if (scanPaths.Length > 0)
            {
                var scanWrapper = new DiskScanEventWrapper(dbPath, scanPaths, true, displayMode);
                scanWrapper.Go();
            }

            // Execute a line of code
            if (!string.IsNullOrWhiteSpace(runScript))
            {
                var pagingData = GetPagingData(ph.GetParam("paging", -1));
                var result = HDSLProvider.ExecuteCode(runScript, dbPath);
                DisplayResult(ph.GetParam("columns", -1), pagingData, result);
            }

            // Execute the contents of a code file
            if (!string.IsNullOrWhiteSpace(executeFile))
            {
                var pagingData = GetPagingData(ph.GetParam("paging", -1));
                var result = HDSLProvider.ExecuteScript(executeFile, dbPath);
                DisplayResult(ph.GetParam("columns", -1), pagingData, result);
            }
        }

        #region Column Configuration and Result Display

        /// <summary>
        /// Takes in a column string and returns a list containing the column identifier, the column index, and the column width
        /// </summary>
        /// <param name="columnStr">The encoded parameter information</param>
        /// <returns>The resulting information</returns>
        private static List<Tuple<string, int, int>> GetColumnTable(string columnStr)
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
        private static Dictionary<string, int> GetPagingData(string paging)
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
        private static bool Contains(List<Tuple<string, int, int>> container, string column)
        {
            return (from t in container where t.Item1 == column select t).Any();
        }

        /// <summary>
        /// Displays the appropriate its from the HDSLResult instance
        /// </summary>
        /// <param name="result">The result to process</param>
        /// <param name="paging">The paging data dictionary</param>
        /// <param name="columns">A character encoded column string</param>
        private static void DisplayResult(string columns, Dictionary<string, int> paging, HDSLResult result)
        {
            if (paging == null) return;

            if (result.Errors.Length > 0)
            {
                foreach (var error in result.Errors)
                {
                    Console.WriteLine(error);
                }
            }
            else
            {
                var cols = GetColumnTable(columns);

                DiskItem[] pagedSet = null;
                if (paging[Page_Index] != -1)
                {
                    pagedSet = result.Results.Skip(paging[Page_Index] * paging[Page_Size_Entry]).Take(paging[Page_Size_Entry]).ToArray();
                }
                else
                {
                    pagedSet = result.Results;
                }

                StringBuilder sb = new StringBuilder();
                for (var i = 0; i < pagedSet.Length; i++)
                {
                    sb.Clear();
                    if (_embellish)
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

                                var format = string.Empty;
                                format = $"{{0, -{col.Item3}}}";
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
                            if (_embellish)
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
                                sb.Append(string.Format(format, di.ItemName));
                                break;
                            case Column_Name_Extension:
                                format = $"{{0, -{col.Item3}}}";
                                sb.Append(string.Format(format, di.Extension));
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
                                sb.Append(string.Format(format, di.LastWritten.ToLocalTime()));
                                break;
                            case Column_Name_LastAccessed:
                                format = $"{{0, -{col.Item3}}}";
                                sb.Append(string.Format(format, di.LastAccessed.ToLocalTime()));
                                break;
                            case Column_Name_Creation:
                                format = $"{{0, -{col.Item3}}}";
                                sb.Append(string.Format(format, di.CreationDate.ToLocalTime()));
                                break;
                        }
                    }

                    Console.WriteLine(sb.ToString());
                }

                if (_count)
                {
                    Console.WriteLine();
                    Console.WriteLine($"{result.Results.Length} matches found.");
                }
            }
        }

        /// <summary>
        /// Takes a path and removes folders until it is under the given length
        /// </summary>
        /// <param name="path">The path to shorten</param>
        /// <param name="maxLength">The desired maximum length</param>
        /// <param name="delimiter">The delimiter to shorten baseed on</param>
        /// <returns>The resultant string</returns>
        public static string ShortenString(string path, int maxLength = 30, char delimiter = '\\')
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
        public static string ShortenSize(long? value)
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

        #endregion        
    }
}
