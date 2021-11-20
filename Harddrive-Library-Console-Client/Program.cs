// Copyright (c) Zain Al-Ahmary.  All rights reserved.
// Licensed under the MIT License, (the "License"); you may not use this file except in compliance with the License. 
// You may obtain a copy of the License at https://mit-license.org/

using HDDL.HDSL.Results;
using HDSL.ConsoleClient.Helpers;
using System;

namespace HDSL.ConsoleClient
{
    /// <summary>
    /// For now, this is test code to develop the Harddrive-Library class library
    /// </summary>
    class Program
    {
        static void Main(string[] args)
        {
            var manager = SettingsHelper.GetIni();
            var ph = SettingsHelper.HandleParams(args, manager);
            var outcome = HDSLExecutionHelper.HandleExecution(manager, ph);
            HandleDisplay(outcome);
        }

        /// <summary>
        /// Handles the display of outcomes
        /// </summary>
        /// <param name="results">The outcomes</param>
        private static void HandleDisplay(HDSLOutcomeSet results)
        {
            if (results.Errors.Length > 0)
            {
                foreach (var error in results.Errors)
                {
                    Console.WriteLine(error.ToString());
                }
            }
            else
            {
                foreach (var result in results.Results)
                {

                }
            }
        }
    }
}
