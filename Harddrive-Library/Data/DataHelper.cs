// Copyright (c) Zain Al-Ahmary.  All rights reserved.
// Licensed under the MIT License, (the "License"); you may not use this file except in compliance with the License. 
// You may obtain a copy of the License at https://mit-license.org/

using System.Collections.Generic;
using System.Text;

namespace HDDL.Data
{
    /// <summary>
    /// Provides some helper methods for processing text
    /// </summary>
    abstract class DataHelper
    {
        /// <summary>
        /// Escapes the provided text to make it safe to use in SQL
        /// </summary>
        /// <param name="text">The text to sanitize</param>
        /// <returns></returns>
        public static string Sanitize(string text)
        {
            string result = null;
            if (text != null)
            {
                result = text.Replace("'", "''");
            }

            return result;
        }

        /// <summary>
        /// Generates a comma seperated, single quote encased string from the list of provided strings
        /// </summary>
        /// <param name="set">The strings to process</param>
        /// <param name="parens">Whether or not to encase with parenthesis</param>
        /// <param name="connector">The string used to connect successive items in the result string</param>
        /// <param name="prefix">This is inserted immediately prior to every item in set</param>
        /// <param name="suffix">This is inserted immediately after to every item in set</param>
        /// <returns></returns>
        public static string GetListing(IEnumerable<string> set, bool parens, string connector = ", ", string prefix = "'", string suffix = "'")
        {
            var first = true;
            var sql = new StringBuilder();
            if (parens)
            {
                sql.Append("(");
            }

            foreach (var str in set)
            {
                if (!first)
                {
                    sql.Append(connector);
                }
                else
                {
                    first = false;
                }

                if (!string.IsNullOrWhiteSpace(prefix))
                {
                    sql.Append($"{prefix}");
                }

                sql.Append($"{Sanitize(str)}");

                if (!string.IsNullOrWhiteSpace(suffix))
                {
                    sql.Append($"{suffix}");
                }
            }
            if (parens)
            {
                sql.Append(")");
            }

            return sql.ToString();
        }
    }
}
