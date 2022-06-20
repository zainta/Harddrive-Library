// Copyright (c) Zain Al-Ahmary.  All rights reserved.
// Licensed under the MIT License, (the "License"); you may not use this file except in compliance with the License. 
// You may obtain a copy of the License at https://mit-license.org/

using System;
using System.Text;

namespace HDDL.Language.Json.Conversion
{
    /// <summary>
    /// Root class for all json storage classes
    /// </summary>
    abstract class JsonBase
    {
        /// <summary>
        /// The type the JsonBase derivation will convert into
        /// </summary>
        public Type ConvertTarget { get; private set; }

        /// <summary>
        /// Debug assist property
        /// </summary>
        public string JsonContainerName { get; protected set; }

        /// <summary>
        /// Returns the JsonBase derivation as a json string
        /// </summary>
        /// <param name="appendTypeProperty">Whether or not JSON should include the $type property</param>
        /// <returns></returns>
        public virtual string AsJson(bool appendTypeProperty)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Returns the object the JsonBase represents
        /// </summary>
        /// <returns></returns>
        public virtual object AsObject()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Assigns the JsonBase derivation a type to convert into
        /// </summary>
        /// <param name="typeTarget">The type the JsonBase derivation will convert into</param>
        public virtual void SetType(Type typeTarget)
        {
            ConvertTarget = typeTarget;
        }

        /// <summary>
        /// Returns an instance of ConvertTarget
        /// </summary>
        /// <returns></returns>
        protected virtual object GetInstance()
        {
            return Activator.CreateInstance(ConvertTarget);
        }

        /// <summary>
        /// Returns a structure-derived string that should be identical across any record of the same type (not intended to be unique across all types)
        /// </summary>
        /// <returns></returns>
        public virtual string GetKeyString()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Adds a correctly comma ended $type property to the given string builder based on the number of items indicated in contentItemCount
        /// </summary>
        /// <param name="sb">The string builder to modify</param>
        /// <param name="type">The value to use in the type parameter</param>
        /// <param name="contentItemCount">The number of content items will be stored in the json object/array</param>
        protected void ProperlyAddType(StringBuilder sb, Type type, int contentItemCount)
        {
            var typeName = type.FullName;

            sb.Append($"\"$type\":\"{typeName}\"");
            if (contentItemCount > 0)
            {
                sb.Append(",");
            }
        }
    }
}
