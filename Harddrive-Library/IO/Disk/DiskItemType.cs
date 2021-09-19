// Copyright (c) Zain Al-Ahmary.  All rights reserved.
// Licensed under the MIT License, (the "License"); you may not use this file except in compliance with the License. 
// You may obtain a copy of the License at https://mit-license.org/

using System.IO;

namespace HDDL.IO.Disk
{
    /// <summary>
    /// Contains the path and what the item is
    /// </summary>
    public class DiskItemType
    {
        /// <summary>
        /// The full path
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        /// Whether or not the item is a file
        /// </summary>
        public bool IsFile { get; set; }

        /// <summary>
        /// If IsFile is false, contains the DirectoryInfo
        /// </summary>
        public DirectoryInfo DInfo { get; set; }

        /// <summary>
        /// If IsFile is true, contains the FileInfo
        /// </summary>
        public FileInfo FInfo { get; set; }

        /// <summary>
        /// Create a DiskItemType
        /// </summary>
        /// <param name="path">The full path</param>
        /// <param name="isFile">Whether or not it is a file</param>
        public DiskItemType(string path, bool isFile)
        {
            Path = path;
            IsFile = isFile;

            if (isFile)
            {
                FInfo = new FileInfo(path);
                DInfo = null;
            }
            else
            {
                FInfo = null;
                DInfo = new DirectoryInfo(path);
            }
        }

        /// <summary>
        /// Create a DiskItemType
        /// </summary>
        /// <param name="file">The file info instance</param>
        public DiskItemType(FileInfo file)
        {
            Path = file.FullName;
            IsFile = true;
            FInfo = file;
        }

        /// <summary>
        /// Create a DiskItemType
        /// </summary>
        /// <param name="directory">The directory info instance</param>
        public DiskItemType(DirectoryInfo directory)
        {
            Path = directory.FullName;
            IsFile = false;
            DInfo = directory;
        }

        public override string ToString()
        {
            return $"{Path}";
        }
    }
}
