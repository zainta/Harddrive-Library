using System;
using System.Collections.Generic;
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
    }
}
