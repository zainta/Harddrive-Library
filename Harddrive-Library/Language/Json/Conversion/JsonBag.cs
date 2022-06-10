// Copyright (c) Zain Al-Ahmary.  All rights reserved.
// Licensed under the MIT License, (the "License"); you may not use this file except in compliance with the License. 
// You may obtain a copy of the License at https://mit-license.org/

using HDDL.Language.Json.Reflection;
using System.Collections.Generic;
using System.Text;

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
                    //Values[p.Name].SetType(p.PropertyType);
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
        public override string GetKeyString()
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
