// Copyright (c) Zain Al-Ahmary.  All rights reserved.
// Licensed under the MIT License, (the "License"); you may not use this file except in compliance with the License. 
// You may obtain a copy of the License at https://mit-license.org/

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using HDDL.IO.Settings;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace HDDL.Web
{
    /// <summary>
    /// Provides the ability to make calls and retrieve information from the system via REST api calls.
    /// </summary>
    public class HDSLWeb
    {
        private IWebHost _host;
        private IniFileManager _settings;

        /// <summary>
        /// Creates an HDSL Web component and supplies it with settings
        /// </summary>
        /// <param name="settings">The configuration settings</param>
        public HDSLWeb(IniFileManager settings)
        {
            _settings = settings;
            _host = null;
        }

        /// <summary>
        /// Starts the service
        /// </summary>
        public void Run()
        {
            _host = new WebHostBuilder()
                .UseKestrel()
                .UseStartup<HDSLWebStarter>()
                .ConfigureLogging(logging =>
                {
                    logging.ClearProviders();
                    logging.AddConsole();
                })
                .Build();
            _host.Start();
        }
    }
}
