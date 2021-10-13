// Copyright (c) Zain Al-Ahmary.  All rights reserved.
// Licensed under the MIT License, (the "License"); you may not use this file except in compliance with the License. 
// You may obtain a copy of the License at https://mit-license.org/

using HDDL.HDSL;
using HDDL.IO.Parameters;
using HDDL.IO.Settings;
using HDDL.Scanning;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace HDSL
{
    /// <summary>
    /// For now, this is test code to develop the Harddrive-Library class library
    /// </summary>
    class Program
    {
        private const string Ini_File_Location = "db location.ini";

        private static bool _embellish;
        private static bool _count;

        static void Main(string[] args)
        {
            // tracks whether or not a request was made of the application
            var receivedOrder = false;

            var manager = IniFileManager.Explore(Ini_File_Location, true, false, false,
                new IniSubsection("HDSL_DB", null, new IniValue("DatabaseLocation", defaultValue: "file database.db") ));

            ParameterHandler ph = new ParameterHandler();
            ph.AddRules(
                new ParameterRuleOption("columns", true, true, null, "-"),
                new ParameterRuleOption("paging", false, true, null, "-"),
                new ParameterRuleOption("db", false, true, manager[@"HDSL_DB>DatabaseLocation"].Value, " - "),
                new ParameterRuleOption("scan", true, true, null, "-"),
                new ParameterRuleOption("run", false, true, null, "-"),
                new ParameterRuleOption("exec", false, true, null, "-"),
                new ParameterRuleOption("dm", false, true, "t", "-"),
                new ParameterRuleOption("check", true, true, null, "-"),
                new ParameterRuleOption("help", true, true, null, "-"),
                new ParameterRuleShortcut("ex"),
                new ParameterRuleFlag(new FlagDefinition[] {
                    new FlagDefinition('e', true, true),
                    new FlagDefinition('c', true, true),
                    new FlagDefinition('s', true, false) }, "-")
                );
            ph.Comb(args);

            // ensure that our columns and paging parameters are valid or will be defaulted
            // (defaulted = null or whitespace, but if the only way to get whitespace is if the user omits a value for the -columns or -paging options)
            if (ph.GetParam("paging") == string.Empty || 
                ph.GetParam("columns", -1) == string.Empty)
            {
                Console.WriteLine("Improper paging or column format strings provided.");
                DisplayHelp("HDSL.Documentation.Help_Options.txt");
                Environment.Exit(0);
            }


            // Handle Help
            var helpRequest = ph.GetAllParam("help").Where(p => !string.IsNullOrWhiteSpace(p)).ToArray();
            if (helpRequest.Length > 0)
            {
                receivedOrder = true;

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

            var dbPath = ph["db"];
            var scanPaths = ph.GetAllParam("scan").Where(p => !string.IsNullOrWhiteSpace(p)).ToArray();
            var checkPaths = ph.GetAllParam("check").Where(p => !string.IsNullOrWhiteSpace(p)).ToArray();
            var runScript = ph["run"];
            var executeFile = !string.IsNullOrWhiteSpace(ph["exec"]) ? ph["exec"] : ph["ex"];
            _embellish = ph.GetFlag("e");
            _count = ph.GetFlag("c");

            EventWrapperDisplayModes displayMode;
            switch (ph.GetParam("dm"))
            {
                case "p":
                    displayMode = EventWrapperDisplayModes.ProgressBar;
                    break;
                case "s":
                    displayMode = EventWrapperDisplayModes.Spinner;
                    break;
                default:
                case "t":
                    displayMode = EventWrapperDisplayModes.Text;
                    break;
                case "q":
                    displayMode = EventWrapperDisplayModes.Displayless;
                    break;
            }

            // the -s flag tells the system to overwrite the ini file and update it.
            // (this will use hte value stored in db, so if it is set by option then it will update)
            if (ph.GetFlag("s"))
            {
                receivedOrder = true;
                manager[@"HDSL_DB>DatabaseLocation"].Value = dbPath;
                manager.WriteFile(Ini_File_Location, Ini_File_Location);
            }

            if (scanPaths.Length > 0)
            {
                receivedOrder = true;
                var scanWrapper = new DiskScanEventWrapper(dbPath, scanPaths, true, displayMode);
                if (scanWrapper.Go())
                {
                    (scanWrapper.Result as DiskScanResultSet)?.Display(_count, _embellish);
                }
            }

            if (checkPaths.Length > 0)
            {
                receivedOrder = true;
                var statement = $"check {ConvertDisplayMode(displayMode)} {string.Join(", ", checkPaths)};";
                HandleDisplay(HDSLProvider.ExecuteCode(statement, dbPath), ph.GetParam("paging"), ph.GetParam("columns", -1));
            }

            // Execute a line of code
            if (!string.IsNullOrWhiteSpace(runScript))
            {
                receivedOrder = true;
                HandleDisplay(HDSLProvider.ExecuteCode(runScript, dbPath), ph.GetParam("paging"), ph.GetParam("columns", -1));
            }

            // Execute the contents of a code file
            if (!string.IsNullOrWhiteSpace(executeFile))
            {
                receivedOrder = true;
                HandleDisplay(HDSLProvider.ExecuteScript(executeFile, dbPath), ph.GetParam("paging"), ph.GetParam("columns", -1));
            }

            if (!receivedOrder)
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

        /// <summary>
        /// Handles the display of result sets
        /// </summary>
        /// <param name="results">The result set</param>
        /// <param name="pagingCode">The paging code for Find queries</param>
        /// <param name="columnData">The column data for Find queries</param>
        private static void HandleDisplay(HDSLResult results, string pagingCode, string columnData)
        {
            if (results.Errors.Length > 0)
            {
                foreach (var error in results.Errors)
                {
                    Console.WriteLine(error);
                }
            }
            else
            {
                for (var i = 0; i < results.Results.Count(); i++)
                {
                    if (i > 0)
                    {
                        Console.WriteLine();
                        Console.WriteLine();
                    }
                    var set = results.Results.ElementAt(i);

                    if (set is FindQueryResultSet)
                    {
                        ((FindQueryResultSet)set).Display(columnData, pagingCode, _count, _embellish);
                    }
                    else if (set is IntegrityScanResultSet)
                    {
                        ((IntegrityScanResultSet)set).Display(null, null, _count, _embellish, IntegrityResultSetDisplayModes.Changed);
                    }
                    else if (set is DiskScanResultSet)
                    {
                        ((DiskScanResultSet)set).Display(_count, _embellish);
                    }
                }
            }
        }

        /// <summary>
        /// Converts the display mode to the respective HDSL keyword
        /// </summary>
        /// <param name="displayMode"></param>
        /// <returns></returns>
        private static object ConvertDisplayMode(EventWrapperDisplayModes displayMode)
        {
            var result = string.Empty;

            switch (displayMode)
            {
                case EventWrapperDisplayModes.Displayless:
                    result = "quiet";
                    break;
                case EventWrapperDisplayModes.ProgressBar:
                    result = "progress";
                    break;
                case EventWrapperDisplayModes.Spinner:
                    result = "spinner";
                    break;
                case EventWrapperDisplayModes.Text:
                    result = "text";
                    break;
            }

            return result;
        }
    }
}
