// Copyright (c) Zain Al-Ahmary.  All rights reserved.
// Licensed under the MIT License, (the "License"); you may not use this file except in compliance with the License. 
// You may obtain a copy of the License at https://mit-license.org/

using HDDL.Data;
using HDDL.Language.HDSL;
using HDDL.Language.HDSL.Results;
using HDDL.IO.Parameters;
using HDDL.IO.Settings;
using HDDL.Scanning;
using HDDL.Web;
using System.IO;
using System.Linq;

namespace HDSL.ConsoleClient.Helpers
{
    /// <summary>
    /// Handles local and remote HDSL execution
    /// </summary>
    class HDSLExecutionHelper
    {
        /// <summary>
        /// Handles the execution of HDSL code both locally and remotely
        /// </summary>
        /// <param name="manager"></param>
        /// <param name="ph"></param>
        /// <returns></returns>
        public static HDSLOutcomeSet HandleExecution(IniFileManager manager, ParameterHandler ph)
        {
            HDSLOutcomeSet outcome = null;

            // do we have any broadcast sources to check?
            if (manager[@"HDSL_Web>BroadcastSources"] == null)
            {
                // we have no broadcast sources, so run locally.
                outcome = HandleLocal(ph);
            }
            else
            {
                bool tryExecuteRemotely;
                bool.TryParse(manager[@"HDSL_Web>TryExecuteRemotely"]?.Value, out tryExecuteRemotely);

                if (tryExecuteRemotely && ph.GetFlag("r"))
                {
                    // check to see if any are active
                    var queryAddress = GetFirstValidAddress(manager[@"HDSL_Web>BroadcastSources"].Value);
                    if (!string.IsNullOrWhiteSpace(queryAddress))
                    {
                        var runScript = ph["run"];
                        var executeFile = !string.IsNullOrWhiteSpace(ph["exec"]) ? ph["exec"] : ph["ex"];

                        // found one.  use the first functional location
                        var client = new HDSLWebClient(queryAddress);

                        if (!string.IsNullOrWhiteSpace(runScript))
                        {
                            outcome = client.Query(runScript);
                        }
                        else if (!string.IsNullOrWhiteSpace(executeFile))
                        {
                            var code = File.ReadAllText(executeFile);
                            outcome = client.Query(code);
                        }
                    }
                    else
                    {
                        // none are active.  run locally.
                        outcome = HandleLocal(ph);
                    }
                }
                else
                {
                    outcome = HandleLocal(ph);
                }
            }

            return outcome;
        }

        /// <summary>
        /// Handles the execution of HDSL locally
        /// </summary>
        /// <param name="manager"></param>
        /// <param name="ph"></param>
        /// <returns></returns>
        private static HDSLOutcomeSet HandleLocal(ParameterHandler ph)
        {
            var dbPath = ph["db"];
            var scanPaths = ph.GetAllParam("scan").Where(p => !string.IsNullOrWhiteSpace(p)).ToArray();
            var runScript = ph["run"];
            var executeFile = !string.IsNullOrWhiteSpace(ph["exec"]) ? ph["exec"] : ph["ex"];

            HDSLOutcomeSet outcome = null;
            if (!FunctionalityHelper.HandleHelp(ph))
            {
                if (scanPaths.Length > 0)
                {
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

                    var scanWrapper = new DiskScanEventWrapper(dbPath, scanPaths, true, displayMode, new ColumnHeaderSet(new DataHandler(dbPath), typeof(DiskItem)));
                    if (scanWrapper.Go())
                    {
                        //(scanWrapper.Result as DiskScanResultSet)?.Display(_count, _embellish);
                        outcome = new HDSLOutcomeSet(new HDSLOutcome[] { scanWrapper.Result });
                    }
                }

                // Execute a line of code
                if (!string.IsNullOrWhiteSpace(runScript))
                {
                    outcome = HDSLProvider.ExecuteCode(runScript, dbPath);
                }

                // Execute the contents of a code file
                if (!string.IsNullOrWhiteSpace(executeFile))
                {
                    outcome = HDSLProvider.ExecuteScript(executeFile, dbPath);
                }
            }

            return outcome;
        }

        /// <summary>
        /// Takes a comma seperated list of HDSL service addresses and checks them sequentially until it finds one that is up.
        /// </summary>
        /// <param name="addressList">The comma seperated list of service addresses</param>
        /// <returns>The first functional service address found</returns>
        private static string GetFirstValidAddress(string addressList)
        {
            try
            {
                var addresses = addressList
                    ?.Split(",")
                    .Where(a => !string.IsNullOrWhiteSpace(a))
                    .Select(a => a.Trim());
                string valid = null;
                foreach (var address in addresses)
                {
                    if (HDSLWebClient.Hi(address))
                    {
                        valid = address;
                        break;
                    }
                }

                return valid;
            }
            catch
            {
                return null;
            }
        }
    }
}
