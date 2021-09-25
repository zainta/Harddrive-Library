// Copyright (c) Zain Al-Ahmary.  All rights reserved.
// Licensed under the MIT License, (the "License"); you may not use this file except in compliance with the License. 
// You may obtain a copy of the License at https://mit-license.org/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HDDL.IO.Parameters
{
    /// <summary>
    /// Defines a parameter shortcut (a single string parameter prefixed by a set of alpha-numeric characters)
    /// 
    /// Note that shortcuts use custom paired notation for content containing spaces.  
    /// That is, for example, 
    /// fr' test testing testiness' will come into the program is 4 parameters, 
    /// but the shortcut (configured to accept single quotes as its pair) will 
    /// automatically gather all of it together.
    /// </summary>
    public class ParameterRuleShortcut : ParameterRuleBase
    {
        /// <summary>
        /// The parameter prefix used to define
        /// </summary>
        public string Prefix { get; private set; }

        /// <summary>
        /// The paired item to use for space containing content.  
        /// 
        /// Double quotes (") are not supported since the commandline will interpret them.
        /// 
        /// The terminating pair member must be at the end of the argument subchunk.
        /// in the example: 'this isn't valid'
        /// only the training single quote will even be considered for termination.
        /// </summary>
        public char PairedNotation { get; private set; }

        /// <summary>
        /// Create a shortcut rule.
        /// </summary>
        /// <param name="flagOpeners">The text used to define a flag (typically '-' or '\', but sometimes can be things like '--').  Defaults to '-'</param>
        /// <param name="pairedNotation">The characters to use for content designation</param>
        /// <param name="prefix">The text to directly precede the paired characters</param>
        public ParameterRuleShortcut(string prefix, char pairedNotation, params string[] flagOpeners) : base(flagOpeners)
        {
            Prefix = prefix;
            PairedNotation = pairedNotation;
        }

        /// <summary>
        /// Create a shortcut rule with the default paired designator (').
        /// </summary>
        /// <param name="flagOpeners">The text used to define a flag (typically '-' or '\', but sometimes can be things like '--').  Defaults to '-'</param>
        /// <param name="pairedNotation">The characters to use for content designation</param>
        /// <param name="prefix">The text to directly precede the paired characters</param>
        public ParameterRuleShortcut(string prefix, params string[] flagOpeners) : this(prefix, '\'', flagOpeners)
        {

        }

        /// <summary>
        /// Combs through the provided parameters and returns all unconsumed items
        /// </summary>
        /// <param name="args">The arguments to comb through</param>
        /// <returns>Any unconsumed arguments</returns>
        /// <exception cref="ArgumentException">Thrown if no terminating pair is found</exception>
        public override string[] Comb(string[] args)
        {
            // Calculates all valid appearances of the shortcut
            var possibleShortcuts = (from opener in FlagDesignators
                                     select $"{opener}{Prefix}")
                                   .ToList();
            // Shortcuts do not need to use an opener.  That is one of their purposes.
            possibleShortcuts.Add(Prefix);

            // arguments that were not consumed
            var leftovers = new List<string>();

            var content = new StringBuilder();

            for (int i = 0; i < args.Length; i++)
            {
                var shortcut = (from ps in possibleShortcuts where args[i].StartsWith($"{ps}{PairedNotation}") select ps).SingleOrDefault();
                if (shortcut != null)
                {
                    // indicates whether or not we have found the closing pair designator
                    var more = args[i].EndsWith($"\\{PairedNotation}") || !args[i].EndsWith($"{PairedNotation}");

                    var rangeStart = $"{shortcut}{PairedNotation}".Length;
                    var rangeEnd = !more ? args[i].Length - rangeStart - 1 : args[i].Length - rangeStart;
                    content.Append(args[i].Substring(rangeStart, rangeEnd));
                    i++;

                    if (more)
                    {
                        // now find the rest of the paired run
                        while (more && (i < args.Length))
                        {
                            // if the parameter ends with an unescaped pair terminator, we are done.
                            if (!args[i].EndsWith($"\\{PairedNotation}") && args[i].EndsWith($"{PairedNotation}"))
                            {
                                content.Append($" {args[i].Substring(0, args[i].Length - 1)}");
                                more = false;
                            }
                            else
                            {
                                content.Append($" {args[i]}");
                            }
                        }

                        // this means we've run out of arguments, not found our match.
                        if (more)
                        {
                            throw new ArgumentException($"Closing paired character not found. ({PairedNotation})");
                        }
                    }

                    Arguments[$"{Prefix}_0"] = content.ToString();
                }
                else
                {
                    leftovers.Add(args[i]);
                }
            }

            return leftovers.ToArray();
        }

        public override string GetProspects()
        {
            return $"{ParameterHandler.ProspectStringSeperator}{Prefix}"; ;
        }
    }
}
