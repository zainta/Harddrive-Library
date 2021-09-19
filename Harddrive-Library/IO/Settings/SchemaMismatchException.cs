// Copyright (c) Zain Al-Ahmary.  All rights reserved.
// Licensed under the MIT License, (the "License"); you may not use this file except in compliance with the License. 
// You may obtain a copy of the License at https://mit-license.org/

using System;

namespace HDDL.IO.Settings
{
    /// <summary>
    /// Thrown when the IniFileManager is performing a fill and the schema and the file do not match in structure
    /// </summary>
    public class SchemaMismatchException : Exception
    {
        public SchemaMismatchException(string message) : base(message)
        {

        }
    }
}
