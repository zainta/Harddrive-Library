using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HDDL.IO.Parameters
{
    /// <summary>
    /// Defines a parameter option (an argument that can take parameters)
    /// </summary>
    public class ParameterRuleOption : ParameterRuleBase
    {
        /// <summary>
        /// The number of trailing paramters to coopt as arguments for this option
        /// </summary>
        public int ArgumentCount { get; private set; }

        /// <summary>
        /// The text component of this option (ie, for "-path:" Option should be "path").  Used as the key to retrieve the parameter's value from the ParameterHandler, too
        /// </summary>
        public string Option { get; private set; }

        /// <summary>
        /// Whether or not the option requires a colon at the end for recognition
        /// </summary>
        public bool UseColonTerminator { get; private set; }

        /// <summary>
        /// Creates an option parameter definition
        /// </summary>
        /// <param name="option">The text component of this option</param>
        /// <param name="useColonTerminator">Whether or not the option requires a colon at the end for recognition</param>
        /// <param name="argumentCount">The number of trailing paramters to coopt as arguments for this option</param>
        /// <param name="flagOpeners"></param>
        /// <param name="optionDefault">The option's default value (if it is not explicitly set)</param>
        public ParameterRuleOption(string option, bool useColonTerminator, int argumentCount, string optionDefault, params string[] flagOpeners) : base(flagOpeners)
        {
            if (argumentCount == 0 || argumentCount < -1)
            {
                throw new ArgumentException("ArgumentCount must be greater than 0 or -1");
            }

            Option = option;
            ArgumentCount = argumentCount;
            UseColonTerminator = useColonTerminator;
            Arguments.Add(Option, optionDefault);
        }

        /// <summary>
        /// Combs through the provided parameters and returns all unconsumed items
        /// </summary>
        /// <param name="args">The arguments to comb through</param>
        /// <returns>Any unconsumed arguments</returns>
        public override string[] Comb(string[] args)
        {
            var possibleOptions = (from opener in FlagDesignators
                                   select $"{opener}{Option}" + (UseColonTerminator ? ":" : string.Empty))
                                   .ToArray();

            var results = new List<string>();
            var found = false;
            for (int i = 0; i < args.Length; i++)
            {
                if (possibleOptions.Contains(args[i]) && !found)
                {
                    var index = i + 1;
                    var remaining = ArgumentCount > 0 ? ArgumentCount : args.Length - index;
                    var sb = new StringBuilder();
                    while (index < args.Length && remaining > 0)
                    {
                        sb.Append(args[index]);
                        index++;
                        remaining--;
                    }

                    Arguments[Option] = sb.ToString();
                    found = true;
                    i = (index - 1);
                }
                else
                {
                    results.Add(args[i]);
                }
            }

            return results.ToArray();
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
