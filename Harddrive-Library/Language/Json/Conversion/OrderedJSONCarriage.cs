// Copyright (c) Zain Al-Ahmary.  All rights reserved.
// Licensed under the MIT License, (the "License"); you may not use this file except in compliance with the License. 
// You may obtain a copy of the License at https://mit-license.org/

namespace HDDL.Language.Json.Conversion
{
    /// <summary>
    /// Tracks a Json string and its index in a list
    /// </summary>
    class OrderedJSONCarriage
    {
        public int Index { get; set; }

        public string Json { get; set; }

        public OrderedJSONCarriage(int index, string json)
        {
            Index = index;
            Json = json;
        }
    }
}
