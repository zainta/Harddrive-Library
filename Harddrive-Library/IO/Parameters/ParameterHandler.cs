using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace HDDL.IO.Parameters
{
    /// <summary>
    /// A generic parameter handling system
    /// 
    /// Definitions:
    /// this -t these-things -efg -path: "path string" -q
    /// in the above example:
    ///  -t is an option
    ///      these-things is its argument
    ///  -efg is a set of flags
    ///  -path: is an option
    ///      "path string" is -path:'s argument
    ///  -q is a flag
    /// </summary>
    public class ParameterHandler
    {
        public const char ProspectStringSeperator = '*';

        /// <summary>
        /// Contains the rules defining the arguments
        /// </summary>
        public List<ParameterRuleBase> Rules { get; private set; }

        /// <summary>
        /// Create a Parameter Handler and provide it a set of rules
        /// </summary>
        /// <param name="rules">The rules to use for parameters</param>
        public ParameterHandler(params ParameterRuleBase[] rules)
        {
            Rules = new List<ParameterRuleBase>();

            SafelyAddRules(rules);
        }

        /// <summary>
        /// Retrieves the parameter's value(s)
        /// if the offset is -1, will return a comma seperated list of all provided values
        /// </summary>
        /// <param name="key">The name of the parameter to retrieve</param>
        /// <param name="offset">The offset index of the subvalue</param>
        /// <returns>The value if found, null otherwise</returns>
        public string GetParam(string key, int offset = 0)
        {
            string val = null;

            if (offset > -1)
            {
                val = (from rule in Rules
                       where
                           rule.Arguments.ContainsKey($"{key}_{offset}")
                       select rule.Arguments[$"{key}_{offset}"])
                            .SingleOrDefault();
            }
            else if (offset == -1)
            {
                val = string.Join(',', 
                    from rule in Rules
                    from pair in rule.Arguments
                    where
                        Regex.IsMatch(pair.Key, $"{key}_\\d+$")
                    select $"{pair.Value}");
            }

            return val;
        }

        /// <summary>
        /// Retrieves the flag's value as a boolean
        /// </summary>
        /// <param name="key">The name of the parameter to retrieve</param>
        /// <returns>The flag's boolean state</returns>
        /// <exception cref="InvalidOperationException">Thrown if the target is found but not a flag</exception>
        public bool GetFlag(string key)
        {
            var offset = 0;
            var val = (from rule in Rules
                       where
                           rule.Arguments.ContainsKey($"{key}_{offset}")
                       select rule.Arguments[$"{key}_{offset}"])
                        .SingleOrDefault();

            bool result;
            if (!bool.TryParse(val, out result))
            {
                throw new InvalidOperationException($"Parameter '{key}' must be a flag parameter.");
            }
            return result;
        }

        /// <summary>
        /// Retrieves the parameter's value(s)
        /// </summary>
        /// <param name="key">The name of the parameter to retrieve</param>
        /// <returns>The value if found, null otherwise</returns>
        public string[] GetAllParam(string key)
        {
            var vals = (from rule in Rules
                        where
                            rule.Arguments.Keys.Where(k => Regex.IsMatch(k, $"{key}_\\d+")).Any()
                        select rule.Arguments.Values.ToArray())
                        .SingleOrDefault();

            return vals;
        }

        /// <summary>
        /// Checks to see if the given parameter was supplied at execution time
        /// </summary>
        /// <param name="key">The name of the parameter to check for</param>
        /// <param name="offset">The offset index of the subvalue</param>
        /// <returns>True if found, false otherwise</returns>
        public bool HasParam(string key, int offset = 0)
        {
            var hasParm = (from rule in Rules
                        where
                            rule.Arguments.ContainsKey($"{key}_{offset}")
                        select rule.Arguments[$"{key}_{offset}"])
                        .SingleOrDefault() != null;

            return hasParm;
        }

        /// <summary>
        /// Combs through the provided parameters and returns all unconsumed parameters
        /// </summary>
        /// <param name="args">The arguments to comb through</param>
        /// <returns>Any unconsumed arguments</returns>
        public string[] Comb(string[] args)
        {
            var parms = args.ToArray(); // clone it
            for (int i = 0; i < Rules.Count; i++)
            {
                parms = Rules[i].Comb(parms);
            }

            return parms;
        }

        /// <summary>
        /// A chainable means of adding new rules to the system
        /// </summary>
        /// <param name="rules">The rules to add</param>
        /// <returns>Returns the handler itself to allow chaining</returns>
        public ParameterHandler AddRules(params ParameterRuleBase[] rules)
        {
            SafelyAddRules(rules);
            return this;
        }

        /// <summary>
        /// Takes a set of rules and adds only the ones that do not already exist.
        /// </summary>
        /// <param name="rules">The rules to add</param>
        /// <exception cref="InvalidOperationException">Thrown upon a duplicate</exception>
        private void SafelyAddRules(ParameterRuleBase[] rules)
        {
            foreach (var rule in rules)
            {
                var duplicates = (from r in Rules
                                  where
                                    r.Id != rule.Id &&
                                    SharesProspects(r.GetProspects(), rule.GetProspects())
                                  select r).ToArray();
                if (duplicates.Any())
                {
                    throw new InvalidOperationException("Duplicate rules detected.");
                }
                else
                {
                    Rules.Add(rule);
                }
            }
        }

        /// <summary>
        /// Retrieves and returns a list of shared prospects from two strings
        /// </summary>
        /// <param name="prospects1"></param>
        /// <param name="prospects2"></param>
        /// <returns>The list of shared prospects</returns>
        private IEnumerable<string> GetSharedProspects(string prospects1, string prospects2)
        {
            var measure = prospects1.Length >= prospects2.Length ? prospects1 : prospects2;
            var tested = prospects1.Length >= prospects2.Length ? prospects2 : prospects1;
            var found = new List<string>();
            var item = new StringBuilder();
            
            // indicates whether or not the next ProspectStringSeperator will be the beginning of an item or the end
            var start = true; 

            // loop through the shorter one, searching for the defined items in the longer one until done
            // if any are found then return them
            for (int i = 0; i < tested.Length; i++)
            {
                // an item is all characters from the ProspectStringSeperator to the next ProspectStringSeperator or EOF (including the first, but not the second)
                // get the next item
                item.Clear();                
                for (; i < tested.Length || (i < tested.Length && tested[i] == ProspectStringSeperator && start); i++)
                {
                    if (tested[i] == ProspectStringSeperator && start)
                    {
                        start = false;
                        item.Append(tested[i]);
                    }
                    else if (tested[i] != ProspectStringSeperator)
                    {
                        item.Append(tested[i]);
                    }
                }
                start = true;

                // check the item against the measure
                if (measure.Contains(item.ToString()))
                {
                    found.Add(item.ToString());
                }
            }

            return found.ToArray();
        }

        /// <summary>
        /// Compares two prospect strings from rules to see if any of their prospects are shared.
        /// </summary>
        /// <param name="prospects1"></param>
        /// <param name="prospects2"></param>
        /// <returns>True if they share some, false otherwise</returns>
        private bool SharesProspects(string prospects1, string prospects2)
        {
            return GetSharedProspects(prospects1, prospects2).Any();
        }
    }
}
