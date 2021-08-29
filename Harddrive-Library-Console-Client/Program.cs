using HDDL.HDSL;
using HDDL.IO.Display;
using HDDL.IO.Parameters;
using HDDL.Scanning;
using System;
using System.IO;
using System.Linq;

namespace HDSL
{
    /// <summary>
    /// For now, this is test code to develop the Harddrive-Library class library
    /// </summary>
    class Program
    {
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
                DisplayResult(result);
            }

            // Execute the contents of a code file
            if (!string.IsNullOrWhiteSpace(executeFile))
            {
                var result = HDSLProvider.ExecuteScript(runScript, dbPath);
                DisplayResult(result);
            }
        }

        /// <summary>
        /// Displays the appropriate its from the HDSLResult instance
        /// </summary>
        /// <param name="result">The result to process</param>
        private static void DisplayResult(HDSLResult result)
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
                foreach (var path in result.Paths)
                {
                    Console.WriteLine(path);
                }
            }
        }

        private static void Scanner_ScanStarted(DiskScan scanner, int directoryCount, int fileCount)
        {
            _progress = new ProgressBar(-1, -1, 60, 0, 0, fileCount + directoryCount);
        }

        private static void Scanner_StatusEventOccurred(DiskScan scanner, ScanStatus newStatus, ScanStatus oldStatus)
        {
            if (oldStatus == ScanStatus.Scanning)
            {
                if (_showProgress)
                {
                    Console.WriteLine($"-- Done! {new String(' ', _progress.Width - 7)}");
                }
                else if (_verbose)
                {
                    Console.WriteLine("-- Done!");
                }
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
            else if (evnt.Nature == ScanEventType.Add)
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
            else if (evnt.Nature == ScanEventType.Update)
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
    }
}
