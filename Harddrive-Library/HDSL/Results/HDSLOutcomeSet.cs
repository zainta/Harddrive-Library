// Copyright (c) Zain Al-Ahmary.  All rights reserved.
// Licensed under the MIT License, (the "License"); you may not use this file except in compliance with the License. 
// You may obtain a copy of the License at https://mit-license.org/

using HDDL.HDSL.Logging;
using System.Collections.Generic;
using System.Linq;

namespace HDDL.HDSL.Results
{
    /// <summary>
    /// Represents the results of an HDSL code execution
    /// </summary>
    public class HDSLOutcomeSet
    {
        private List<HDSLOutcome> _results;
        /// <summary>
        /// A set of all query results from the script
        /// </summary>
        public IReadOnlyList<HDSLOutcome> Results
        {
            get
            {
                return _results.AsReadOnly();
            }
        }

        /// <summary>
        /// Any errors encountered during the process
        /// </summary>
        public HDSLLogBase[] Errors { get; private set; }

        /// <summary>
        /// Creates a success result with the resulting items as its contents
        /// </summary>
        /// <param name="paths">The records matching the query</param>
        public HDSLOutcomeSet(IEnumerable<HDSLOutcome> results)
        {
            _results = new List<HDSLOutcome>(results);
            Errors = new HDSLLogBase[] { };
        }

        /// <summary>
        /// Creates an empty result
        /// </summary>
        public HDSLOutcomeSet()
        {
            _results = new List<HDSLOutcome>();
            Errors = new HDSLLogBase[] { };
        }

        /// <summary>
        /// Creates an error result with the errors encountered
        /// </summary>
        /// <param name="errors">The errors encountered during execution</param>
        public HDSLOutcomeSet(IEnumerable<HDSLLogBase> errors)
        {
            _results = new List<HDSLOutcome>();
            Errors = errors.ToArray();
        }

        /// <summary>
        /// Adds a result set to the results
        /// </summary>
        /// <param name="result">The result set to add</param>
        public void Add(HDSLOutcome result)
        {
            _results.Add(result);
        }
    }
}
