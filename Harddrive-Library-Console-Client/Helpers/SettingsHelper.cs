// Copyright (c) Zain Al-Ahmary.  All rights reserved.
// Licensed under the MIT License, (the "License"); you may not use this file except in compliance with the License. 
// You may obtain a copy of the License at https://mit-license.org/

using HDDL.IO.Parameters;
using HDDL.IO.Settings;

namespace HDSL.ConsoleClient.Helpers
{
    /// <summary>
    /// Handles ini and parameter digestion
    /// </summary>
    class SettingsHelper
    {
        private const string Ini_File_Location = "db location.ini";

        /// <summary>
        /// Retrieves the ini file, digests it, and returns the manager instance
        /// </summary>
        /// <returns></returns>
        public static IniFileManager GetIni()
        {
            var manager = IniFileManager.Explore(Ini_File_Location, true, false, false,
                new IniSubsection("HDSL_DB", null,
                    new IniValue("DatabaseLocation", defaultValue: "file database.db")),
                new IniSubsection("HDSL_Web", null,
                    new IniValue("BroadcastSources", defaultValue: null),
                    new IniValue("TryExecuteRemotely", defaultValue: "False")));

            return manager;
        }

        /// <summary>
        /// Digests the program's arguments and returns the resulting parameter handler
        /// </summary>
        /// <param name="args">The programs arguments</param>
        /// <param name="manager">An existing ini file manager</param>
        /// <returns></returns>
        public static ParameterHandler HandleParams(string[] args, IniFileManager manager)
        {
            ParameterHandler ph = new ParameterHandler();
            ph.AddRules(
                new ParameterRuleOption("db", false, true, manager[@"HDSL_DB>DatabaseLocation"].Value, "-"),
                new ParameterRuleOption("scan", true, true, null, "-"),
                new ParameterRuleOption("run", false, true, null, "-"),
                new ParameterRuleOption("exec", false, true, null, "-"),
                new ParameterRuleOption("dm", false, true, "t", "-"),
                new ParameterRuleOption("help", true, true, null, "-"),
                new ParameterRuleShortcut("ex"),
                new ParameterRuleFlag(new FlagDefinition[] {
                    new FlagDefinition('e', true, true),
                    new FlagDefinition('c', true, true),
                    new FlagDefinition('s', true, false), 
                    new FlagDefinition('r', true, true) }, "-")
                );
            ph.Comb(args);

            // the -s flag tells the system to overwrite the ini file and update it.
            // (this will use hte value stored in db, so if it is set by option then it will update)
            if (ph.GetFlag("s"))
            {
                manager[@"HDSL_DB>DatabaseLocation"].Value = ph["db"];
                manager.WriteFile(Ini_File_Location, Ini_File_Location);
            }

            return ph;
        }
    }
}
