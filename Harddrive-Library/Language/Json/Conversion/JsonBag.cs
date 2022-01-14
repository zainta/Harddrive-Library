// Copyright (c) Zain Al-Ahmary.  All rights reserved.
// Licensed under the MIT License, (the "License"); you may not use this file except in compliance with the License. 
// You may obtain a copy of the License at https://mit-license.org/

using HDDL.Language.Json.Reflection;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace HDDL.Language.Json.Conversion
{
    /// <summary>
    /// Stores json objects' properties
    /// </summary>
    class JsonBag : JsonBase
    {
        /// <summary>
        /// Ties a value to property name
        /// </summary>
        public Dictionary<string, JsonBase> Values { get; private set; }

        /// <summary>
        /// Creates an instance
        /// </summary>
        public JsonBag() : base()
        {
            JsonContainerName = "JsonBag";
            Values = new Dictionary<string, JsonBase>();
        }

        /// <summary>
        /// Returns the JsonBase derivation as a json string
        /// </summary>
        /// <returns></returns>
        public override string AsJson()
        {
            return JsonBagHandler.GetBagJson(this);
        }

        /// <summary>
        /// Attempts to determine the type of the JsonBase derivation
        /// </summary>
        /// <returns>True upon complete success, false otherwise</returns>
        /// <exception cref="JsonConversionException"></exception>
        public override bool DetermineType()
        {
            var result = false;

            // evaluate children first
            var childSuccesses = new List<bool>();
            foreach (var jb in Values.Values)
            {
                var r = jb.DetermineType();
                childSuccesses.Add(r);
                if (!r)
                {
                    break;
                }
            }

            // only continue the type assessment if all children successfully assessed themselves
            if (!childSuccesses.Where(cs => !cs).Any())
            {
                // assess every relevant type and keep the one(s) that match(es)
                // for a type to match, it must:
                //      the names must match
                //      all properties must be account for
                //      all property types must be assignable
                var potentials = new List<Type>();
                var relevant = TypeHelper.GetRelevantTypes(this);
                foreach (var type in relevant)
                {
                    // for the given type, assess whether its properties match a property on the JsonBag and has an assignable type

                    // to do that,
                    // compare all of the potential type's properties to all of the JsonBag's properties to see if they match
                    // then take the number that match and check it against the total number of the JsonBag's properties to make sure that they *all* match
                    // if they do then that's a potential (it's a full match, really)

                    // note that if the value is a JsonArray then it is ignored because all JsonArray automatically assume they will be arrays.
                    // this means they can be passed as the initialization parameter to the actual target enumeration
                    var validProps = TypeHelper.GetValidProperties(type);
                    bool compatibleProperties =
                        validProps
                            .Where(vp =>
                                Values.Keys.Where(k => k.Equals(vp.Name, StringComparison.InvariantCultureIgnoreCase)).Any() &&
                                Values.Values.Where(v => v.ConvertTarget.IsAssignableTo(vp.PropertyType) || v is JsonArray).Any()
                            )
                            .Count() == Values.Keys.Count;
                    if (compatibleProperties)
                    {
                        potentials.Add(type);
                    }
                }

                if (potentials.Count == 1)
                {
                    SetType(potentials.First());
                    result = true;
                }
                else if (potentials.Count == 0)
                {
                    throw new JsonConversionException("Unable to derive optimal class.  Try explicitly providing a type for conversion.", Array.Empty<LogItemBase>());
                }
                else
                {
                    throw new JsonConversionException("Multiple classes perfectly match the provided Json.  Try explicitly providing a type for conversion.", Array.Empty<LogItemBase>());
                }
            }

            // because JsonArrays always make their type a generic array,
            // once we know what the actual property type is, set it on the array
            if (result)
            {
                // get all enumeration properties
                var props = TypeHelper.GetValidProperties(ConvertTarget)
                    .Where(p => p.PropertyType != typeof(string) &&
                                p.PropertyType.GetInterfaces()
                                .Where(t => t.Name == typeof(IEnumerable).Name)
                                .Any())
                    .ToArray();

                // set that type as the corresponding json array's type
                foreach (var p in props)
                {
                    Values[p.Name].SetType(p.PropertyType);
                }
            }
            return result;
        }

        /// <summary>
        /// Returns the object the JsonBase represents
        /// </summary>
        /// <returns></returns>
        public override object AsObject()
        {
            object result = null;
            if (ConvertTarget != null)
            {
                result = GetInstance();
                var props = TypeHelper.GetValidProperties(ConvertTarget);
                foreach (var p in props)
                {
                    p.SetValue(result, Values[p.Name].AsObject());
                }
            }

            return result;
        }

        public override string ToString()
        {
            return $"Count: {Values.Count}, Type: {ConvertTarget?.Name}";
        }
    }
}
