using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations.Schema;
using LiteDB;

namespace HDDL.Data
{
    /// <summary>
    /// Represents a bookmark item record, a reference to a file(s) or directory
    /// </summary>
    [Table("Bookmark", Schema = "main")]
    public class BookmarkItem : BsonHDDLRecordBase
    {
        /// <summary>
        /// The item's target directory path
        /// </summary>
        public string Target { get; set; }

        /// <summary>
        /// The name of the item
        /// </summary>
        public string ItemName { get; set; }

        /// <summary>
        /// Creates an instance from the current record in the data reader
        /// </summary>
        /// <param name="record"></param>
        public BookmarkItem(BsonValue record) : base(record)
        {
        }

        /// <summary>
        /// Default constructor
        /// </summary>
        public BookmarkItem() : base()
        {
        }

        public override string ToString()
        {
            return $"[{ItemName}:{Target}]";
        }
    }
}
