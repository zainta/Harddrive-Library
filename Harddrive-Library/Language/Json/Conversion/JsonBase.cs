// Copyright (c) Zain Al-Ahmary.  All rights reserved.
// Licensed under the MIT License, (the "License"); you may not use this file except in compliance with the License. 
// You may obtain a copy of the License at https://mit-license.org/

using System;

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
        /// <returns></returns>
        public virtual string AsJson()
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
        public void SetType(Type typeTarget)
        {
            ConvertTarget = typeTarget;
        }

        /// <summary>
        /// Attempts to determine the type of the JsonBase derivation
        /// </summary>
        /// <returns>True upon complete success, false otherwise</returns>
        /// <exception cref="JsonConversionException"></exception>
        public virtual bool DetermineType()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Returns an instance of ConvertTarget
        /// </summary>
        /// <returns></returns>
        protected virtual object GetInstance()
        {
            return Activator.CreateInstance(ConvertTarget);
        }
    }
}
