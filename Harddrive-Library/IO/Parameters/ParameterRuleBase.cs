// Copyright (c) Zain Al-Ahmary.  All rights reserved.
// Licensed under the MIT License, (the "License"); you may not use this file except in compliance with the License. 
// You may obtain a copy of the License at https://mit-license.org/

using System;
using System.Collections.Generic;

namespace HDDL.IO.Parameters
{
    /// <summary>
    /// Base for all parameter rules.
    /// </summary>
    public abstract class ParameterRuleBase
    {
        /// <summary>
        /// The rule's identifier
        /// </summary>
        internal Guid Id { get; private set; }

        /// <summary>
        /// The flag's contents after loading
        /// </summary>
        public Dictionary<string, string> Arguments { get; private set; }

        /// <summary>
        /// The text used to define a flag (typically '-' or '\', but sometimes can be things like '--')
        /// </summary>
        public List<string> FlagDesignators { get; private set; }

        /// <summary>
        /// Create a base parameter rule
        /// </summary>
        /// <param name="flagOpeners">The text used to define a flag (typically '-' or '\', but sometimes can be things like '--').  Defaults to '-'</param>
        public ParameterRuleBase(params string[] flagOpeners)
        {
            Arguments = new Dictionary<string, string>();
            FlagDesignators = new List<string>(flagOpeners);
            Id = Guid.NewGuid();
        }

        /// <summary>
        /// Combs through the provided parameters and returns all unconsumed parameters
        /// </summary>
        /// <param name="args">The arguments to comb through</param>
        /// <returns>Any unconsumed arguments</returns>
        public virtual string[] Comb(string[] args)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Returns the Rule's target arguments
        /// </summary>
        /// <returns>A string containing the rule's target arguments</returns>
        public virtual string GetProspects()
        {
            throw new NotImplementedException();
        }
    }
}
