﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace HDDL.Data
{
    abstract class DateTimeDataHelper
    {
        /// <summary>
        /// Takes a string and converts it into a DateTime
        /// </summary>
        /// <param name="str">The string to convert</param>
        /// <returns></returns>
        public static DateTime ConvertToDateTime(string str)
        {
            string pattern = @"(\d{4})-(\d{2})-(\d{2}) (\d{2}):(\d{2}):(\d{2})(?:\.(\d{0,3}))?";
            if (Regex.IsMatch(str, pattern))
            {
                Match match = Regex.Match(str, pattern);
                int year = Convert.ToInt32(match.Groups[1].Value);
                int month = Convert.ToInt32(match.Groups[2].Value);
                int day = Convert.ToInt32(match.Groups[3].Value);
                int hour = Convert.ToInt32(match.Groups[4].Value);
                int minute = Convert.ToInt32(match.Groups[5].Value);
                int second = Convert.ToInt32(match.Groups[6].Value);
                int millisecond = Convert.ToInt32(match.Groups[7].Value);
                return (new DateTime(year, month, day, hour, minute, second, millisecond)).ToLocalTime();
            }
            else
            {
                throw new Exception("Unable to parse.");
            }
        }

        /// <summary>
        /// Converts a DateTime to a string in the format that ConvertToDateTime expects
        /// </summary>
        /// <param name="dt">The datetime to convert</param>
        /// <returns></returns>
        public static string ConvertToString(DateTime dt)
        {
            return dt.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss.fff");
        }
    }
}