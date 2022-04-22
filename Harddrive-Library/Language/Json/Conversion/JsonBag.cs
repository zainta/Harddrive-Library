﻿// Copyright (c) Zain Al-Ahmary.  All rights reserved.
// Licensed under the MIT License, (the "License"); you may not use this file except in compliance with the License. 
// You may obtain a copy of the License at https://mit-license.org/

using HDDL.Language.Json.Reflection;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        /// Determines the appropriate type to convert the derivation into
        /// </summary>
        /// <param name="root">Indicates if this is the root call</param>
        /// <returns></returns>
        public override bool Evaluate(bool root = false)
        {
            var result = false;

            // evaluate children first
            var childSuccesses = new List<bool>();
            if (root)
            {
                Parallel.ForEach(Values.Values, (jb) =>
                {
                    if (jb is JsonBag || jb is JsonArray)
                    {
                        var r = jb.Evaluate();
                        childSuccesses.Add(r);
                    }
                    else if (jb is ValueTypeQuantity)
                    {
                        // value type quantities are not evaluated, and so always succeed
                        childSuccesses.Add(true);
                    }
                });
            }
            else
            {
                foreach (var jb in Values.Values)
                {
                    if (jb is JsonBag || jb is JsonArray)
                    {
                        var r = jb.Evaluate();
                        childSuccesses.Add(r);
                    }
                    else if (jb is ValueTypeQuantity)
                    {
                        // value type quantities are not evaluated, and so always succeed
                        childSuccesses.Add(true);
                    }
                }
            }

            // only continue the type assessment if the majority of children successfully assessed themselves
            if (childSuccesses.Where(csResult => !csResult).Count() < childSuccesses.Count)
            {
                // assess every relevant type and keep the one(s) that match(es)
                // for a type to match, it must:
                //      the names must match
                //      all properties must be accounted for
                //      all property types must be assignable or null(and the target property be nullable)
                var potentials = new List<Type>();
                var relevant = TypeHelper.GetRelevantTypes(this);
                foreach (var type in relevant)
                {
                    // for the given type, assess whether its properties match a property on the JsonBag and has an assignable type

                    // to do that,
                    // compare all of the potential type's properties to all of the JsonBag's properties to see if they match
                    // * (a match being either a one to one match on property type and name, or a name match with a null value on a nullable property)
                    // then take the number that match and check it against the total number of the JsonBag's properties to make sure that they *all* match
                    // if they do then that's a potential (it's a full match, really)

                    // note that if the value is a JsonArray then it is ignored because all JsonArray automatically assume they will be arrays.
                    // this means they can be passed as the initialization parameter to the actual target enumeration
                    var validProps = TypeHelper.GetValidProperties(type);

                    // vp = valid prop
                    // pp = potential prop
                    var matchingProperties =
                        (
                            from vp in validProps
                            from pp in Values
                            where
                                // names match
                                pp.Key.Equals(vp.Name, StringComparison.InvariantCultureIgnoreCase) &&
                                // for JsonBag and JsonArray properties
                                (
                                    ((pp.Value is JsonArray || pp.Value is JsonBag) &&
                                    (
                                        // if potential prop is null and valid prop is nullable
                                        (
                                            pp.Value.ConvertTarget == null &&
                                            !vp.PropertyType.IsValueType || (Nullable.GetUnderlyingType(vp.PropertyType) != null)
                                        ) ||
                                        // or the potential prop can be assigned to the valid prop
                                        pp.Value.ConvertTarget.IsAssignableTo(vp.PropertyType) ||
                                        // or the potential prop is a jsonarray
                                        pp.Value is JsonArray
                                    )) ||
                                    (pp.Value is ValueTypeQuantity &&
                                    (
                                        // if potential prop is null and valid prop is nullable
                                        (
                                            pp.Value.ConvertTarget == null &&
                                            !vp.PropertyType.IsValueType || (Nullable.GetUnderlyingType(vp.PropertyType) != null)
                                        ) ||
                                        // or the potential prop can be converted to the valid prop's type
                                        TypeHelper.TryConvertTo(((ValueTypeQuantity)pp.Value).Value, vp.PropertyType)
                                    ))
                                )
                            select pp
                        );

                    if (matchingProperties.Count() == validProps.Length)
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

            // 
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
        /// Takes a type and determines if it is a potential match for the derived type
        /// </summary>
        /// <param name="type">The type to evaluate</param>
        /// <param name="root">Indicates if this is the root call</param>
        /// <returns></returns>
        public override bool Evaluate(Type type, bool root = false)
        {
            var result = false;

            // evaluate children first
            var childSuccesses = new List<bool>();
            if (root)
            {
                Parallel.ForEach(Values.Values, (jb) =>
                {
                    if (jb is JsonBag || jb is JsonArray)
                    {
                        var r = jb.Evaluate();
                        childSuccesses.Add(r);
                    }
                    else if (jb is ValueTypeQuantity)
                    {
                        // value type quantities are not evaluated, and so always succeed
                        childSuccesses.Add(true);
                    }
                });
            }
            else
            {
                foreach (var jb in Values.Values)
                {
                    if (jb is JsonBag || jb is JsonArray)
                    {
                        var r = jb.Evaluate();
                        childSuccesses.Add(r);
                    }
                    else if (jb is ValueTypeQuantity)
                    {
                        // value type quantities are not evaluated, and so always succeed
                        childSuccesses.Add(true);
                    }
                }
            }

            // only continue the type assessment if the majority of children successfully assessed themselves
            if (childSuccesses.Where(csResult => !csResult).Count() < childSuccesses.Count)
            {
                // assess every relevant type and keep the one(s) that match(es)
                // for a type to match, it must:
                //      the names must match
                //      all properties must be accounted for
                //      all property types must be assignable or null(and the target property be nullable)
                var potentials = new List<Type>();
                var relevant = new Type[] { type };
                foreach (var t in relevant)
                {
                    // for the given type, assess whether its properties match a property on the JsonBag and has an assignable type

                    // to do that,
                    // compare all of the potential type's properties to all of the JsonBag's properties to see if they match
                    // * (a match being either a one to one match on property type and name, or a name match with a null value on a nullable property)
                    // then take the number that match and check it against the total number of the JsonBag's properties to make sure that they *all* match
                    // if they do then that's a potential (it's a full match, really)

                    // note that if the value is a JsonArray then it is ignored because all JsonArray automatically assume they will be arrays.
                    // this means they can be passed as the initialization parameter to the actual target enumeration
                    var validProps = TypeHelper.GetValidProperties(t);

                    // vp = valid prop
                    // pp = potential prop
                    var matchingProperties =
                        (
                            from vp in validProps
                            from pp in Values
                            where
                                // names match
                                pp.Key.Equals(vp.Name, StringComparison.InvariantCultureIgnoreCase) &&
                                // for JsonBag and JsonArray properties
                                (
                                    ((pp.Value is JsonArray || pp.Value is JsonBag) &&
                                    (
                                        // if potential prop is null and valid prop is nullable
                                        (
                                            pp.Value.ConvertTarget == null &&
                                            !vp.PropertyType.IsValueType || (Nullable.GetUnderlyingType(vp.PropertyType) != null)
                                        ) ||
                                        // or the potential prop can be assigned to the valid prop
                                        pp.Value.ConvertTarget.IsAssignableTo(vp.PropertyType) ||
                                        // or the potential prop is a jsonarray
                                        pp.Value is JsonArray
                                    )) ||
                                    (pp.Value is ValueTypeQuantity &&
                                    (
                                        // if potential prop is null and valid prop is nullable
                                        (
                                            pp.Value.ConvertTarget == null &&
                                            !vp.PropertyType.IsValueType || (Nullable.GetUnderlyingType(vp.PropertyType) != null)
                                        ) ||
                                        // or the potential prop can be converted to the valid prop's type
                                        TypeHelper.TryConvertTo(((ValueTypeQuantity)pp.Value).Value, vp.PropertyType)
                                    ))
                                )
                            select pp
                        );

                    if (matchingProperties.Count() == validProps.Length)
                    {
                        potentials.Add(t);
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

            // 
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
                    Values[p.Name].SetType(p.PropertyType);
                    p.SetValue(result, Values[p.Name].AsObject());
                }
            }

            return result;
        }

        public override string ToString()
        {
            return $"Count: {Values.Count}, Type: {ConvertTarget?.Name}";
        }

        /// <summary>
        /// Returns a structure-derived string that should be identical across any record of the same type (not intended to be unique across all types)
        /// </summary>
        /// <returns></returns>
        public string GetKeyString()
        {
            var sb = new StringBuilder();
            foreach (var v in Values)
            {
                if (sb.Length > 0)
                {
                    sb.Append(",");
                }
                sb.Append(v.Key);
            }

            return $"[{sb}]";
        }
    }
}
