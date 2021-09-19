// Copyright (c) Zain Al-Ahmary.  All rights reserved.
// Licensed under the MIT License, (the "License"); you may not use this file except in compliance with the License. 
// You may obtain a copy of the License at https://mit-license.org/

using HDDL.Data;
using HDDL.HDSL.Logging;
using System.Collections.Generic;
using System.Linq;

namespace HDDL.HDSL
{
    /// <summary>
    /// Represents the results of an HDSL code execution
    /// </summary>
    public class HDSLResult
    {
        /// <summary>
        /// The paths that matched the query
        /// </summary>
        public DiskItem[] Results { get; private set; }

        /// <summary>
        /// Any errors encountered during the process
        /// </summary>
        public HDSLLogBase[] Errors { get; private set; }

        /// <summary>
        /// Creates a success result with the resulting paths as its contents
        /// </summary>
        /// <param name="paths">The records matching the query</param>
        public HDSLResult(IEnumerable<DiskItem> items)
        {
            Results = items.ToArray();
            Errors = new HDSLLogBase[] { };
        }

        /// <summary>
        /// Creates an error result with the errors encountered
        /// </summary>
        /// <param name="errors">The errors encountered during execution</param>
        public HDSLResult(IEnumerable<HDSLLogBase> errors)
        {
            Results = new DiskItem[] { };
            Errors = errors.ToArray();
        }
    }
}
