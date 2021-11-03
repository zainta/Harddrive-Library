// Copyright (c) Zain Al-Ahmary.  All rights reserved.
// Licensed under the MIT License, (the "License"); you may not use this file except in compliance with the License. 
// You may obtain a copy of the License at https://mit-license.org/

using System;
using System.Data;
using System.Data.SQLite;
using System.IO;

namespace HDDL.Data
{
    /// <summary>
    /// Represents a disk item record (file or directory)
    /// </summary>
    public class DiskItem : HDDLRecordBase
    {
        /// <summary>
        /// Indicates the containing directory.
        /// Only applies to files.
        /// </summary>
        public Guid? ParentId { get; set; }

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
        public long SizeInBytes { get; set; }

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
        /// The disk item's distance from the root (0 if it is a root)
        /// </summary>
        public int Depth { get; set; }

        /// <summary>
        /// A calculated hash
        /// </summary>
        public string FileHash { get; set; }

        /// <summary>
        /// When the calculated hash was generated
        /// </summary>
        public DateTime HashTimestamp { get; set; }

        /// <summary>
        /// The file's attribute bit-field
        /// </summary>
        public FileAttributes Attributes { get; set; }

        /// <summary>
        /// The UNC name of the machine where the disk item resides
        /// </summary>
        public string MachineUNCName { get; set; }

        /// <summary>
        /// Creates an instance from the current record in the data reader
        /// </summary>
        /// <param name="row"></param>
        public DiskItem(SQLiteDataReader row) : base(row)
        {
            if (!string.IsNullOrWhiteSpace(row.GetString("parentId")))
            {
                ParentId = row.GetGuid("parentId");
            }
            else
            {
                ParentId = null;
            }

            FirstScanned = DateTimeDataHelper.ConvertToDateTime(row.GetString("firstScanned"));
            LastScanned = DateTimeDataHelper.ConvertToDateTime(row.GetString("lastScanned"));
            Path = row.GetString("path");
            ItemName = row.GetString("itemName");
            IsFile = row.GetBoolean("isFile");
            Extension = row.GetString("extension");
            SizeInBytes = row.GetInt64("size");
            LastWritten = DateTimeDataHelper.ConvertToDateTime(row.GetString("lastWritten"));
            LastAccessed = DateTimeDataHelper.ConvertToDateTime(row.GetString("lastAccessed"));
            CreationDate = DateTimeDataHelper.ConvertToDateTime(row.GetString("created"));
            Depth = row.GetInt32("depth");
            FileHash = row.GetString("hash");
            HashTimestamp = DateTimeDataHelper.ConvertToDateTime(row.GetString("lastHashed"));
            Attributes = row["attributes"] is DBNull ? 0 : (FileAttributes)Enum.ToObject(typeof(FileAttributes), row.GetInt32("attributes"));
            MachineUNCName = row.GetString("unc");
        }

        /// <summary>
        /// Default constructor
        /// </summary>
        public DiskItem() : base()
        {

        }

        /// <summary>
        /// Generates and returns a SQLite Insert statement for this record
        /// </summary>
        /// <returns>The line of SQL</returns>
        public override string ToInsertStatement()
        {
            return $@"insert into diskitems 
                        (id, parentId, firstScanned, lastScanned, path, itemName, isFile, extension, size, lastWritten, lastAccessed, created, depth, hash, lastHashed, attributes, unc) 
                      values 
                        ('{Id}', 
                         '{ParentId}', 
                         '{DateTimeDataHelper.ConvertToString(FirstScanned)}', 
                         '{DateTimeDataHelper.ConvertToString(LastScanned)}', 
                         '{DataHelper.Sanitize(Path)}', 
                         '{DataHelper.Sanitize(ItemName)}', 
                         {IsFile}, 
                         '{DataHelper.Sanitize(Extension)}',
                         {SizeInBytes}, 
                         '{DateTimeDataHelper.ConvertToString(LastWritten)}', 
                         '{DateTimeDataHelper.ConvertToString(LastAccessed)}', 
                         '{DateTimeDataHelper.ConvertToString(CreationDate)}', 
                         {Depth},
                         '{FileHash}',
                         '{DateTimeDataHelper.ConvertToString(HashTimestamp)}',
                         {Convert.ToInt32(Attributes)},
                         '{DataHelper.Sanitize(MachineUNCName)}');";
        }

        /// <summary>
        /// Generates and returns a SQLite Update statement for this record
        /// </summary>
        /// <returns>The line of SQL</returns>
        public override string ToUpdateStatement()
        {
            return $@"update diskitems 
                      set 
                        parentId = '{ParentId}', 
                        firstScanned = '{DateTimeDataHelper.ConvertToString(FirstScanned)}', 
                        lastScanned = '{DateTimeDataHelper.ConvertToString(LastScanned)}', 
                        itemName = '{DataHelper.Sanitize(ItemName)}', 
                        isFile = {IsFile}, 
                        extension = '{DataHelper.Sanitize(Extension)}', 
                        size = {SizeInBytes}, 
                        lastWritten = '{DateTimeDataHelper.ConvertToString(LastWritten)}', 
                        lastAccessed = '{DateTimeDataHelper.ConvertToString(LastAccessed)}', 
                        created = '{DateTimeDataHelper.ConvertToString(CreationDate)}', 
                        depth = {Depth},
                        attributes = {Convert.ToInt32(Attributes)},
                        unc = '{DataHelper.Sanitize(MachineUNCName)}'
                      where 
                        path = '{DataHelper.Sanitize(Path)}';";
        }

        /// <summary>
        /// Generates and returns a SQLite Update statement to this record's integrity hash related fields
        /// </summary>
        /// <returns>The line of SQL</returns>
        public string ToHashUpdateStatement()
        {
            return $@"update diskitems 
                      set 
                        hash = '{FileHash}',
                        lastHashed = '{DateTimeDataHelper.ConvertToString(HashTimestamp)}'
                      where 
                        path = '{DataHelper.Sanitize(Path)}';";
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
        /// Generates and returns a new hash entry for a hash update
        /// </summary>
        /// <param name="newHash">The new hash</param>
        /// <returns></returns>
        public DiskItemHashLogItem GetHashLogEntry(string newHash)
        {
            return new DiskItemHashLogItem()
            {
                Id = Guid.NewGuid(),
                ParentId = Id,
                Path = Path,
                Occurred = DateTime.UtcNow,
                OldFileHash = FileHash,
                NewFileHash = newHash,
                MachineUNCName = MachineUNCName
            };

        }

        /// <summary>
        /// Assigns a value to the Depth property and returns the instance for further use
        /// </summary>
        /// <param name="depth">The distance from the root (0 if it is a root)</param>
        /// <returns></returns>
        public DiskItem SetDepth(int depth)
        {
            Depth = depth;
            return this;
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
