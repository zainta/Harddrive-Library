using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations.Schema;
using LiteDB;

namespace HDDL.Data
{
    /// <summary>
    /// Represents a disk item record (file or directory)
    /// </summary>
    [Table("DiskItem", Schema = "main")]
    public class DiskItem : BsonHDDLRecordBase
    {
        /// <summary>
        /// Indicates the containing directory.
        /// Only applies to files.
        /// </summary>
        public Guid? ParentId { get; set; }

        /// <summary>
        /// The containing directory instance
        /// </summary>
        public DiskItem Parent { get; set; }

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
        /// The name of the item
        /// </summary>
        public string ItemName { get; set; }

        /// <summary>
        /// Whether or not the item is a file (or a directory)
        /// </summary>
        public bool IsFile { get; set; }

        /// <summary>
        /// The file's extension
        /// </summary>
        public string Extension { get; set; }

        /// <summary>
        /// The file size in bytes
        /// </summary>
        public long? SizeInBytes { get; set; }

        /// <summary>
        /// When the item was last updated
        /// </summary>
        public DateTime LastWritten { get; set; }

        /// <summary>
        /// When the item was last accessed
        /// </summary>
        public DateTime LastAccessed { get; set; }

        /// <summary>
        /// When the file was created
        /// </summary>
        public DateTime CreationDate { get; set; }

        /// <summary>
        /// Creates an instance from the current record in the data reader
        /// </summary>
        /// <param name="record"></param>
        public DiskItem(BsonValue record) : base(record)
        {
        }

        /// <summary>
        /// Default constructor
        /// </summary>
        public DiskItem() : base()
        {

        }

        public override string ToString()
        {
            if (IsFile)
            {
                return $"({SizeInBytes}) '{Path}'";
            }
            else
            {
                return Path;
            }
        }

        /// <summary>
        /// Overwrites the method instance with the given instance's values 
        /// (excluding id, parentid, firstscanned, itemname, extension, isfile, creationdate, and path)
        /// </summary>
        /// <param name="item">The item to copy from</param>
        public void CopyFrom(DiskItem item)
        {
            LastScanned = item.LastScanned;
            SizeInBytes = item.SizeInBytes;
            LastWritten = item.LastWritten;
            LastAccessed = item.LastAccessed;
        }
    }
}
