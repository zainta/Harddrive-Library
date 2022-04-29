using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HDDL.IO
{
    /// <summary>
    /// Provides helper and utility methods for use
    /// </summary>
    public class DiskHelper
    {
        /// <summary>
        /// Takes in a numerical value and reduces it to a textual representation (e.g 1.1mb)
        /// </summary>
        /// <param name="value">The value to shorten</param>
        /// <returns></returns>
        public static string ShortenSize(long? value)
        {
            if (value.HasValue)
            {
                var abbreviations = new string[] { "B", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB", "XB", "SB", "DB" };
                var degrees = 1;
                long denomination = 1024;
                while (value > denomination)
                {
                    degrees++;
                    denomination *= 1024;
                }
                degrees--;
                denomination /= 1024;

                var displayValue = Math.Truncate(100 * ((double)value) / denomination) / 100;
                var result = $"{displayValue}{abbreviations[degrees]}";
                return result;
            }
            else
            {
                return "0B";
            }
        }
    }
}
