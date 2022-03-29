// Copyright (c) Zain Al-Ahmary.  All rights reserved.
// Licensed under the MIT License, (the "License"); you may not use this file except in compliance with the License. 
// You may obtain a copy of the License at https://mit-license.org/

using HDDL.Language.Json.Conversion;
using System;

namespace HDDL.Language.Json
{
    /// <summary>
    /// Static class to handle the translation to and from Json of Outcome classes.
    /// </summary>
    static class JsonConverter
    {
        /// <summary>
        /// Formats the json string for easy viewing
        /// </summary>
        /// <param name="json">The json string to format</param>
        /// <param name="indentation">The character(s) to use for indentation</param>
        /// <returns>The formatted string</returns>
        public static string Format(string json, string indentation = "  ")
        {
            return JsonFormatter.Format(json, indentation);
        }

        /// <summary>
        /// Takes an HDSLOutcome instance, or derived instance, and returns it as JSON.
        /// </summary>
        /// <param name="obj">The object to convert</param>
        /// <param name="format">Whether or not to format the json</param>
        /// <returns></returns>
        public static string GetJson(object obj, bool format = false)
        {
            if (format)
            {
                return Format(JsonBagHandler.GetJson(obj));
            }
            else
            {
                return JsonBagHandler.GetJson(obj);
            }
        }

        /// <summary>
        /// Takes a Json string and attempts to convert it to the indicated type
        /// </summary>
        /// <typeparam name="T">The desired result type</typeparam>
        /// <param name="json">The json to convert</param>
        /// <returns>Returns the proper type if successful</returns>
        /// <exception cref="JsonConversionException">Thrown if conversion fails</exception>
        public static T GetObject<T>(string json)
        {
            LogItemBase[] issues;
            var obj = JsonBagHandler.GetIntermediate(json, out issues);

            if (issues.Length > 0)
            {
                throw new JsonConversionException("An error occurred during conversion.", issues);
            }
            else if (obj.Evaluate(typeof(T), true))
            {
                return (T)Convert(obj);
            }
            else
            {
                throw new JsonConversionException("Target type does not match json.", Array.Empty<LogItemBase>());
            }
        }

        /// <summary>
        /// Takes a json string and determines what type to convert it to
        /// </summary>
        /// <param name="json">The json to convert</param>
        /// <returns>Returns the proper type if successful</returns>
        /// <exception cref="JsonConversionException">Thrown if conversion fails</exception>
        public static object GetObject(string json)
        {
            LogItemBase[] issues;
            var obj = JsonBagHandler.GetIntermediate(json, out issues);

            if (issues.Length > 0)
            {
                throw new JsonConversionException("An error occurred during conversion.", issues);
            }
            else
            {
                // determine what type to use
                if (obj.Evaluate(true))
                {
                    return Convert(obj);
                }
                else
                {
                    throw new JsonConversionException("Deserialization failed.  See Issues property for further information.", issues);
                }
            }
        }

        /// <summary>
        /// Converts the JsonBase instance into the actual target class
        /// </summary>
        /// <param name="jb">The JsonBase to convert</param>
        /// <returns></returns>
        private static object Convert(JsonBase jb)
        {
            try
            {
                var result = jb.AsObject();
                return result;
            }
            catch (Exception ex)
            {
                throw new JsonConversionException("An error was encountered during the deserialization process.", ex);
            }
        }
    }
}
