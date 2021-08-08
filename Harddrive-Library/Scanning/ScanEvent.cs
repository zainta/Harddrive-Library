using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HDDL.Scanning
{
    /// <summary>
    /// Generated when a file or directory is scanned during a scan operation
    /// </summary>
    public class ScanEvent
    {
        /// <summary>
        /// The nature of the event
        /// </summary>
        public ScanEventType Nature { get; private set; }

        /// <summary>
        /// The path of the scanned item
        /// </summary>
        public string Path { get; private set; }

        /// <summary>
        /// If the scanned item is a file (or directory)
        /// </summary>
        public bool IsFile { get; private set; }

        /// <summary>
        /// Will contain the associated exception if the type is an error
        /// </summary>
        public Exception Error { get; private set; }

        /// <summary>
        /// Creates a ScanEvent instance
        /// </summary>
        /// <param name="nature">The nature of the event</param>
        /// <param name="path">The path of the scanned item</param>
        /// <param name="isFile">If the scanned item is a file (or directory)</param>
        /// <param name="error">The relevant error</param>
        public ScanEvent(ScanEventType nature, string path, bool isFile, Exception error = null)
        {
            Nature = nature;
            Path = path;
            IsFile = isFile;
            Error = error;
        }
    }
}
