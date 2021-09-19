// Copyright (c) Zain Al-Ahmary.  All rights reserved.
// Licensed under the MIT License, (the "License"); you may not use this file except in compliance with the License. 
// You may obtain a copy of the License at https://mit-license.org/

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

namespace HDDL.IO.Settings
{
    /// <summary>
    /// A simple ini file reader / writer
    /// </summary>
    public class IniFileManager
    {
        internal const char File_Content_Designation = ':';
        internal const char SubSection_Content_Designation = '>';
        private const string Fill_Temp_Schema_Dict_Key = "schema";

        private Dictionary<string, List<IniItemBase>> _files;
        /// <summary>
        /// A dictionary of loaded ini files, where the key is the name of the file
        /// </summary>
        public IReadOnlyDictionary<string, ReadOnlyCollection<IniItemBase>> Files
        {
            get
            {
                return _files.ToDictionary(pair => pair.Key, pair => pair.Value.AsReadOnly());
            }
        }

        /// <summary>
        /// Whether or not to use the full path or the name of the file as its key
        /// </summary>
        public bool UseFullPathAsKey { get; private set; }

        /// <summary>
        /// Uses a pathing string and file key to search for a specific node or set of nodes, and returns them
        /// </summary>
        /// <param name="path">A pathing string to a node or group of nodes</param>
        /// <returns></returns>
        public IniValue this[string path]
        {
            get
            {
                return GetByPath(path) as IniValue;
            }
        }

        /// <summary>
        /// Creates an IniFileManager.  Use the static methods to digest files
        /// </summary>
        /// <param name="fullPathAsKey">Whether or not to use the full path or the name of the file as its key</param>
        private IniFileManager(bool fullPathAsKey = false)
        {
            UseFullPathAsKey = fullPathAsKey;
            _files = new Dictionary<string, List<IniItemBase>>();
        }

        #region Content Management

        /// <summary>
        /// Adds the item to the given ini file.  Creates it if the file doesn't exist
        /// </summary>
        /// <param name="item">The item to add</param>
        /// <param name="iniContent">The file's key</param>
        public void Add(string iniContent, IniItemBase item)
        {
            if (_files.ContainsKey(iniContent))
            {
                var anyDupes = (from f in _files[iniContent] where f.Label == item.Label select f).Any();
                if (!anyDupes)
                {
                    item.RootKey = iniContent;
                    _files[iniContent].Add(item);
                }
            }
            else
            {
                _files.Add(iniContent, new List<IniItemBase>() { item });
                item.RootKey = iniContent;
            }
        }

        /// <summary>
        /// Removes the item from the given ini file.
        /// </summary>
        /// <param name="item">The item to remove</param>
        /// <param name="iniContent">The file's key</param>
        public void Remove(string iniContent, IniItemBase item)
        {
            if (_files.ContainsKey(iniContent))
            {
                var found = (from f in _files[iniContent] where f.Label == item.Label select f).Any();
                if (found)
                {
                    item.RootKey = null;
                    _files[iniContent].Remove(item);
                }
            }
        }

        /// <summary>
        /// Resets the ini file's contents to be only those found in items, removing any preexisting content
        /// </summary>
        /// <param name="items">The new items</param>
        /// <param name="iniContent">The file's key</param>
        public void Set(string iniContent, IEnumerable<IniItemBase> items)
        {
            if (_files.ContainsKey(iniContent))
            {
                foreach (var c in _files[iniContent])
                {
                    Remove(iniContent, c);
                }

                foreach (var item in items)
                {
                    Add(iniContent, item);
                }
            }
            else
            {
                _files.Add(iniContent, new List<IniItemBase>());
                foreach (var item in items)
                {
                    Add(iniContent, item);
                }
            }
        }

        #endregion

        #region Searching

        /// <summary>
        /// Breaks an ini path string down into components
        /// </summary>
        /// <param name="path">The path string</param>
        /// <returns>The component listing</returns>
        private List<List<string>> BreakdownPath(string path)
        {
            var results = new List<List<string>>();
            string[] parts = null;
            if (path.Contains(File_Content_Designation))
            {
                parts = path.Split(File_Content_Designation);
                results.Add(new List<string>() { parts.First() });
                parts = parts.Last().Split(SubSection_Content_Designation);
            }
            else
            {
                parts = path.Split(SubSection_Content_Designation);
            }

            results.Add(parts.ToList());
            return results;
        }

        /// <summary>
        /// Searches the ini file manager for the given path
        /// </summary>
        /// <param name="path">The path to search for</param>
        /// <returns>Null, or the resulting item</returns>
        public IniItemBase Search(string path)
        {
            return GetByPath(path);
        }

        /// <summary>
        /// Searches for and returns the ini item indicated by the path
        /// </summary>
        /// <param name="path">The path to search for</param>
        /// <param name="catalogue">The ini file structure(s) to search through</param>
        /// <returns>Null or the ini value instance found</returns>
        private IniItemBase GetByPath(string path, Dictionary<string, List<IniItemBase>> catalogue = null)
        {
            if (catalogue == null) catalogue = _files;
            if (catalogue.Count > 1 && !path.Contains(File_Content_Designation))
            {
                throw new InvalidOperationException("Must use file designator when querying store with multiple loaded files.");
            }

            IniItemBase result = null;

            var parts = BreakdownPath(path);
            List<IniItemBase> topic = null;

            // if there is a file designator then use the indicated one
            if (path.Contains(File_Content_Designation) && catalogue.ContainsKey(parts.First().First()))
            {
                topic = catalogue[parts.First().First()];
            }
            else // otherwise, if we got this far, then we only have one file loaded.
            {
                topic = catalogue.First().Value;
            }

            Func< Queue<string>, IniItemBase, IniItemBase> recursor = null;
            recursor = (path, focus) =>
            {
                if (focus == null) return null;

                IniItemBase result = null;
                // check to see if the current focus has the current path node as it's label
                if (focus.Label == path.Dequeue())
                {
                    // it does.  Is this the end of the path?
                    if (path.Count == 0)
                    {
                        // we're done
                        result = focus;
                    }
                    else
                    {
                        // is this a subsection?
                        if (focus is IniSubsection)
                        {
                            // if it's a subsection, then search its contents, continuing the process.
                            result = (from c in ((IniSubsection)focus).Contents
                                        where recursor(CloneQueue(path), c) != null
                                        select c).SingleOrDefault();
                        }
                        else if (focus is IniValue)
                        {
                            // the path doesn't exist
                        }
                    }
                }

                return result;
            };
            result = recursor(
                new Queue<string>(parts.Last()),
                (from item in topic where item.Label == parts.Last().First() select item).SingleOrDefault());
            return result;
        }

        /// <summary>
        /// Clones a string queue
        /// </summary>
        /// <param name="items"></param>
        /// <returns></returns>
        private Queue<string> CloneQueue(Queue<string> items)
        {
            var result = new Queue<string>();
            foreach (var item in items)
            {
                result.Enqueue(item);
            }

            return result;
        }

        #endregion

        #region I/O

        /// <summary>
        /// Loads an ini file, using default values and minimal structure from a provided schema, always overwrites existing entries
        /// </summary>
        /// <param name="iniFilePath">The path to load</param>
        /// <param name="additive">If true, items discovererd in the file will be added to the schema if missing</param>
        /// <param name="throwMismatchExceptions">If true, will throw a SchemaMismatchException if the loaded file and the schema conflict</param>
        /// <param name="schema">The expected minimal structure of the ini file.  Missing items will use their default values</param>
        public void Fill(string iniFilePath, bool additive, bool throwMismatchExceptions, params IniItemBase[] schema)
        {
            if (File.Exists(iniFilePath))
            {
                var fi = new FileInfo(iniFilePath);
                var key = (UseFullPathAsKey ? fi.FullName : fi.Name);
                var items = ReadFile(iniFilePath);

                // rather than take the read file as our result,
                // we use it to fill in the schema's values.
                // overwriting unconditionally, as the schema should only contain defaults.

                // to do this:
                // recursively loop through the file results,
                // search for each part found in the schema,
                // and copy the value over (from file item into schema item)
                Action<IniItemBase> recursor = null;
                var schemaDict = new Dictionary<string, List<IniItemBase>>() { [Fill_Temp_Schema_Dict_Key] =schema.ToList() };
                recursor = (focus) =>
                {
                    var schemaItem = GetByPath(focus.GetPath(), schemaDict);
                    if (focus is IniValue)
                    {
                        if (schemaItem != null && schemaItem is IniValue)
                        {
                            ((IniValue)schemaItem).Value = ((IniValue)focus).Value;
                        }
                        else if (schemaItem != null && schemaItem is IniSubsection)
                        {
                            if (throwMismatchExceptions) throw new SchemaMismatchException($"Node type mismatch at path '{focus.GetPath()}' of conflicting types.");
                        }
                        else if (schemaItem == null && additive)
                        {
                            EnsureAdd(focus, schemaDict);
                        }
                    }
                    else if (focus is IniSubsection)
                    {
                        if (schemaItem != null && schemaItem is IniSubsection)
                        {
                            // Loop through and check the contents.
                            foreach (var c in ((IniSubsection)focus).Contents)
                            {
                                recursor(c);
                            }
                        }
                        else if (schemaItem != null && schemaItem is IniValue)
                        {
                            if (throwMismatchExceptions) throw new SchemaMismatchException($"Node type mismatch at path '{focus.GetPath()}' of conflicting types.");
                        }
                        else if (schemaItem == null && additive)
                        {
                            EnsureAdd(focus, schemaDict);
                        }
                    }
                };
                items.ForEach((c) => recursor(c));

                if (schemaDict[Fill_Temp_Schema_Dict_Key].Count > 0)
                {
                    if (Files.ContainsKey(key))
                    {
                        Set(key, schemaDict[Fill_Temp_Schema_Dict_Key]);
                    }
                    else
                    {
                        Set(key, schemaDict[Fill_Temp_Schema_Dict_Key]);
                    }
                }
            }
        }

        /// <summary>
        /// Builds the given item and its path into topic
        /// </summary>
        /// <param name="item">The item who's path is to exist</param>
        /// <param name="topic">The topic to contain it</param>
        private void EnsureAdd(IniItemBase item, Dictionary<string, List<IniItemBase>> topic)
        {
            var topicListing = topic[topic.Keys.First()];

            Action<IniItemBase> recursor = null;
            recursor = (focus) =>
            {
                // search the topic for the focus
                var found = GetByPath(focus.GetPath(), topic);
                if (found == null)
                {
                    // if we don't find the focus then check its container.  If it has one.
                    if (focus.SubSection == null)
                    {
                        // this is a root element
                        // just add it and we're done
                        topicListing.Add((IniItemBase)focus.Clone());
                    }
                    else
                    {
                        // this is not a root element
                        // see if we can find the container
                        recursor(focus.SubSection);

                        // once that completes, we should have the structure
                        // search for it
                        found = GetByPath(focus.SubSection.GetPath(), topic);
                        if (found != null && found is IniSubsection)
                        {
                            // add a clone of the focus
                            ((IniItemBase)focus.Clone()).SetParent((IniSubsection)found);
                        }
                        else
                        {
                            // getting here means something absolutely failed.
                        }
                    }
                }
                // if we found the focus then we're done
            };
            recursor(item);
        }

        /// <summary>
        /// Loads an ini file from disk and adds it to the Files dictionary
        /// </summary>
        /// <param name="iniFilePath">The path to load</param>
        /// <param name="overwriteExisting">Whether or not to overwrite an existing file entry</param>
        public void Load(string iniFilePath, bool overwriteExisting)
        {
            if (File.Exists(iniFilePath))
            {
                var fi = new FileInfo(iniFilePath);
                var key = (UseFullPathAsKey ? fi.FullName : fi.Name);
                if (!overwriteExisting && Files.ContainsKey(key)) return;
                var items = ReadFile(iniFilePath);                

                if (items.Count > 0)
                {
                    if (Files.ContainsKey(key))
                    {
                        Set(key, items);
                    }
                    else
                    {
                        Set(key, items);
                    }
                }
            }
        }

        /// <summary>
        /// Assumes that a file exists and is an ini file
        /// Reads the contents and returns it as IniItemBase objects
        /// </summary>
        /// <param name="file">The path to the file</param>
        /// <returns>The results</returns>
        private List<IniItemBase> ReadFile(string file)
        {
            var items = new List<IniItemBase>();
            var line = string.Empty;
            IniItemBase item = null;
            IniSubsection section = null;

            using (TextReader reader = File.OpenText(file))
            {
                while ((line = reader.ReadLine()) != null)
                {
                    if (!line.Trim().StartsWith(";"))
                    {
                        item = IniItemBase.Discover(line);
                        if (item != null)
                        {
                            if (item is IniSubsection)
                            {
                                section = item as IniSubsection;
                                items.Add(item);
                            }
                            else if (item is IniValue)
                            {
                                if (section != null)
                                {
                                    item.SetParent(section);
                                }
                                else
                                {
                                    items.Add(item);
                                }
                            }
                        }
                    }
                }
            }

            return items;
        }

        /// <summary>
        /// Writes the indicated file to the given path
        /// </summary>
        /// <param name="iniFile">The file to write (the key to the specific loaded files)</param>
        /// <param name="path">The path to write the file to</param>
        public void WriteFile(string iniFile, string path)
        {
            if (_files.ContainsKey(iniFile))
            {
                Action<IniItemBase, TextWriter> recursive = null;
                recursive = (item, file) =>
                {
                    if (item is IniSubsection)
                    {
                        var sect = (IniSubsection)item;
                        file.WriteLine(sect.ToString());

                        if (sect.Contents.Count > 0)
                        {
                            foreach (var c in sect.Contents)
                            {
                                recursive(c, file);
                            }
                            file.WriteLine();
                        }
                    }
                    else if (item is IniValue)
                    {
                        var val = (IniValue)item;
                        file.WriteLine(val.ToString());
                    }
                };

                using (TextWriter writer = File.CreateText(path))
                {
                    foreach (var item in _files[iniFile])
                    {
                        recursive(item, writer);
                    }
                }
            }
        }

        #endregion

        #region Static Readers

        /// <summary>
        /// Loads an IniFilePath and returns it in a manager
        /// </summary>
        /// <param name="iniFilePath">The file to load</param>
        /// <param name="manager">Manager to load the ini file into</param>
        /// <param name="overwriteExisting">Whether or not to overwrite an existing file entry</param>
        /// <returns></returns>
        public static IniFileManager Discover(string iniFilePath, IniFileManager manager, bool overwriteExisting = false)
        {
            if (manager == null) return null;
            
            manager.Load(iniFilePath, overwriteExisting);
            return manager;
        }

        /// <summary>
        /// Loads an IniFilePath and returns it in a manager
        /// </summary>
        /// <param name="iniFilePath">The file to load</param>
        /// <param name="fullPathAsKey">Whether or not to use the full path or the name of the file as its key</param>
        /// <param name="overwriteExisting">Whether or not to overwrite an existing file entry</param>
        /// <returns></returns>
        public static IniFileManager Discover(string iniFilePath, bool fullPathAsKey = false, bool overwriteExisting = false)
        {
            var manager = new IniFileManager(fullPathAsKey);
            return Discover(iniFilePath, manager, overwriteExisting);
        }

        /// <summary>
        /// Takes an ini file and adds its values to a given definition.  This approach uses default values
        /// </summary>
        /// <param name="iniFilePath">The file to load</param>
        /// <param name="schema">The expected minimal structure of the ini file.  Missing items will use their default values</param>
        /// <param name="manager">Manager to load the ini file into</param>
        /// <returns></returns>
        public static IniFileManager Explore(
            string iniFilePath, 
            bool additive, 
            bool throwMismatchExceptions, 
            IniFileManager manager,
            params IniItemBase[] schema)
        {
            if (manager == null) return null;

            manager.Fill(iniFilePath, additive, throwMismatchExceptions, schema);
            return manager;
        }

        /// <summary>
        /// Takes an ini file and adds its values to a given definition.  This approach uses default values
        /// </summary>
        /// <param name="iniFilePath">The file to load</param>
        /// <param name="schema">The expected minimal structure of the ini file.  Missing items will use their default values</param>
        /// <param name="fullPathAsKey">Whether or not to use the full path or the name of the file as its key</param>
        /// <returns></returns>
        public static IniFileManager Explore(
            string iniFilePath, 
            bool additive, 
            bool throwMismatchExceptions, 
            bool fullPathAsKey = false, 
            params IniItemBase[] schema)
        {
            var manager = new IniFileManager(fullPathAsKey);
            return Explore(iniFilePath, additive, throwMismatchExceptions, manager, schema);
        }

        #endregion
    }
}
