﻿// Copyright (c) Zain Al-Ahmary.  All rights reserved.
// Licensed under the MIT License, (the "License"); you may not use this file except in compliance with the License. 
// You may obtain a copy of the License at https://mit-license.org/

using HDDL.Collections;
using HDDL.Language.Json.Conversion;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace HDDL.Language.Json.Reflection
{
    /// <summary>
    /// Provides static helper methods for reflection-related tasks
    /// </summary>
    abstract class TypeHelper
    {
        /// <summary>
        /// Lookup table for property info instances for specific types
        /// </summary>
        private static ConcurrentDictionary<Type, PropertyInfo[]> _propertyCache;

        /// <summary>
        /// Lookup table for property
        /// </summary>
        private static ExpiringCache<string, Tuple<bool, Type[]>> _relevantTypeCache;

        static TypeHelper()
        {
            _propertyCache = new ConcurrentDictionary<Type, PropertyInfo[]>();
            Task.Run(() =>
            {
                GetAllTypes();
            });

            _relevantTypeCache = new ExpiringCache<string, Tuple<bool, Type[]>>(500);
        }

        /// <summary>
        /// Gets propertyinfo for each of the json serializable properties on a type
        /// </summary>
        /// <param name="t">The type</param>
        /// <returns>An array of elligible properties</returns>
        public static PropertyInfo[] GetValidProperties(Type t)
        {
            return (from p in PropsOf(t)
                    where
                        p.CanWrite == true &&
                        p.GetCustomAttribute<JsonIgnoreAttribute>(true) == null
                    select p).ToArray();
        }

        /// <summary>
        /// Retrieves all available relevant types
        /// </summary>
        /// <param name="jb">The type to get relevant types for</param>
        /// <param name="refresh">If true, forces a refresh instead of using the cache</param>
        /// <returns></returns>
        public static Type[] GetRelevantTypes(JsonBase jb, bool refresh = false)
        {
            var possibleTypes = GetRelevantTypes(GetAllTypes(refresh), jb);
            return possibleTypes;
        }

        //private static ConcurrentStack<double> _calls = new ConcurrentStack<double>();
        /// <summary>
        /// Based on the type of JsonBase, returns the relevant types for comparison
        /// </summary>
        /// <param name="types">A list of types to filter</param>
        /// <param name="jb">The topical JsonBase</param>
        /// <returns></returns>
        public static Type[] GetRelevantTypes(IEnumerable<Type> types, JsonBase jb)
        {
            Type[] results = null;
            if (jb is ValueTypeQuantity)
            {
                var vtq = jb as ValueTypeQuantity;
                results = new Type[] { vtq.Kind };
            }
            else if (jb is JsonBag)
            {
                var bag = jb as JsonBag;
                if (_relevantTypeCache.Has(bag.GetKeyString()))
                {
                    results = _relevantTypeCache[bag.GetKeyString()].Item2;
                }
                else
                {
                    results =
                        (from t in types.AsParallel()
                         where
                             !t.IsAbstract &&
                             !t.IsInterface &&
                             t.GetCustomAttribute<ObsoleteAttribute>() == null &&
                             t.GetCustomAttribute<JsonIgnoreAttribute>() == null &&
                             (from p in PropsOf(t)
                              where
                                  p.CanWrite == true &&
                                  bag.Values.Keys.Contains(p.Name) &&
                                  p.GetCustomAttribute<JsonIgnoreAttribute>(true) == null &&
                                  p.GetCustomAttribute<ObsoleteAttribute>() == null
                              select p).Count() == bag.Values.Count
                         select t).ToArray();
                    _relevantTypeCache[bag.GetKeyString()] = new Tuple<bool, Type[]>(false, results);
                }
            }
            else if (jb is JsonArray)
            {
                results =
                    (from t in types.AsParallel()
                     where
                        t.GetInterfaces().Where(t => t == typeof(IEnumerable)).Any() &&
                        t.GetCustomAttribute<ObsoleteAttribute>() == null &&
                        t.GetCustomAttribute<JsonIgnoreAttribute>() == null &&
                        !t.IsInterface &&
                        !t.IsAbstract
                     select t).ToArray();
            }

            return results;
        }

        /// <summary>
        /// Updates (will throw an exception if key does not exist) an existing key with a single, assured target
        /// </summary>
        /// <param name="types">The new set of types</param>
        /// <param name="key">The JsonBag key to update</param>
        /// <exception cref="InvalidOperationException" />
        public static void SetBagKeyCache(IEnumerable<Type> types, string key)
        {
            if (_relevantTypeCache.Has(key))
            {
                _relevantTypeCache[key] = new Tuple<bool, Type[]>(true, types.ToArray());
            }
            else
            {
                throw new InvalidOperationException("JsonBag key not found");
            }
        }

        /// <summary>
        /// Checks to see if the given key has been assured via call to SetBagKeyCache
        /// </summary>
        /// <param name="key">The key to check</param>
        /// <returns></returns>
        public static bool IsAssured(string key)
        {
            if (_relevantTypeCache.Has(key))
            {
                return _relevantTypeCache[key].Item1;
            }

            return false;
        }

        /// <summary>
        /// Returns the key string's associated assured type
        /// </summary>
        /// <param name="key">The key to retrieve</param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException" />
        public static Type GetAssuredType(string key)
        {
            if (_relevantTypeCache.Has(key) &&
                _relevantTypeCache[key].Item2.Length == 1)
            {
                return _relevantTypeCache[key].Item2.Single();
            }

            throw new InvalidOperationException("JsonBag key not found or contains multiple entries.");
        }

        private static Type[] _allTypes;
        /// <summary>
        /// Retrieves a cached set of all available types
        /// </summary>
        /// <param name="refresh">If true, forces a refresh instead of using the cache</param>
        /// <returns></returns>
        public static Type[] GetAllTypes(bool refresh = false)
        {
            if (refresh || _allTypes == null)
            {
                var possibleTargets = new List<Type>();
                foreach (Assembly ass in AppDomain.CurrentDomain.GetAssemblies())
                {
                    possibleTargets.AddRange(ass.GetExportedTypes());
                }

                _allTypes = possibleTargets.ToArray();
            }

            return _allTypes;
        }

        /// <summary>
        /// Returns the first type that all of the provided types can be assigned to safely
        /// </summary>
        /// <param name="types">The types to get the average of</param>
        /// <returns>Always returns at least object</returns>
        public static Type GetAverageType(IEnumerable<Type> types)
        {
            Type result = null;
            
            // check if all of the provided types are the same
            if (types.Distinct().Count() == 1)
            {
                result = types.FirstOrDefault();
            }
            else
            {
                // because we don't really care where the types' inheritance chains cross,
                // we just take the one with the longest chain and use it as the yard stick
                var current = (from t in types orderby GetInheritanceCount(t) descending select t).FirstOrDefault();
                var done = false;
                while (!done)
                {
                    done = (from t in types where t.IsAssignableTo(current) select t).Count() == types.Count();
                    if (!done)
                    {
                        current = current.BaseType;
                    }
                }

                result = current;
            }

            return result;
        }

        /// <summary>
        /// Returns the number of times the class inherits until it's derived from object
        /// </summary>
        /// <param name="type">The type to test</param>
        /// <returns></returns>
        public static int GetInheritanceCount(Type type)
        {
            if (type == typeof(object)) return 0;

            var count = 1;
            var t = type.BaseType;
            while (t != typeof(object))
            {
                t = t.BaseType;
                count++;
            }

            return count;
        }

        /// <summary>
        /// Checks to see if the given value can be converted to the given type
        /// </summary>
        /// <param name="value">The value to attempt conversion on</param>
        /// <param name="type">The target type</param>
        /// <returns>True if successful, false otherwise</returns>
        public static bool TryConvertTo(object? value, Type type)
        {
            if (value == null &&
                (!type.IsValueType || Nullable.GetUnderlyingType(type) != null)) return true;

            try
            {
                if (type.IsEnum)
                {
                    Enum.ToObject(type, value);
                }
                else
                {
                    Convert.ChangeType(value, type);
                }
                
            }
            catch (Exception ex)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Checks to see if the given value can be converted to the given type
        /// </summary>
        /// <param name="value">The value to attempt conversion on</param>
        /// <param name="type">The target type</param>
        /// <returns>True if successful, false otherwise</returns>
        public static object? ConvertTo(object? value, Type type)
        {
            if (value == null &&
                (!type.IsValueType || Nullable.GetUnderlyingType(type) != null)) return true;

            object? result = null;
            if (type.IsEnum)
            {
                result = Enum.ToObject(type, value);
            }
            else
            {
                result = Convert.ChangeType(value, type);
            }

            return result;
        }

        /// <summary>
        /// Returns the propertyinfo instances for the given type.  Favors the cache over a full call
        /// </summary>
        /// <param name="type">The type to retreive the properties for</param>
        /// <returns></returns>
        public static PropertyInfo[] PropsOf(Type type, bool refresh = false)
        {
            if (!_propertyCache.ContainsKey(type) || refresh)
            {
                _propertyCache[type] = type.GetProperties();
            }

            return _propertyCache[type];
        }
    }
}
