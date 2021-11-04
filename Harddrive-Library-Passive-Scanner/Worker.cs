// Copyright (c) Zain Al-Ahmary.  All rights reserved.
// Licensed under the MIT License, (the "License"); you may not use this file except in compliance with the License. 
// You may obtain a copy of the License at https://mit-license.org/

using HDDL.IO.Settings;
using HDDL.Scanning.Monitoring;
using HDDL.Web;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
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
                    new IniValue("ConsumedSideloadScriptExtension", defaultValue: ".done")),
                new IniSubsection("HDSL_Web", null,
                    new IniValue("Listen", defaultValue: "False"),
                    new IniValue("Broadcast", defaultValue: "http://localhost:5000")));
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var hdslW = new HDSLWeb(_iniFile);
            hdslW.Run();

            using (var sk = new ScannerKernal(
                    _iniFile[@"HDSL_DB>DatabaseLocation"].Value,
                    new ScriptLoadingDetails(_iniFile),
                    MessagingModes.Errors | MessagingModes.Information,
                    false))
            {
                sk.MessageRelayed += Sk_MessageRelayed; ;
                if (sk.Initialize())
                {
                    sk.Start();
                    while (!stoppingToken.IsCancellationRequested)
                    {
                        await Task.Delay(1000, stoppingToken);
                    }
                    sk.Stop();
                }
            }
        }

        private void Sk_MessageRelayed(ReporterBase origin, MessageBundle message)
        {
            switch (message.Type)
            {
                case MessageTypes.Error:
                    _logger.LogError(message.Message, message.Error);
                    break;
                case MessageTypes.Information:
                    _logger.LogInformation(message.Message);
                    break;
                case MessageTypes.Warning:
                    _logger.LogWarning(message.Message);
                    break;
            }
        }
    }
}
