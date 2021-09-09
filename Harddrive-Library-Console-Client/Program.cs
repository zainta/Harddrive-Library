using HDDL.HDSL;
using HDDL.UI;
using HDDL.IO.Parameters;
using HDDL.IO.Disk;
using HDDL.Scanning;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using HDDL.Data;

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

        private static ProgressBar _progress;
        private static bool _showProgress;
        private static bool _verbose;
        private static bool _embellish;

        static void Main(string[] args)
        {
            ParameterHandler ph = new ParameterHandler();
            ph.AddRules(
                new ParameterRuleOption("columns", true, true, "psc", "-"),
                new ParameterRuleOption("paging", false, true, "-1:-1", "-"),
                new ParameterRuleOption("db", false, true, "files database.db", " - "),
                new ParameterRuleOption("scan", true, true, null, "-"),
                new ParameterRuleOption("run", false, true, null, "-"),
                new ParameterRuleOption("exec", false, true, null, "-"),
                new ParameterRuleFlag(new FlagDefinition[] {
                    new FlagDefinition('e', true, true),
                    new FlagDefinition('p', true, false), 
                    new FlagDefinition('v', true, true) }, "-")
                );
            ph.Comb(args);

            var dbPath = ph.GetParam("db");
            var scanPaths = ph.GetAllParam("scan");
            var runScript = ph.GetParam("run");
            var executeFile = ph.GetParam("exec");
            _showProgress = ph.GetFlag("p");
            _verbose = ph.GetFlag("v");
            _embellish = ph.GetFlag("e");
            var recreate = false;

            // Do the scan first
            var paths = (from p in scanPaths where !string.IsNullOrWhiteSpace(p) select p).ToArray();
            if (paths.Length > 0)
            {
                if (File.Exists(dbPath))
                {
                    // If the file exists, the scan will be significantly slower.
                    // Ask the user if they would like to recreate the database, rather than update it.
                    Console.Write($"Database '{dbPath}' already exists.  This will significantly slow the operation.\nWould you like to recreate the database? (y/n/c): ");
                    ConsoleKeyInfo k;
                    do
                    {
                        k = Console.ReadKey();
                    } while (
                        char.ToLower(k.KeyChar) != 'y' &&
                        char.ToLower(k.KeyChar) != 'n' &&
                        char.ToLower(k.KeyChar) != 'c');
                    if (char.ToLower(k.KeyChar) == 'c')
                    {
                        Environment.Exit(0);
                    }
                    else if (char.ToLower(k.KeyChar) == 'y')
                    {
                        recreate = true;
                    }
                    else if (char.ToLower(k.KeyChar) == 'n')
                    {
                        recreate = false;
                    }
                    Console.WriteLine();
                }

                var scanner = new DiskScan(dbPath, paths);
                scanner.DatabaseResetRequested += Scanner_DatabaseResetRequested;
                scanner.InitializeDatabase(recreate);

                if (recreate)
                {
                    Console.WriteLine("Done.");
                }
                if (_showProgress)
                {
                    Console.Write($"Performing scans on '{string.Join("\', \'", paths)}\': ");
                }
                else if (_verbose)
                {
                    Console.WriteLine($"Performing scans on '{string.Join("\', \'", paths)}\'... ");
                }

                if (_showProgress)
                {
                    scanner.ScanStarted += Scanner_ScanStarted;
                }
                scanner.ScanEventOccurred += Scanner_ScanEventOccurred;
                scanner.StatusEventOccurred += Scanner_StatusEventOccurred;
                scanner.ScanEnded += Scanner_ScanEnded;
                scanner.ScanInsertsCompleted += Scanner_ScanInsertsCompleted;
                scanner.StartScan();

                while (scanner.Status == ScanStatus.InitiatingScan ||
                    scanner.Status == ScanStatus.Scanning ||
                    scanner.Status == ScanStatus.Deleting)
                {
                    if (scanner.Status == ScanStatus.Scanning)
                    {
                        if (_showProgress)
                        {
                            _progress.Display();
                        }
                    }

                    System.Threading.Thread.Sleep(100);
                }
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
                                sb.Append(string.Format(format,  shortened));
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
                var denomination = 1024;
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

        #region Events

        private static void Scanner_ScanStarted(DiskScan scanner, int directoryCount, int fileCount)
        {
            _progress = new ProgressBar(-1, -1, 60, 0, 0, (fileCount + directoryCount) * 2);
        }

        private static void Scanner_StatusEventOccurred(DiskScan scanner, ScanStatus newStatus, ScanStatus oldStatus)
        {
            if (oldStatus == ScanStatus.Scanning)
            {
                
            }
        }

        private static void Scanner_ScanEnded(DiskScan scanner, int totalDeleted, Timings elapsed, ScanOperationOutcome outcome)
        {
            if (_showProgress)
            {
                Console.WriteLine($"-- Done! {new String(' ', _progress.Width - 7)}");
            }
            else if (_verbose)
            {
                Console.WriteLine(string.Format("Done -- Total time: {0}", elapsed.GetScanDuration()));
            }
        }

        private static void Scanner_ScanEventOccurred(DiskScan scanner, ScanEvent evnt)
        {
            var itemType = evnt.IsFile ? "file" : "directory";

            if (evnt.Nature == ScanEventType.DeleteAttempted ||
                evnt.Nature == ScanEventType.KeyNotDeleted ||
                evnt.Nature == ScanEventType.UnknownError)
            {
                // We aren't interested in these right now.  We'll implement this later.
            }
            else if (evnt.Nature == ScanEventType.AddRequired)
            {
                if (_showProgress)
                {
                    _progress.Value++;
                }
                else if (_verbose)
                {
                    Console.WriteLine($"Discovered {itemType} @ '{evnt.Path}'.");
                }
            }
            else if (evnt.Nature == ScanEventType.UpdateRequired)
            {
                if (_showProgress)
                {
                    _progress.Value++;
                }
                else if (_verbose)
                {
                    Console.WriteLine($"Rediscovered {itemType} @ '{evnt.Path}'.");
                }
            }
            else if (evnt.Nature == ScanEventType.Update)
            {
                if (_showProgress)
                {
                    _progress.Value++;
                }
                else if (_verbose)
                {
                    Console.WriteLine($"Updated {itemType} @ '{evnt.Path}'.");
                }
            }
            else if (evnt.Nature == ScanEventType.Delete)
            {
                if (!_showProgress && _verbose)
                {
                    Console.WriteLine($"Deleted entry for {itemType} @ '{evnt.Path}'.");
                }
            }
            else if (evnt.Nature == ScanEventType.DatabaseError)
            {
                if (_showProgress)
                {
                    _progress.Value++;
                }
                else if (_verbose)
                {
                    Console.WriteLine($"!!Database Error!! {evnt.Error.Message}");
                }
            }
        }

        private static void Scanner_ScanInsertsCompleted(DiskScan scanner, int additions)
        {
            if (_showProgress)
            {
                _progress.Value += additions;
            }
            else if (_verbose)
            {
                Console.WriteLine($"Successfully added {additions} records to the database.");
            }
        }

        private static void Scanner_DatabaseResetRequested(DiskScan scanner)
        {
            if (_verbose)
            {
                Console.Write($"Resetting database...");
            }
        }

        #endregion
    }
}
