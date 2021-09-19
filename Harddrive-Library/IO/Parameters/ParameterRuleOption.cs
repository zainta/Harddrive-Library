// Copyright (c) Zain Al-Ahmary.  All rights reserved.
// Licensed under the MIT License, (the "License"); you may not use this file except in compliance with the License. 
// You may obtain a copy of the License at https://mit-license.org/

using System;
using System.Collections.Generic;
using System.Linq;

namespace HDDL.IO.Parameters
{
    /// <summary>
    /// Defines a parameter option (an argument that can take parameters)
    /// </summary>
    public class ParameterRuleOption : ParameterRuleBase
    {
        /// <summary>
        /// The text component of this option (ie, for "-path:" Option should be "path").  Used as the key to retrieve the parameter's value from the ParameterHandler, too
        /// </summary>
        public string Option { get; private set; }

        /// <summary>
        /// Whether or not the option requires a colon at the end for recognition
        /// </summary>
        public bool UseColonTerminator { get; private set; }

        /// <summary>
        /// Whether or not comma seperated lists will be recognized
        /// </summary>
        public bool AcceptsCommaLists { get; private set; }

        /// <summary>
        /// Creates an option parameter definition
        /// </summary>
        /// <param name="option">The text component of this option</param>
        /// <param name="takesCommaLists">Whether or not comma seperated lists will be recognized</param>
        /// <param name="useColonTerminator">Whether or not the option requires a colon at the end for recognition</param>
        /// <param name="flagOpeners"></param>
        /// <param name="optionDefault">The option's default value (if it is not explicitly set)</param>
        public ParameterRuleOption(string option, bool takesCommaLists, bool useColonTerminator, string optionDefault, params string[] flagOpeners) : base(flagOpeners)
        {
            AcceptsCommaLists = takesCommaLists;
            Option = option;
            UseColonTerminator = useColonTerminator;
            Arguments.Add($"{Option}_0", optionDefault);
        }

        /// <summary>
        /// Combs through the provided parameters and returns all unconsumed items
        /// </summary>
        /// <param name="args">The arguments to comb through</param>
        /// <returns>Any unconsumed arguments</returns>
        public override string[] Comb(string[] args)
        {
            // Calculates all valid appearances of the option
            var possibleOptions = (from opener in FlagDesignators
                                   select $"{opener}{Option}" + (UseColonTerminator ? ":" : string.Empty))
                                   .ToArray();

            // arguments that were not consumed
            var leftovers = new List<string>();

            // indicates that we haven't obtained our value or there was a comma after the last value or before the next (current) value
            var more = true;

            // tracks the number of items
            var count = 0; 

            for (int i = 0; i < args.Length; i++)
            {
                if (possibleOptions.Contains(args[i]))
                {
                    while (more)
                    {
                        i++;

                        if (AcceptsCommaLists)
                        {
                            // We don't care if there is a comma at the beginning because we are here.
                            // Therefore, there must be a comma at the start of this argument here or at the end of the last one
                            if (!CommaEnd(args[i]) &&
                                !((args.Length - 1) > i && CommaStart(args[i + 1])))
                            {
                                more = false;
                            }
                        }
                        else
                        {
                            more = false;
                        }

                        // either way, we keep the option value
                        Arguments[$"{Option}_{count}"] = GetCommaLess(args[i]);
                        count++;
                    }
                }
                else
                {
                    leftovers.Add(args[i]);
                }
            }

            return leftovers.ToArray();
        }

        /// <summary>
        /// Strips starting and ending commas from the argument
        /// </summary>
        /// <param name="arg">The argument</param>
        /// <returns>The resulting string</returns>
        private string GetCommaLess(string arg)
        {
            arg = CommaEnd(arg) ? arg.Substring(0, arg.Length - 1) : arg;
            arg = CommaStart(arg) ? arg.Substring(1, arg.Length) : arg;

            return arg;
        }

        /// <summary>
        /// Checks to see if the given argument has a comma at the beginning
        /// </summary>
        /// <param name="arg">the argument to test</param>
        /// <returns>true if found, false otherwise</returns>
        private bool CommaStart(string arg)
        {
            return arg.StartsWith(",");
        }

        /// <summary>
        /// Checks to see if the given argument has a comma at the end
        /// </summary>
        /// <param name="arg">the argument to test</param>
        /// <returns>true if found, false otherwise</returns>
        private bool CommaEnd(string arg)
        {
            return arg.EndsWith(",");
        }

        /// <summary>
        /// Returns the Rule's target arguments
        /// </summary>
        /// <returns>A string containing the rule's target arguments</returns>
        public override string GetProspects()
        {
            return $"{ParameterHandler.ProspectStringSeperator}{Option}";
        }
    }
}
