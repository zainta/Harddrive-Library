using HDDL.HDSL;
using HDDL.UI;
using HDDL.IO.Parameters;
using HDDL.Scanning;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

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
        private const int Column_Default_Width_Size = 6;
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

        private static ProgressBar _progress;
        private static bool _showProgress;
        private static bool _verbose;

        static void Main(string[] args)
        {
            ParameterHandler ph = new ParameterHandler();
            ph.AddRules(
                new ParameterRuleOption("db", false, true, "files database.db", " - "),
                new ParameterRuleOption("scan", true, true, null, "-"),
                new ParameterRuleOption("run", false, true, null, "-"),
                new ParameterRuleOption("exec", false, true, null, "-"),
                new ParameterRuleOption("columns", true, true, "psc", "-"),
                new ParameterRuleFlag(new FlagDefinition[] { new FlagDefinition('p', true, false), new FlagDefinition('v', true, true) }, "-")
                );
            ph.Comb(args);

            var dbPath = ph.GetParam("db");
            var scanPaths = ph.GetAllParam("scan");
            var runScript = ph.GetParam("run");
            var executeFile = ph.GetParam("exec");
            _showProgress = ph.GetFlag("p");
            _verbose = ph.GetFlag("v");

            // Do the scan first
            var paths = (from p in scanPaths where !string.IsNullOrWhiteSpace(p) select p).ToArray();
            if (paths.Length > 0)
            {
                if (_showProgress)
                {
                    Console.Write($"Performing scans on '{string.Join("\', \'", paths)}\': ");
                }
                else if (_verbose)
                {
                    Console.WriteLine($"Performing scans on '{string.Join("\', \'", paths)}\'... ");
                }
                var scanner = new DiskScan(dbPath, false, paths);

                if (_showProgress)
                {
                    scanner.ScanStarted += Scanner_ScanStarted;
                }
                scanner.ScanEventOccurred += Scanner_ScanEventOccurred;
                scanner.StatusEventOccurred += Scanner_StatusEventOccurred;
                scanner.ScanEnded += Scanner_ScanEnded;
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
                var result = HDSLProvider.ExecuteCode(runScript, dbPath);
                DisplayResult(ph.GetParam("columns", -1), result);
            }

            // Execute the contents of a code file
            if (!string.IsNullOrWhiteSpace(executeFile))
            {
                var result = HDSLProvider.ExecuteScript(executeFile, dbPath);
                DisplayResult(ph.GetParam("columns", -1), result);
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
        /// <param name="columns">A character encoded column string</param>
        private static void DisplayResult(string columns, HDSLResult result)
        {
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
                StringBuilder sb = new StringBuilder();
                for (var i = 0; i < result.Results.Length; i++)
                {
                    sb.Clear();
                    // immediately, and every 32 lines, display the column headers
                    if (i == 0 || i % 32 == 0)
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

                    var di = result.Results[i];
                    for (var j = 0; j < cols.Count; j++)
                    {
                        var col = cols[j];
                        if (sb.Length > 0)
                        {
                            sb.Append(" | ");
                        }

                        var format = string.Empty;
                        StringBuilder buffer = new StringBuilder(col.Item3);
                        switch (col.Item1) // column name
                        {
                            case Column_Name_Location:
                                format = $"{{0, -{col.Item3}}}";
                                GetShortPathName(GetLocation(di), buffer, (uint)buffer.Capacity);
                                sb.Append(string.Format(format,  buffer.ToString()));
                                break;
                            case Column_Name_FullPath:
                                format = $"{{0, -{col.Item3}}}";
                                GetShortPathName(di.Path, buffer, (uint)buffer.Capacity);
                                sb.Append(string.Format(format, buffer.ToString()));
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
                                format = $"{{0, -{col.Item3}}}";
                                sb.Append(string.Format(format, di.SizeInBytes));
                                break;
                            case Column_Name_LastWritten:
                                format = $"{{0, {col.Item3}}}";
                                sb.Append(string.Format(format, di.LastWritten.ToLocalTime()));
                                break;
                            case Column_Name_LastAccessed:
                                format = $"{{0, {col.Item3}}}";
                                sb.Append(string.Format(format, di.LastAccessed.ToLocalTime()));
                                break;
                            case Column_Name_Creation:
                                format = $"{{0, {col.Item3}}}";
                                sb.Append(string.Format(format, di.CreationDate.ToLocalTime()));
                                break;
                        }
                    }

                    Console.WriteLine(sb.ToString());
                }
            }
        }

        /// <summary>
        /// Uses the appropriate info object to extract the target's location
        /// </summary>
        /// <param name="rec">The target record</param>
        /// <returns></returns>
        private static string GetLocation(HDDL.Data.DiskItem rec)
        {
            string location;
            if (rec.IsFile)
            {
                var fi = new FileInfo(rec.Path);
                location = fi.Directory.FullName;
            }
            else
            {
                var di = new DirectoryInfo(rec.Path);
                location = di.Parent.FullName;
            }
            return location;
        }

        #endregion

        #region Events

        private static void Scanner_ScanStarted(DiskScan scanner, int directoryCount, int fileCount)
        {
            _progress = new ProgressBar(-1, -1, 60, 0, 0, fileCount + directoryCount);
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

            if (evnt.Nature == ScanEventType.AddAttempted ||
                evnt.Nature == ScanEventType.UpdateAttempted ||
                evnt.Nature == ScanEventType.DeleteAttempted ||
                evnt.Nature == ScanEventType.KeyNotDeleted ||
                evnt.Nature == ScanEventType.UnknownError)
            {
                // We aren't interested in these right now.  We'll implement this later.
            }
            else if (evnt.Nature == ScanEventType.AddRequired)
            {
                //_progress.Message = $"Discovered {itemType} @ '{evnt.Path}'.";
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
                //_progress.Message = $"Updated entry for {itemType} @ '{evnt.Path}'.";
                if (_showProgress)
                {
                    _progress.Value++;
                }
                else if (_verbose)
                {
                    Console.WriteLine($"Updated entry for {itemType} @ '{evnt.Path}'.");
                }
            }
            else if (evnt.Nature == ScanEventType.Delete)
            {
                //_progress.Message = $"Deleted entry for {itemType} @ '{evnt.Path}'.";
                if (!_showProgress && _verbose)
                {
                    Console.WriteLine($"Deleted entry for {itemType} @ '{evnt.Path}'.");
                }
            }
            else if (evnt.Nature == ScanEventType.DatabaseError)
            {
                //_progress.Message = $"!!Database Error!! {evnt.Error.Message}";
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

        #endregion

        #region Externals

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        static extern uint GetShortPathName([MarshalAs(UnmanagedType.LPTStr)]string lpszLongPath, [MarshalAs(UnmanagedType.LPTStr)]StringBuilder lpszShortPath, uint cchBuffer);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        static extern uint GetShortPathName(string lpszLongPath, char[] lpszShortPath, int cchBuffer);

        #endregion
    }
}
