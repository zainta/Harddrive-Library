// Copyright (c) Zain Al-Ahmary.  All rights reserved.
// Licensed under the MIT License, (the "License"); you may not use this file except in compliance with the License. 
// You may obtain a copy of the License at https://mit-license.org/

using HDDL.Language.HDSL.Results;
using HDDL.IO.Parameters;
using System;
using System.IO;
using System.Linq;
using System.Text;
using HDDL.Language.HDSL;

namespace HDSL.ConsoleClient.Helpers
{
    /// <summary>
    /// Handles the display of results on the console
    /// </summary>
    class ResultDisplayHelper
    {
        /// <summary>
        /// Displays the provided set of outcomes one after another, in sequence
        /// </summary>
        /// <param name="results">The outcome set to display</param>
        /// <param name="ph">The parameter handler to use for flags</param>
        public static void Display(ParameterHandler ph, HDSLOutcomeSet results)
        {
            if (results.Errors.Length > 0)
            {
                foreach (var error in results.Errors)
                {
                    Console.WriteLine(error.ToString());
                }
            }
            else
            {
                var embelish = ph.GetFlag("e");

                foreach (var result in results.Results)
                {
                    if (embelish)
                    {
                        Console.WriteLine();
                        Console.WriteLine();
                        Console.WriteLine($"Results for '{result.Statement}':");
                        Console.WriteLine();
                        Console.WriteLine($"Displaying Page {result.PageIndex + 1} of {(result.TotalRecords / result.RecordsPerPage) + 1} total.  {result.TotalRecords} Records.");
                    }
                    else
                    {
                        Console.WriteLine();
                    }

                    int rowsDisplayed = 0;
                    foreach (var row in result.Records)
                    {
                        if (rowsDisplayed == 0 || (rowsDisplayed % HDSLConstants.Page_Size) == 0)
                        {
                            DisplayRow(result, row, false, embelish);
                        }

                        DisplayRow(result, row, true, embelish);
                        rowsDisplayed++;
                    }
                }
            }
        }

        /// <summary>
        /// Displays a row in the output table
        /// </summary>
        /// <param name="result">The result the row is from</param>
        /// <param name="row">The data row to display</param>
        /// <param name="displayValue">If true, displays the property's value, otherwise displays the column name</param>
        /// <param name="embelish">Whether or not to display embelishments</param>
        private static void DisplayRow(HDSLOutcome result, HDSLRecord row, bool displayValue, bool embelish)
        {
            // headers are not shown for unembelished results
            if (!displayValue && !embelish) return;

            foreach (var column in row.Columns)
            {
                var columnData = result.Columns.Where(c => c.Column == column).Single();
                var columnIndex = Array.IndexOf(result.Columns, columnData);

                if (displayValue)
                {
                    Console.Write(Pad(GetFormattedValue(columnData, row[column]?.Value), columnData.Width));
                }
                else
                {
                    Console.Write(Pad(column, columnData.Width));
                }
                if (columnIndex < result.Columns.Length)
                {
                    if (embelish)
                    {
                        Console.Write(" | ");
                    }
                    else
                    {
                        Console.Write("\t");
                    }
                }
            }
            Console.WriteLine();
        }

        /// <summary>
        /// Attempts to convert an object to the given type
        /// </summary>
        /// <typeparam name="T">The type to convert it to</typeparam>
        /// <param name="value">The value to convert</param>
        /// <exception cref="InvalidCastException" />
        /// <returns></returns>
        private static T GetAs<T>(object value)
        {
            return (T)value;
        }

        #region Formatting

        /// <summary>
        /// Takes a value and pads it to be the given width
        /// </summary>
        /// <param name="value">The value</param>
        /// <param name="width">The target width</param>
        /// <returns></returns>
        private static string Pad(string value, int width)
        {
            if (width == ColumnDefinition.UnrestrictedWidth) return value;

            var result = value;
            if (value.Length < width)
            {
                if (width >= 0)
                {
                    result = value.PadRight(width);
                }
                else if (width < 0)
                {
                    result = value.PadLeft(Math.Abs(width));
                }
            }

            return result;
        }

        /// <summary>
        /// Returns the properly formatted and prepared value as text
        /// </summary>
        /// <param name="cd">The column definition</param>
        /// <param name="value">The value to process</param>
        /// <returns>The resulting string value</returns>
        private static string GetFormattedValue(ColumnDefinition cd, object value)
        {
            var result = string.Empty;
            var type = Type.GetType(cd.Type);
            if (type == typeof(long))
            {
                if (value is int)
                {
                    var v = GetAs<int>(value);
                    switch (cd.Column)
                    {
                        case "Size":
                            result = ShortenSize(v);
                            break;
                    }
                }
                else if (value is long)
                {
                    var v = GetAs<long>(value);
                    switch (cd.Column)
                    {
                        case "Size":
                            result = ShortenSize(v);
                            break;
                    }
                }
            }
            else if (type == typeof(DateTime))
            {
                var v = GetAs<DateTime>(value);
                switch (cd.Column)
                {
                    case "Occurred":
                    case "NextScan":
                    case "FirstScanned":
                    case "LastScanned":
                    case "LastWritten":
                    case "LastAccessed":
                    case "CreationDate":
                    case "HashTimestamp":
                        result = v.ToString();
                        break;
                }
            }
            else if (type == typeof(string))
            {
                var v = GetAs<string>(value);
                switch (cd.Column)
                {
                    case "Path":
                        result = ShortenString(v, cd.Width);
                        break;
                    case "NewFileHash":
                    case "OldFileHash":
                    case "HDSL":
                    case "MachineUNCName":
                    case "FileHash":
                    case "ItemName":
                    case "Extension":
                        result = EnforceLength(v, cd.Width);
                        break;
                }
            }
            else if (type == typeof(bool))
            {
                var v = GetAs<bool>(value);
                switch (cd.Column)
                {
                    case "IsFile":
                    case "InPassiveMode":
                        result = v ? "Yes" : "No";
                        break;
                }
            }
            else if (type == typeof(Guid))
            {
                var v = GetAs<Guid>(value);
                switch (cd.Column)
                {
                    case "Id":
                    case "ParentId":
                        result = v.ToString();
                        break;
                }
            }
            else if (type == typeof(int))
            {
                var v = GetAs<int>(value);
                switch (cd.Column)
                {
                    case "Depth":
                        result = v.ToString();
                        break;
                }
            }
            else if (type == typeof(TimeSpan))
            {
                var v = GetAs<TimeSpan>(value);
                switch (cd.Column)
                {
                    case "Interval":
                        result = v.ToString();
                        break;
                }
            }
            else if (type == typeof(FileAttributes))
            {
                var v = GetAs<FileAttributes>(value);
                switch (cd.Column)
                {
                    case "Attributes":
                        result = GetAttributeAbbreviation(v);
                        break;
                }
            }

            return result;
        }

        /// <summary>
        /// Takes a path and removes folders until it is under the given length
        /// </summary>
        /// <param name="path">The path to shorten</param>
        /// <param name="maxLength">The desired maximum length</param>
        /// <param name="delimiter">The delimiter to shorten baseed on</param>
        /// <returns>The resultant string</returns>
        private static string ShortenString(string path, int maxLength = 30, char delimiter = '\\')
        {
            if (maxLength == ColumnDefinition.UnrestrictedWidth) return path;
            if (path.Length <= maxLength) return path;

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
        private static string ShortenSize(long? value)
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
        private static string EnforceLength(string value, int maxlength = 30)
        {
            if (maxlength == ColumnDefinition.UnrestrictedWidth) return value;

            if (value.Length <= maxlength) return value;
            return $"{value.Substring(0, maxlength - 3)}...";
        }

        /// <summary>
        /// Takes a file attribute value and returns the code letter for each selected attribute in a block
        /// </summary>
        /// <param name="attributes">The attributes to process</param>
        /// <returns>The abbreviated string</returns>
        private static string GetAttributeAbbreviation(FileAttributes attributes)
        {
            var results = new StringBuilder();
            var vals = Enum.GetValues<FileAttributes>();
            foreach (var v in vals)
            {
                if (attributes.HasFlag(v))
                {
                    results.Append(GetLetter(v));
                }
            }

            return results.ToString();
        }

        /// <summary>
        /// Returns the single character abbreviation representing the given attribute
        /// </summary>
        /// <param name="attribute">The attribute to convert</param>
        /// <returns></returns>
        private static char GetLetter(FileAttributes attribute)
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

        #endregion
    }
}
