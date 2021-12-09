// Copyright (c) Zain Al-Ahmary.  All rights reserved.
// Licensed under the MIT License, (the "License"); you may not use this file except in compliance with the License. 
// You may obtain a copy of the License at https://mit-license.org/

using HDDL.Data;
using System;

namespace HDDL.Language.HDSL
{
    /// <summary>
    /// Encapsulates the context of a query for use during filtration clause exploration
    /// </summary>
    class ClauseContext
    {
        /// <summary>
        /// The data handler currently in use
        /// </summary>
        public DataHandler Data { get; set; }

        /// <summary>
        /// The type being queried
        /// </summary>
        public Type QueriedType { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="dh"></param>
        /// <param name="queriedType"></param>
        public ClauseContext(DataHandler dh, Type queriedType)
        {
            Data = dh;
            QueriedType = queriedType;
        }
    }
}
