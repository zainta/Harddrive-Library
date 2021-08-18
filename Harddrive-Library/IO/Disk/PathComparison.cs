﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HDDL.IO.Disk
{
    /// <summary>
    /// Contains static methods for path comparison
    /// </summary>
    class PathComparison
    {
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
            if (!query.EndsWith("\\")) query = query + "\\";
            if (!container.EndsWith("\\")) container = container + "\\";

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
        /// Takes a list of paths and sorts them and their contents by disk.  
        /// 
        /// This prevents multiple threads from trying to read the same hard drive at once.
        /// </summary>
        /// <param name="paths">The paths to test</param>
        /// <returns>Returns the sorted items</returns>
        public static PathSetData GetContentsSortedByRoot(IEnumerable<string> paths)
        {
            var result = new PathSetData(new Dictionary<string, List<DiskItemType>>(), 0, 0);

            var intermediate = new List<DiskItemType>();

            // Expand the search paths out to get the files and subdirectories
            foreach (var path in paths)
            {
                if (File.Exists(path))
                {
                    intermediate.Add(new DiskItemType(path, true));
                }
                else if (Directory.Exists(path))
                {
                    intermediate.AddRange(Directory.GetDirectories(path, "*.*", SearchOption.AllDirectories).Select(p => new DiskItemType(p, false)));
                    intermediate.AddRange(Directory.GetFiles(path, "*.*", SearchOption.AllDirectories).Select(p => new DiskItemType(p, true)));
                }
            }

            // sort them by containing folder
            foreach (var item in intermediate)
            {
                string root = null;
                if (item.IsFile)
                {
                    root = item.FInfo.Directory.Root.FullName.ToUpper();
                }
                else if (!item.IsFile)
                {
                    root = item.DInfo.Root.FullName.ToUpper();
                }

                if (!result.TargetInformation.ContainsKey(root))
                {
                    result.TargetInformation.Add(root, new List<DiskItemType>());
                }

                // Add it
                result.TargetInformation[root].Add(item);
            }

            // Remove duplicates from each root
            foreach (var root in result.TargetInformation.Keys)
            {
                var uniques = result.TargetInformation[root].Distinct(new DiskItemTypeEqualityComparer()).ToList();
                result.TotalDirectories += uniques.Where(dit => !dit.IsFile).Count();
                result.TotalFiles += uniques.Where(dit => dit.IsFile).Count();

                result.TargetInformation[root] = uniques;
            }

            return result;
        }
    }
}
