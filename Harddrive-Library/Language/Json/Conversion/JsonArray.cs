// Copyright (c) Zain Al-Ahmary.  All rights reserved.
// Licensed under the MIT License, (the "License"); you may not use this file except in compliance with the License. 
// You may obtain a copy of the License at https://mit-license.org/

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
        /// caches the JsonArray's structure key
        /// </summary>
        private string _key;

        /// <summary>
        /// Creates an instance
        /// </summary>
        /// <param name="includeFields">Whether or not the JsonBag will include permitted fields</param>
        public JsonArray()
        {
            JsonContainerName = "JsonArray";
            Values = new List<JsonBase>();
            _key = null;
        }

        /// <summary>
        /// Returns the JsonBag as a json string
        /// </summary>
        /// <param name="appendTypeProperty">Whether or not JSON should include the $type property</param>
        /// <returns></returns>
        public override string AsJson(bool appendTypeProperty)
        {
            var result = new StringBuilder("[");
            if (appendTypeProperty)
            {
                ProperlyAddType(result, ConvertTarget, Values.Count);
            }
            result.Append(
                string.Join(",", from v in Values select v.AsJson(appendTypeProperty))
                );
            result.Append("]");

            return result.ToString();
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

        /// <summary>
        /// Returns a structure-derived string that should be identical across any record of the same type (not intended to be unique across all types)
        /// </summary>
        /// <returns></returns>
        public override string GetKeyString()
        {
            if (string.IsNullOrWhiteSpace(_key))
            {
                // get all key strings from all content
                // in ascending order
                var keys =
                    (
                        from v in Values
                        select v.GetKeyString()
                    ).Distinct()
                    .OrderBy(v => v);

                var sb = new StringBuilder();
                foreach (var k in keys)
                {
                    if (sb.Length > 0)
                    {
                        sb.Append(";");
                    }
                    sb.Append(k);
                }

                _key = $"<{sb}>";
            }

            return _key;
        }
    }
}
