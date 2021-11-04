// Copyright (c) Zain Al-Ahmary.  All rights reserved.
// Licensed under the MIT License, (the "License"); you may not use this file except in compliance with the License. 
// You may obtain a copy of the License at https://mit-license.org/

using System.Collections.Generic;

namespace HDDL.IO.Settings
{
    /// <summary>
    /// Describes an ini file manager implementation
    /// </summary>
    public interface IInitializationManager
    {
        /// <summary>
        /// Whether or not to use the full path or the name of the file as its key
        /// </summary>
        public bool UseFullPathAsKey { get; }

        /// <summary>
        /// Uses a pathing string and file key to search for a specific node or set of nodes, and returns them
        /// </summary>
        /// <param name="path">A pathing string to a node or group of nodes</param>
        /// <returns></returns>
        public IniValue this[string path] { get; }

        #region Content Management

        /// <summary>
        /// Adds the item to the given ini file.  Creates it if the file doesn't exist
        /// </summary>
        /// <param name="item">The item to add</param>
        /// <param name="iniContent">The file's key</param>
        public void Add(string iniContent, IniItemBase item);

        /// <summary>
        /// Removes the item from the given ini file.
        /// </summary>
        /// <param name="item">The item to remove</param>
        /// <param name="iniContent">The file's key</param>
        public void Remove(string iniContent, IniItemBase item);

        /// <summary>
        /// Resets the ini file's contents to be only those found in items, removing any preexisting content
        /// </summary>
        /// <param name="items">The new items</param>
        /// <param name="iniContent">The file's key</param>
        public void Set(string iniContent, IEnumerable<IniItemBase> items);

        #endregion

        #region Searching

        /// <summary>
        /// Searches the ini file manager for the given path
        /// </summary>
        /// <param name="path">The path to search for</param>
        /// <returns>Null, or the resulting item</returns>
        public IniItemBase Search(string path);

        #endregion

        #region I/O

        /// <summary>
        /// Loads an ini file, using default values and minimal structure from a provided schema, always overwrites existing entries
        /// </summary>
        /// <param name="iniFilePath">The path to load</param>
        /// <param name="additive">If true, items discovererd in the file will be added to the schema if missing</param>
        /// <param name="throwMismatchExceptions">If true, will throw a SchemaMismatchException if the loaded file and the schema conflict</param>
        /// <param name="schema">The expected minimal structure of the ini file.  Missing items will use their default values</param>
        public void Fill(string iniFilePath, bool additive, bool throwMismatchExceptions, params IniItemBase[] schema);

        /// <summary>
        /// Loads an ini file from disk and adds it to the Files dictionary
        /// </summary>
        /// <param name="iniFilePath">The path to load</param>
        /// <param name="overwriteExisting">Whether or not to overwrite an existing file entry</param>
        public void Load(string iniFilePath, bool overwriteExisting);

        /// <summary>
        /// Writes the indicated file to the given path
        /// </summary>
        /// <param name="iniFile">The file to write (the key to the specific loaded files)</param>
        /// <param name="path">The path to write the file to</param>
        public void WriteFile(string iniFile, string path);

        #endregion
    }
}
