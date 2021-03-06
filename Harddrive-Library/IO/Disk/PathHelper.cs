// Copyright (c) Zain Al-Ahmary.  All rights reserved.
// Licensed under the MIT License, (the "License"); you may not use this file except in compliance with the License. 
// You may obtain a copy of the License at https://mit-license.org/

using HDDL.Data;
using ReddWare.Threading;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace HDDL.IO.Disk
{
    /// <summary>
    /// Contains static methods for path comparison
    /// </summary>
    class PathHelper
    {
        internal delegate void PathHelperExploringLocation(string path, bool isFile);

        /// <summary>
        /// Occurs when a location is discovered by the path processing method's recursive method
        /// </summary>
        internal static event PathHelperExploringLocation ExploringLocation;


        /// <summary>
        /// Takes a path and, if it contains text, ensures that it ends in a forward slash (\)
        /// </summary>
        /// <param name="path">The path to check</param>
        /// <param name="ignoreNonExistent">If true, then EnsurePath will return the value unmodified if the target does not exist</param>
        /// <returns></returns>
        public static string EnsurePath(string path, bool ignoreNonExistent = false)
        {
            var result = path;
            if (!string.IsNullOrWhiteSpace(path))
            {
                switch (IsDirectory(path))
                {
                    case DiskItemStatus.File:
                        // files need to end in no forward slash
                        if (EndsInDirSeperator(path))
                        {
                            result = path.Substring(0, path.Length - 1);
                        }
                        break;
                    case DiskItemStatus.Directory:
                        // directories need to end in a forward slash (\)
                        if (!EndsInDirSeperator(path))
                        {
                            result = $"{path}{Path.DirectorySeparatorChar}";
                        }
                        break;
                    case DiskItemStatus.NonExistent:
                        if (!ignoreNonExistent)
                        {
                            result = null;
                        }
                        break;
                }
            }

            return result;
        }

        /// <summary>
        /// Checks to see if the given string ends in one of the accepted directory seperators
        /// </summary>
        /// <param name="path">The path to check</param>
        /// <returns></returns>
        public static bool EndsInDirSeperator(string path)
        {
            return path.EndsWith(Path.DirectorySeparatorChar) || path.EndsWith(Path.AltDirectorySeparatorChar);
        }

        /// <summary>
        /// Takes a set of paths and, if it contains text, ensures that they end in forward slashes (\)
        /// </summary>
        /// <param name="paths">The paths to check</param>
        /// <param name="ignoreNonExistent">If true, then EnsurePath will return the value unmodified if the target does not exist</param>
        /// <returns></returns>
        public static string[] EnsurePath(IEnumerable<string> paths, bool ignoreNonExistent = false)
        {
            var result = paths.Select(p => EnsurePath(p, ignoreNonExistent)).Where(p => p != null);
            return result.ToArray();
        }

        /// <summary>
        /// Checks to see if the given path is a file or directory
        /// </summary>
        /// <param name="path">The patch to check</param>
        /// <returns></returns>
        public static DiskItemStatus IsDirectory(string path)
        {
            try
            {
                return (File.GetAttributes(path) & FileAttributes.Directory) == FileAttributes.Directory ? DiskItemStatus.Directory : DiskItemStatus.File;
            }
            catch (Exception ex)
            {
                return DiskItemStatus.NonExistent;
            }
        }

        /// <summary>
        /// Takes a DiskItemType and regresses until it reaches the drive root
        /// </summary>
        /// <param name="diskItemType">A path to anchor to the root</param>
        /// <returns>Returns all directories found during the regression</returns>
        public static IEnumerable<DiskItemType> AnchorPath(DiskItemType diskItemType)
        {
            var results = new List<string>();
            var dir = diskItemType.IsFile ? diskItemType.FInfo.Directory : diskItemType.DInfo;
            while (dir != null && dir.Parent != null)
            {
                if (dir.Parent != null)
                {
                    results.Add(dir.Parent.FullName);
                    dir = dir.Parent;
                }
            }

            return (from d in results
                    orderby d.Count(f => f == Path.DirectorySeparatorChar || f == Path.AltDirectorySeparatorChar) ascending
                    select new DiskItemType(d, false));
        }

        /// <summary>
        /// Takes a DiskItemType and returns the number of levels from the root it is (including the root)
        /// Adds 1 for files
        /// </summary>
        /// <param name="diskItemType">The item to find the dependency count of</param>
        /// <returns>The number of dependencies</returns>
        public static int GetDependencyCount(DiskItemType diskItemType)
        {
            return GetDependencyCount(diskItemType.Path, diskItemType.IsFile);
        }

        /// <summary>
        /// Returns the number of levels from the root the given path is (including the root)
        /// Adds 1 for files
        /// </summary>
        /// <param name="path">The path to test</param>
        /// <param name="isFile">If the path is a file or directory</param>
        /// <returns>The number of dependencies</returns>
        public static int GetDependencyCount(string path, bool isFile)
        {
            var primCount = path.Where(c => c == Path.DirectorySeparatorChar).Count();
            var secCount = path.Where(c => c == Path.AltDirectorySeparatorChar).Count();

            if (isFile)
            {
                return primCount + secCount;
            }

            return primCount + secCount - 1;
        }

        /// <summary>
        /// Case insensitively compares paths to check if the query is or is within the container
        /// </summary>
        /// <param name="query">The path to check the status of</param>
        /// <param name="container">The path the query is checked against</param>
        /// <param name="acceptDuplicates">Whether or not to return true on duplicate paths</param>
        /// <returns>True if it's inside, false otherwise</returns>
        public static bool IsWithinPath(string query, string container, bool acceptDuplicates = true)
        {
            // Ensure that the two paths end in slashes
            if (!EndsInDirSeperator(query)) query = query + Path.DirectorySeparatorChar;
            if (!EndsInDirSeperator(container)) container = container + Path.DirectorySeparatorChar;

            if (Uri.IsWellFormedUriString(query, UriKind.RelativeOrAbsolute) ||
                Uri.IsWellFormedUriString(container, UriKind.RelativeOrAbsolute))
            {
                return false;
            }

            // For case insensitivity
            var q = query.ToLower();
            var c = container.ToLower();

            // Check to see if they are duplicates
            if (q == c) return acceptDuplicates ? true : false;

            // Check their status
            var uri = new Uri(c);
            return uri.IsBaseOf(new Uri(q));
        }

        /// <summary>
        /// Case insensitively compares paths to check if the query is or is within any of the containers
        /// </summary>
        /// <param name="query">The path to check the status of</param>
        /// <param name="containers">The paths the query is checked against</param>
        /// <param name="acceptDuplicates">Whether or not to return true on duplicate paths</param>
        /// <returns>True if it's inside, false otherwise</returns>
        public static bool IsWithinPaths(string query, IEnumerable<string> containers, bool acceptDuplicates = true)
        {
            var any =
                (from c in containers
                 where IsWithinPath(query, c, acceptDuplicates) == true
                 select "Yes").Any();

            return any;
        }

        /// <summary>
        /// Takes a disk item type and a dependency level sorted list dictionary and adds the item to the correct dependency level list
        /// </summary>
        /// <param name="target">The dictionary to add to</param>
        /// <param name="item">The item to add it to</param>
        private static void SafelyAdd(ConcurrentDictionary<int, List<DiskItemType>> target, DiskItemType item)
        {
            var level = GetDependencyCount(item);
            target.TryAdd(level, new List<DiskItemType>());
            target[level].Add(item);
        }

        /// <summary>
        /// Returns the number of total items in all sublists in the dictionary
        /// </summary>
        /// <param name="focus">The dictionary to process</param>
        /// <returns></returns>
        private static long GetTotal(ConcurrentDictionary<int, List<DiskItemType>> focus)
        {
            long result = 0;
            foreach (var key in focus.Keys)
            {
                result += focus[key].Count;
            }

            return result;
        }

        /// <summary>
        /// Takes a list of paths and sorts them and their contents by disk.  
        /// 
        /// This prevents multiple threads from trying to read the same hard drive at once.
        /// </summary>
        /// <param name="paths">The paths to test</param>
        /// <param name="exclusions">A list of defined exclusions (paths that should not be scanned)</param>
        /// <param name="maxThreads">The maximum number of root threads to scan the disk with</param>
        /// <returns>Returns the sorted items</returns>
        public static PathSetData GetProcessedPathContents(IEnumerable<string> paths, IEnumerable<ExclusionItem> exclusions, int maxThreads = 4)
        {
            var result = new PathSetData();

            // make sure our directories correctly end in forward slashes (\) and remove any bad paths
            paths = EnsurePath(paths);

            // extract the excluded regions so we can more easily query it
            var excludedItems = (from e in exclusions select e.Path);

            var allFiles = new ConcurrentDictionary<int, List<DiskItemType>>();
            var allDirectories = new ConcurrentDictionary<int, List<DiskItemType>>();
            var unlistables = new ConcurrentBag<DiskItemType>();
            Func<string, ConcurrentDictionary<int, List<DiskItemType>>, ConcurrentDictionary<int, List<DiskItemType>>, bool> recursor = null;
            recursor = (path, files, directories) =>
            {
                ExploringLocation?.Invoke(path, false);

                // exclusions will have ensured paths while path won't be ensured.
                if (IsWithinPaths(path, excludedItems))
                {
                    // this is not a failure because we have been told not to do it.
                    return true; 
                }

                if (Directory.Exists(path))
                {
                    // used for the purpose of tracking what we were scanning when the exception occurred
                    bool isFile = false;
                    try
                    {
                        var outcome = true;
                        Parallel.ForEach(Directory.GetDirectories(path).Select(p => $"{p}\\"),
                            (dir) =>
                            {
                                outcome = recursor(dir, files, directories);
                            });

                        if (outcome)
                        {
                            isFile = true;
                            foreach (var file in Directory.GetFiles(path))
                            {
                                // specific files can be excluded, so check
                                if (!excludedItems.Where(ei => ei.Equals(file, StringComparison.InvariantCultureIgnoreCase)).Any())
                                {
                                    ExploringLocation?.Invoke(file, true);
                                    SafelyAdd(files, new DiskItemType(file, true));
                                }
                            };
                        }
                    }
                    catch (UnauthorizedAccessException ex)
                    {
                        int start = ex.Message.IndexOf("'") + 1, end = ex.Message.LastIndexOf("'") - 1;
                        var unlistablePath = ex.Message.Substring(start, end - start);
                        unlistables.Add(new DiskItemType(unlistablePath, isFile));

                        return false;
                    }

                    var ensured = EnsurePath(path);
                    if (ensured != null)
                    {
                        SafelyAdd(directories, new DiskItemType(ensured, false));
                    }
                    else
                    {
                        return false;
                    }
                }

                return true;
            };

            // figure out how many unique lettered media (disks, cd drives, etc) are getting scanned.
            // this is the maximum number of threads we want to use
            var roots = new List<string>();
            foreach (var path in paths)
            {
                string root = null;
                if (EndsInDirSeperator(path))
                {
                    root = new DirectoryInfo(path).Root.FullName;
                }
                else
                {
                    root = new FileInfo(path).Directory.Root.FullName;
                }

                if (!roots.Contains(root))
                {
                    roots.Add(root);
                }
            }
            var threads = roots.Count <= maxThreads ? roots.Count : maxThreads;
            if (threads == 0)
            {
                return new PathSetData();
            }

            // Figure out how many unique targets are getting scanned.
            // This eliminates paths within paths, ensuring we will not get any duplicates
            var uniqueTargets = new List<string>();
            foreach (var path in paths.OrderBy(p => p.Length))
            {
                if (uniqueTargets.Count > 0)
                {
                    if (!IsWithinPaths(path, uniqueTargets))
                    {
                        uniqueTargets.Add(path);
                    }
                }
                else
                {
                    uniqueTargets.Add(path);
                }
            }

            var tq = new ThreadedQueue<string>(
                (path) =>
                {
                    if (File.Exists(path))
                    {
                        SafelyAdd(allFiles, new DiskItemType(path, true));
                    }
                    else if (Directory.Exists(path))
                    {
                        recursor(path, allFiles, allDirectories);
                    }
                }, threads);
            tq.Start(uniqueTargets);
            tq.WaitAll();

            // the directories must reach all the way to the root.
            // check to see if each path parameter has 0 dependencies (i.e is a root path)
            // if it isn't, then add its dependencies (this will include the root)
            foreach (var path in paths)
            {
                var dit = new DiskItemType(path, File.Exists(path));
                var anchors = AnchorPath(dit)
                    .Select((t) =>
                    {
                        t.Path = EnsurePath(t.Path);
                        if (string.IsNullOrWhiteSpace(t.Path))
                        {
                            return null;
                        }

                        return t;
                    })
                    .Where(t => !string.IsNullOrWhiteSpace(t.Path));
                if (anchors.Count() > 0)
                {
                    var allDirs = new List<DiskItemType>();
                    foreach (var key in allDirectories.Keys)
                    {
                        allDirs.AddRange(allDirectories[key]);
                    }

                    var nonDuplicates = anchors.Where(ap => !allDirs.Where(d => d.Path == ap.Path).Any());
                    foreach (var item in nonDuplicates)
                    {
                        SafelyAdd(allDirectories, item);
                    }
                }
            }

            // now that we have everything sorted and ready to go, merge the groups with the directories first
            result.ProcessedContent.AddRange(allDirectories.Values);
            result.ProcessedContent.AddRange(allFiles.Values);

            result.TotalDirectories = GetTotal(allDirectories);
            result.TotalFiles = GetTotal(allFiles);

            return result;
        }
    }
}
