// Copyright (c) Zain Al-Ahmary.  All rights reserved.
// Licensed under the MIT License, (the "License"); you may not use this file except in compliance with the License. 
// You may obtain a copy of the License at https://mit-license.org/

using HDDL.IO.Parameters;
using System;
using System.IO;
using System.Linq;
using System.Reflection;

namespace HDSL.ConsoleClient.Helpers
{
    /// <summary>
    /// Handles help associated functionality
    /// </summary>
    class FunctionalityHelper
    {
        /// <summary>
        /// Handles the display of help instructions
        /// </summary>
        /// <param name="ph">The parameter handler</param>
        /// <returns></returns>
        public static bool HandleHelp(ParameterHandler ph)
        {
            // Handle Help
            var helpRequest = ph.GetAllParam("help").Where(p => !string.IsNullOrWhiteSpace(p)).ToArray();
            if (helpRequest.Length > 0)
            {
                foreach (var item in helpRequest)
                {
                    switch (item.ToLower())
                    {
                        case "o":
                            DisplayHelp("HDSL.Documentation.Help_Options.txt");
                            break;
                        case "l":
                            DisplayHelp("HDSL.Documentation.Help_HDSL.txt");
                            break;
                        case "f":
                            DisplayHelp("HDSL.Documentation.Help_Flags.txt");
                            break;
                        case "s":
                            DisplayHelp("HDSL.Documentation.Help_Shortcuts.txt");
                            break;
                        case "h":
                        default:
                            DisplayHelp("HDSL.Documentation.Help_Help.txt");
                            break;
                    }
                }
                Environment.Exit(0);
            }

            return false;
        }

        /// <summary>
        /// Displays basic help if no action was taken (i.e. insufficient parameters were provided to invoke a function)
        /// </summary>
        /// <param name="anyActionTaken"></param>
        /// <returns></returns>
        public static void HandleFallThroughHelp(bool anyActionTaken)
        {
            if (!anyActionTaken)
            {
                DisplayHelp("HDSL.Documentation.Help_Help.txt");
            }
        }

        /// <summary>
        /// Displays the requested embedded resource on the commandline prompt
        /// </summary>
        /// <param name="resourceName"></param>
        private static void DisplayHelp(string resourceName)
        {
            var current = Assembly.GetExecutingAssembly();
            using (var f = new StreamReader(current.GetManifestResourceStream(resourceName)))
            {
                Console.WriteLine(f.ReadToEnd());
            }
        }
    }
}
