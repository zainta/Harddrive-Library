using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HDDL.IO.Parameters
{
    /// <summary>
    /// Defines a parameter flag or flag family (a flag parameter that can accept multiple flags at once in the form -hdsl)
    /// </summary>
    public class ParameterRuleFlag : ParameterRuleBase
    {
        /// <summary>
        /// Defines the prefix used to state that a flag is nestable (ie, -hdsl)
        /// </summary>
        public static char CompoundFlagSet = '+';

        /// <summary>
        /// Accepted flags
        /// </summary>
        public List<FlagDefinition> AcceptedFlags { get; private set; }

        /// <summary>
        /// Creates a Parameter Rule to allow a specific set of flags to be valid
        /// </summary>
        /// <param name="flagList">A list of definitions</param>
        /// <param name="flagOpeners">The text used to define a flag (typically '-' or '\', but sometimes can be things like '--').  Defaults to '-'</param>
        public ParameterRuleFlag(IEnumerable<FlagDefinition> flagList, params string[] flagOpeners) : base(flagOpeners)
        {
            // check for duplicate flags within the rule
            var duplicates = (from f in flagList
                              group f by f.Flag into fG
                              orderby fG.Key
                              select fG)
                              .Where(fg => fg.Count() > 1).Any();
            if (duplicates)
            {
                throw new InvalidOperationException("Duplicate flag definitions detected!");
            }

            AcceptedFlags = new List<FlagDefinition>(flagList);

            // Set default values in Arguments
            foreach (var flagDef in AcceptedFlags)
            {
                Arguments.Add($"{flagDef.Flag}_0", flagDef.Default.ToString());
            }
        }

        /// <summary>
        /// Combs through the provided parameters and returns all unconsumed items
        /// </summary>
        /// <param name="args">The arguments to comb through</param>
        /// <returns>Any unconsumed arguments</returns>
        public override string[] Comb(string[] args)
        {
            if (args.Length == 0) return args;

            // We need to track which keys we've found
            var flagSet = new Dictionary<char, bool>();
            foreach (var flagDef in AcceptedFlags)
            {
                flagSet.Add(flagDef.Flag, false);
            }

            var parms = new List<string>(args);
            var result = new List<string>();

            for (int i = 0; i < parms.Count; i++)
            {
                var prefix = (from flag in FlagDesignators
                              where parms[i].StartsWith(flag)
                              select flag).SingleOrDefault();
                var sb = new StringBuilder();

                // if we found a prefix that we know then process the parameter
                if (prefix != null)
                {
                    var flags = parms[i].Substring(prefix.Length); // just the letters
                    var multiFlag = flags.Count() > 1; // whether or not there are multiple flags in this set

                    // go through the keys in flagset in order
                    // check to see if they are there.  If they are then 
                    // set the flag as true (found) and remove it from the string
                    for (int j = (flags.First() == CompoundFlagSet ? 1 : 0); j < flags.Length; j++)
                    {
                        // Get the definition that matches the current letter (if any)
                        var flagDef = (from fd in AcceptedFlags
                                       where 
                                            fd.Flag == flags[j] &&
                                            !flagSet[fd.Flag]
                                       select fd).SingleOrDefault();

                        if (flagDef == null || (!flagDef.Nestable && multiFlag))
                        {
                            sb.Append(flags[j]);
                            continue;
                        }

                        if (flagDef != null)
                        {
                            flagSet[flags[j]] = !flagDef.Default;
                            Arguments[$"{flags[j]}_0"] = (!flagDef.Default).ToString();
                            if (multiFlag && !sb.ToString().StartsWith(CompoundFlagSet))
                            {
                                sb.Insert(0, CompoundFlagSet);
                            }
                        }
                    }

                    if (sb.ToString() != CompoundFlagSet.ToString() &&
                    sb.Length > 0)
                    {
                        sb.Insert(0, prefix);
                        result.Add(sb.ToString());
                    }
                }
                else // if there was no recognized prefix then pass it on.
                {
                    result.Add(parms[i]);
                }
            }

            return result.ToArray();
        }

        /// <summary>
        /// Returns the Rule's target arguments
        /// </summary>
        /// <returns>A string containing the rule's target arguments</returns>
        public override string GetProspects()
        {
            var sb = new StringBuilder();

            foreach (var flag in AcceptedFlags)
            {
                // * marks the beginning of a new item
                // (this is more for other things than flags as they are single letters)
                sb.Append($"{ParameterHandler.ProspectStringSeperator}{flag.Flag}"); 
            }

            return sb.ToString();
        }
    }
}
