// Copyright (c) Zain Al-Ahmary.  All rights reserved.
// Licensed under the MIT License, (the "License"); you may not use this file except in compliance with the License. 
// You may obtain a copy of the License at https://mit-license.org/

using HDDL.IO.Settings;
using System;
using System.Linq;

namespace HDDL.Language.HDSL.Permissions
{
    /// <summary>
    /// Managers the keyword white, black, and gray lists
    /// </summary>
    public class HDSLListManager
    {
        public string DisallowedHDSLStatements = "HDSL_Web>DisallowedHDSLStatements";
        public string AllowedHDSLStatements = "HDSL_Web>AllowedHDSLStatements";

        /// <summary>
        /// All of the possible keywords that can be white or black listed
        /// </summary>
        public static string[] All { get; private set; }

        /// <summary>
        /// All of the explicitly allowed keywords
        /// </summary>
        public string[] Whitelist { get; private set; }

        /// <summary>
        /// All of the explicitly disallowed keywords
        /// </summary>
        public string[] Blacklist { get; private set; }

        /// <summary>
        /// The list of keywords that meets both the white and black list specifications
        /// 
        /// Gray list = All - Black + White (note that White overwrites black in this system)
        /// </summary>
        public string[] Graylist
        {
            get
            {
                var results = (from a in All
                               where
                                !Blacklist.Contains(a) ||
                                Whitelist.Contains(a)
                               select a).ToArray();

                return results;
            }
        }

        static HDSLListManager()
        {
            All = Enum.GetNames<HDSLTokenTypes>().Select(tt => tt.ToLower()).ToArray();
        }

        /// <summary>
        /// Creates a list manager
        /// </summary>
        /// <param name="iniManager">The ini file to extract the white and black lists from</param>
        public HDSLListManager(IInitializationFileManager iniManager)
        {
            if (iniManager[DisallowedHDSLStatements] != null)
            {
                Blacklist = iniManager[DisallowedHDSLStatements].Value
                    .Split(",")
                    .Select(bli => bli.ToLower().Trim())
                    .ToArray();
            }
            else
            {
                Blacklist = new string[] { };
            }

            if (iniManager[AllowedHDSLStatements] != null)
            {
                Whitelist = iniManager[AllowedHDSLStatements].Value
                    .Split(",")
                    .Select(wli => wli.ToLower().Trim())
                    .ToArray();
            }
            else
            {
                Whitelist = new string[] { };
            }
        }
    }
}
