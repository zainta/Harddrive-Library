// Copyright (c) Zain Al-Ahmary.  All rights reserved.
// Licensed under the MIT License, (the "License"); you may not use this file except in compliance with the License. 
// You may obtain a copy of the License at https://mit-license.org/

using HDDL.Language.Json.Reflection;
using System;
using System.Reflection;

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
        /// Returns the JsonBase derivation as a json string
        /// </summary>
        /// <returns></returns>
        public override string AsJson()
        {
            return JsonBagHandler.GetItemJson(Value);
        }

        /// <summary>
        /// Determines the appropriate type to convert the derivation into
        /// </summary>
        /// <returns></returns>
        public override bool Evaluate()
        {
            if (Kind != null)
            {
                SetType(Kind);
                return true;
            }
            
            return false;
        }

        /// <summary>
        /// Takes a type and determines if it is a potential match for the derived type
        /// </summary>
        /// <param name="type">The type to evaluate</param>
        /// <returns></returns>
        public override bool Evaluate(Type type)
        {
            return type != null && Kind == type;
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
                return $"{Value.GetType().Name}, '{Value}'";
            }
            else
            {
                return $"{Value.GetType().Name}, {Value}";
            }
        }
    }
}
