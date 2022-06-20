// Copyright (c) Zain Al-Ahmary.  All rights reserved.
// Licensed under the MIT License, (the "License"); you may not use this file except in compliance with the License. 
// You may obtain a copy of the License at https://mit-license.org/

using HDDL.Language.Json.Reflection;
using System;
using System.Reflection;
using System.Text;

namespace HDDL.Language.Json.Conversion
{
    /// <summary>
    /// Stores an object and its actual type
    /// </summary>
    class ValueTypeQuantity : JsonBase
    {
        /// <summary>
        /// The kind of object stored
        /// </summary>
        public Type Kind { get; set; }

        /// <summary>
        /// The value stored
        /// </summary>
        public object Value { get; set; }

        /// <summary>
        /// Create an instance
        /// </summary>
        /// <param name="obj">The value to store.  Stores its type as Kind</param>
        /// <exception cref="ArgumentException" />
        public ValueTypeQuantity(object obj)
        {
            JsonContainerName = "ValueTypeQuantity";
            Value = obj;
            Kind = obj?.GetType();
        }

        /// <summary>
        /// Create an intance
        /// </summary>
        /// <param name="prop">The property meta object</param>
        /// <param name="obj">The instance of the object the property should be pulled from</param>
        public ValueTypeQuantity(PropertyInfo prop, object obj)
        {
            JsonContainerName = "ValueTypeQuantity";
            Value = prop.GetValue(obj);
            Kind = prop.PropertyType;
        }

        /// <summary>
        /// Create an instance
        /// </summary>
        /// <param name="obj">The value to store.</param>
        /// <param name="type">The type of object to store</param>
        /// <exception cref="ArgumentException" />
        public ValueTypeQuantity(object obj, Type type)
        {
            if (obj.GetType() != type) throw new ArgumentException($"Object parameter is not of type '{type.FullName}'.");

            Value = obj;
            Kind = type;
        }

        /// <summary>
        /// Assigns the JsonBase derivation a type to convert into
        /// </summary>
        /// <param name="typeTarget">The type the JsonBase derivation will convert into</param>
        public override void SetType(Type typeTarget)
        {
            base.SetType(typeTarget);
            Kind = typeTarget;
        }

        /// <summary>
        /// Returns the ValueTypeQuantity as a json string
        /// </summary>
        /// <param name="appendTypeProperty">Whether or not JSON should include the $type property</param>
        /// <returns></returns>
        public override string AsJson(bool appendTypeProperty)
        {
            var result = new StringBuilder();
            if (Value is IConvertible)
            {
                if (Value is string)
                {
                    var str = (string)Value;
                    result.Append($"\"{str.Replace("\\", "\\\\")}\"");
                }
                else if (Value is bool || Value is DateTime || Value is TimeSpan)
                {
                    result.Append($"\"{Value}\"");
                }
                else if (Value is Enum)
                {
                    result.Append((int)Value);
                }
                else
                {
                    result.Append(Value);
                }
            }
            else if (Value is Guid)
            {
                result.Append($"\"{Value}\"");
            }

            return result.ToString();
        }

        /// <summary>
        /// Returns the object the JsonBase represents
        /// </summary>
        /// <returns></returns>
        public override object AsObject()
        {
            return TypeHelper.ConvertTo(Value, Kind);
        }

        public override string ToString()
        {
            if (Value is string || Value is Guid || Value is bool)
            {
                return $"{Value.GetType().Name}, {Kind}, '{Value}'";
            }
            else
            {
                return $"{Value.GetType().Name}, {Kind}, {Value}";
            }
        }
    }
}
