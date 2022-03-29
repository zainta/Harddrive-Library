// Copyright (c) Zain Al-Ahmary.  All rights reserved.
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
    /// Stores json enumerations' properties
    /// </summary>
    class JsonArray : JsonBase
    {
        /// <summary>
        /// The represented array's value
        /// </summary>
        public List<JsonBase> Values { get; private set; }

        /// <summary>
        /// Creates an instance
        /// </summary>
        /// <param name="includeFields">Whether or not the JsonBag will include permitted fields</param>
        public JsonArray()
        {
            JsonContainerName = "JsonArray";
            Values = new List<JsonBase>();
        }

        /// <summary>
        /// Returns the JsonBase derivation as a json string
        /// </summary>
        /// <returns></returns>
        public override string AsJson()
        {
            var result = new StringBuilder("[");

            var first = true;
            foreach (var v in Values)
            {
                if (first)
                {
                    first = false;
                }
                else
                {
                    result.Append(",");
                }

                result.Append(v.AsJson());
            }
            result.Append("]");

            return result.ToString();
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
                Parallel.ForEach(Values, (jb) =>
                {
                    var r = jb.Evaluate();
                    childSuccesses.Add(r);
                });
            }
            else
            {
                foreach (var jb in Values)
                {
                    var r = jb.Evaluate();
                    childSuccesses.Add(r);
                    if (!r)
                    {
                        break;
                    }
                }
            }

            // only continue the type assessment if all children successfully assessed themselves
            if (!childSuccesses.Where(cs => !cs).Any())
            {
                // get the common base type for all of this JsonArray's content
                var contentTypes = (from jb in Values select jb.ConvertTarget).ToArray();
                Type averageContentType = contentTypes.Length == 0 ? typeof(object) : TypeHelper.GetAverageType(contentTypes);

                var type = Array.CreateInstance(averageContentType, 0).GetType();
                SetType(type);
                result = true;
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
                Parallel.ForEach(Values, (jb) =>
                {
                    var r = jb.Evaluate();
                    childSuccesses.Add(r);
                });
            }
            else
            {
                foreach (var jb in Values)
                {
                    var r = jb.Evaluate();
                    childSuccesses.Add(r);
                    if (!r)
                    {
                        break;
                    }
                }
            }

            // only continue the type assessment if all children successfully assessed themselves
            if (!childSuccesses.Where(cs => !cs).Any())
            {
                // get the common base type for all of this JsonArray's content
                var contentTypes = (from jb in Values select jb.ConvertTarget).ToArray();
                Type averageContentType = contentTypes.Length == 0 ? typeof(object) : TypeHelper.GetAverageType(contentTypes);
                var arrayType = Array.CreateInstance(averageContentType, 0).GetType();

                // rather than blindly use the average type, 
                // check to see if the average type can contain the desired type
                if (arrayType.IsAssignableTo(type))
                {
                    SetType(arrayType);
                    result = true;
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
                if (ConvertTarget.IsArray)
                {
                    for (int i = 0; i < Values.Count; i++)
                    {
                        ((IList)result)[i] = Values[i].AsObject();
                    }
                }
                else
                {
                    foreach (var val in Values)
                    {
                        ((IList)result).Add(val.AsObject());
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Returns an instance of ConvertTarget
        /// </summary>
        /// <returns></returns>
        protected override object GetInstance()
        {
            if (ConvertTarget.IsArray)
            {
                return Array.CreateInstance(ConvertTarget.GetElementType(), Values.Count);
            }
            else
            {
                return base.GetInstance();
            }
        }

        public override string ToString()
        {
            return $"Count: {Values.Count}, Type: {ConvertTarget?.Name}";
        }
    }
}
