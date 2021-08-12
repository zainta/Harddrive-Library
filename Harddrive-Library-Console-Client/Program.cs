using HDDL.Scanning;
using System;

namespace HDSL
{
    /// <summary>
    /// For now, this is test code to develop the Harddrive-Library class library
    /// </summary>
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length == 0) return;
            var path = args[0];
            var dbPath = args.Length > 1 ? args[0] : "files database.db";

            Console.WriteLine(string.Format("Starting Scan on {0}", path));
            var scanner = new DiskScan(dbPath, path);
            scanner.StartScan();
            scanner.ScanEventOccurred += Scanner_ScanEventOccurred;
            while (scanner.Status == ScanStatus.Scanning)
            {

            }
        }

        private static void Scanner_ScanEventOccurred(DiskScan scanner, ScanEvent evnt)
        {
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
                Console.WriteLine(string.Format("Discovered a {0} at '{1}'.",
                    evnt.IsFile ? "file" : "directory",
                    evnt.Path));
            }
            else if (evnt.Nature == ScanEventType.Update)
            {
                Console.WriteLine(string.Format("Updated a {0} found at '{1}'.",
                    evnt.IsFile ? "file" : "directory",
                    evnt.Path));
            }
            else if (evnt.Nature == ScanEventType.Delete)
            {
                Console.WriteLine(string.Format("Deleted entry for {0} once at '{1}'.",
                    evnt.IsFile ? "file" : "directory",
                    evnt.Path));
            }
            else if (evnt.Nature == ScanEventType.DatabaseError)
            {
                Console.WriteLine(string.Format("!!Database Error!! {0}",
                    evnt.Error.Message));
            }
        }
    }
}
