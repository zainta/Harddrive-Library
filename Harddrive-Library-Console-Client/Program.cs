using HDDL.HDSL;
using HDDL.IO.Display;
using HDDL.IO.Parameters;
using HDDL.Scanning;
using System;
using System.IO;

namespace HDSL
{
    /// <summary>
    /// For now, this is test code to develop the Harddrive-Library class library
    /// </summary>
    class Program
    {
        private static ProgressBar _progress;

        static void Main(string[] args)
        {
            ParameterHandler ph = new ParameterHandler();
            ph.AddRules(
                new ParameterRuleOption("db", true, 1, "files database.db", " - "),
                new ParameterRuleOption("scan", true, 1, null, "-"),
                new ParameterRuleOption("run", true, 1, null, "-"),
                new ParameterRuleOption("exec", true, 1, null, "-")
                );
            ph.Comb(args);

            var dbPath = ph.GetParam("db");
            var scanPath = ph.GetParam("scan");
            var runScript = ph.GetParam("run");
            var executeFile = ph.GetParam("exec");

            // Do the scan first
            if (!string.IsNullOrWhiteSpace(scanPath))
            {
                Console.Write($"Performing scan on '{scanPath}' ");
                var scanner = new DiskScan(dbPath, true, scanPath);

                scanner.ScanStarted += Scanner_ScanStarted;
                scanner.ScanEventOccurred += Scanner_ScanEventOccurred;
                scanner.StatusEventOccurred += Scanner_StatusEventOccurred;
                scanner.StartScan();

                while (scanner.Status == ScanStatus.InitiatingScan ||
                    scanner.Status == ScanStatus.Scanning ||
                    scanner.Status == ScanStatus.Deleting)
                {
                    if (scanner.Status == ScanStatus.Scanning)
                    {
                        _progress.Display();
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
                Console.WriteLine($"-- Done! {new String(' ', _progress.Width - 7)}");
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
                _progress.Value++;
            }
            else if (evnt.Nature == ScanEventType.Update)
            {
                //_progress.Message = $"Updated entry for {itemType} @ '{evnt.Path}'.";
                _progress.Value++;
            }
            else if (evnt.Nature == ScanEventType.Delete)
            {
                //_progress.Message = $"Deleted entry for {itemType} @ '{evnt.Path}'.";
            }
            else if (evnt.Nature == ScanEventType.DatabaseError)
            {
                //_progress.Message = $"!!Database Error!! {evnt.Error.Message}";
                _progress.Value++;
            }
        }
    }
}
