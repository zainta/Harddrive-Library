// Copyright (c) Zain Al-Ahmary.  All rights reserved.
// Licensed under the MIT License, (the "License"); you may not use this file except in compliance with the License. 
// You may obtain a copy of the License at https://mit-license.org/

using HDDL.HDSL;
using HDDL.HDSL.Logging;
using HDDL.IO.Settings;
using HDDL.Scanning;
using HDDL.Scanning.Monitoring;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Harddrive_Library_Passive_Scanner
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IniFileManager _iniFile;

        public Worker(ILogger<Worker> logger)
        {
            _logger = logger;

            var fi = new FileInfo(Assembly.GetExecutingAssembly().Location);
            _iniFile = IniFileManager.Explore($"{fi.Directory.FullName}\\db location.ini", true, false, false,
                new IniSubsection("HDSL_DB", null,
                    new IniValue("DatabaseLocation", defaultValue: "file database.db")),
                new IniSubsection("HDSL_Passives", null,
                    new IniValue("InitialScript", defaultValue: ""),
                    new IniValue("SideloadScript", defaultValue: ""),
                    new IniValue("DeleteSideloadScriptAfterConsumption", defaultValue: "False"),
                    new IniValue("ConsumedSideloadScriptExtension", defaultValue: ".done")));
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            #region Initialization

            var errorInitializing = false;
            try
            {
                HDSLResult outcome = null;
                if (!File.Exists(_iniFile[@"HDSL_DB>DatabaseLocation"].Value))
                {
                    if (File.Exists(_iniFile[@"HDSL_Scans>InitialScript"].Value))
                    {
                        outcome = HDSLProvider.ExecuteScript(_iniFile[@"HDSL_Scans>InitialScript"].Value, _iniFile[@"HDSL_DB>DatabaseLocation"].Value);
                    }
                    else
                    {
                        outcome = HDSLProvider.ExecuteCode(_iniFile[@"HDSL_Scans>InitialScript"].Value, _iniFile[@"HDSL_DB>DatabaseLocation"].Value);
                    }
                }

                if (outcome.Errors.Length > 0)
                {
                    _logger.LogError($"The following errors occurred during initial script execution: {string.Join<HDSLLogBase>('\n', outcome.Errors)}");
                    errorInitializing = true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"An exception occurred during the initial script.  {ex}");
                errorInitializing = true;
            }

            #endregion

            if (!errorInitializing)
            {
                using (var sk = new ScannerKernal(
                    _iniFile[@"HDSL_DB>DatabaseLocation"].Value, 
                    _iniFile[@"HDSL_Scans>InitialScript"].Value, 
                    MessagingModes.Errors | MessagingModes.Information))
                {
                    while (!stoppingToken.IsCancellationRequested)
                    {


                        //_logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
                        //await Task.Delay(1000, stoppingToken);
                    }
                }
            }
        }
    }
}
