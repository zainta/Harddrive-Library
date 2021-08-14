using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HDDL.IO.Parameters
{
    /// <summary>
    /// Defines a flag for use by the ParameterRuleFlag class
    /// </summary>
    public class FlagDefinition
    {
        /// <summary>
        /// The flag's letter
        /// </summary>
        public char Flag { get; private set; }

        /// <summary>
        /// Whether or not the flag can be set through a string of characters (ie -hdsl)
        /// </summary>
        public bool Nestable { get; private set; }

        /// <summary>
        /// The flag's default state
        /// </summary>
        public bool Default { get; private set; }

        /// <summary>
        /// Creates a Flag Definition
        /// </summary>
        /// <param name="flag">The flag's letter</param>
        /// <param name="nestable">Whether or not the flag can be set through a string of characters (ie -hdsl)</param>
        /// <param name="deflt">The flag's default state</param>
        public FlagDefinition(char flag, bool nestable, bool deflt)
        {
            Flag = flag;
            Nestable = nestable;
            Default = deflt;
        }
    }
}
