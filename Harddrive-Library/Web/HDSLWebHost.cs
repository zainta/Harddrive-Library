// Copyright (c) Zain Al-Ahmary.  All rights reserved.
// Licensed under the MIT License, (the "License"); you may not use this file except in compliance with the License. 
// You may obtain a copy of the License at https://mit-license.org/

using Microsoft.AspNetCore.Hosting;
using ReddWare.IO.Settings;
using Microsoft.Extensions.Logging;

namespace HDDL.Web
{
    /// <summary>
    /// Provides the ability to make calls and retrieve information from the system via REST api calls.
    /// </summary>
    public class HDSLWebHost
    {
        private IWebHost _host;
        private IniFileManager _settings;

        /// <summary>
        /// Creates an HDSL Web component and supplies it with settings
        /// </summary>
        /// <param name="settings">The configuration settings</param>
        public HDSLWebHost(IniFileManager settings)
        {
            _settings = settings;
            _host = null;
        }

        /// <summary>
        /// Starts the service
        /// </summary>
        public void Run()
        {
            bool transmit = false;
            bool.TryParse(_settings[@"HDSL_Web>Listen"]?.Value, out transmit);

            if (transmit)
            {
                var broadcastAddress = _settings[@"HDSL_Web>Broadcast"].Value;
                if (string.IsNullOrWhiteSpace(broadcastAddress))
                {
                    broadcastAddress = "http://localhost:5000";
                }

                _host = new WebHostBuilder()
                    .UseKestrel()
                    .UseStartup<HDSLWebStarter>()
                    .ConfigureLogging(logging =>
                    {
                        logging.ClearProviders();
                        logging.AddConsole();
                    })
                    .UseUrls(broadcastAddress)
                    .Build();
                _host.Start();
            }
        }
    }
}
