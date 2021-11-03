// Copyright (c) Zain Al-Ahmary.  All rights reserved.
// Licensed under the MIT License, (the "License"); you may not use this file except in compliance with the License. 
// You may obtain a copy of the License at https://mit-license.org/

using System;
using System.Collections.Generic;

namespace HDDL.Extensions
{
    /// <summary>
    /// Extension methods for the Enum type
    /// </summary>
    public static class EnumExtensions
    {
        /// <summary>
        /// Returns an enumeration of the items in an enum
        /// </summary>
        /// <typeparam name="T">The type of the enum</typeparam>
        /// <param name="item">The item to enumerate</param>
        /// <returns></returns>
        public static IEnumerable<T> Enumerate<T>(this Enum item)
        {
            if (!(item is T))
            {
                throw new ArgumentException("Calling type and type parameter types are mismatched.");
            }

            var results = new List<T>();
            foreach (var a in Enum.GetValues(item.GetType()))
            {
                results.Add((T)a);
            }

            return results;
        }
    }
}
