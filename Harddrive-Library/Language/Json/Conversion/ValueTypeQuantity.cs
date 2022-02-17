// Copyright (c) Zain Al-Ahmary.  All rights reserved.
// Licensed under the MIT License, (the "License"); you may not use this file except in compliance with the License. 
// You may obtain a copy of the License at https://mit-license.org/

using System;

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
            if (obj == null) throw new ArgumentException("Cannot pass null");

            JsonContainerName = "ValueTypeQuantity";
            Value = obj;
            Kind = obj.GetType();
        }

        /// <summary>
        /// Create an instance
        /// </summary>
        /// <param name="obj">The value to store.</param>
        /// <param name="type">The type of object to store</param>
        /// <exception cref="ArgumentException" />
        public ValueTypeQuantity(object obj, Type type)
        {
            if (obj.GetType() != type) throw new ArgumentException("Object parameter is not of type Type.");

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
        /// Attempts to determine the type of the JsonBase derivation
        /// </summary>
        /// <returns>True upon complete success, false otherwise</returns>
        /// <exception cref="JsonConversionException"></exception>
        public override bool DetermineType()
        {
            SetType(Kind);
            return true;
        }

        /// <summary>
        /// Returns the object the JsonBase represents
        /// </summary>
        /// <returns></returns>
        public override object AsObject()
        {
            return Value;
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
