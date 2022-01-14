// Copyright (c) Zain Al-Ahmary.  All rights reserved.
// Licensed under the MIT License, (the "License"); you may not use this file except in compliance with the License. 
// You may obtain a copy of the License at https://mit-license.org/

using System;

namespace HDDL.Language.Json
{
    /// <summary>
    /// Used to tell the Json system to ignore a type during deserialization, or ignore a property or field during serialization.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public class JsonIgnoreAttribute : Attribute
    {

    }
}
