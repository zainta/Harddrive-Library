using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HDDL.Scanning
{
    /// <summary>
    /// Represents a disk item record (file or directory)
    /// </summary>
    public class DiskItemRecord
    {
        /// <summary>
        /// The unique identified
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Indicates the containing directory.
        /// Only applies to files.
        /// </summary>
        public Guid? ParentItemId { get; set; }

        /// <summary>
        /// When the item was first scanned
        /// </summary>
        public DateTime FirstScanned { get; set; }

        /// <summary>
        /// When the item was last scanned
        /// </summary>
        public DateTime LastScanned { get; set; }

        /// <summary>
        /// The item's path
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        /// Whether or not the item is a file (or a directory)
        /// </summary>
        public bool IsFile { get; set; }

        /// <summary>
        /// The file size in bytes
        /// </summary>
        public long? SizeInBytes { get; set; }

        /// <summary>
        /// The file's extension
        /// </summary>
        public string Extension { get; set; }
    }
}
