// Copyright (c) Zain Al-Ahmary.  All rights reserved.
// Licensed under the MIT License, (the "License"); you may not use this file except in compliance with the License. 
// You may obtain a copy of the License at https://mit-license.org/

using System;

namespace HDDL.HDSL.Where
{
    /// <summary>
    /// Thrown when a two WherreValues with different value types are compared
    /// </summary>
    public class TypeMismatchException : Exception
    {
        public TypeMismatchException(string message) : base(message)
        {

        }
    }
}
