// Copyright (c) Zain Al-Ahmary.  All rights reserved.
// Licensed under the MIT License, (the "License"); you may not use this file except in compliance with the License. 
// You may obtain a copy of the License at https://mit-license.org/

using HDDL.Data;
using ReddWare.IO.Settings;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Reflection;

namespace HDDL.Web
{
    /// <summary>
    /// The startup class handed to the Kestral server
    /// </summary>
    class HDSLWebStarter
    {
        /// <summary>
        /// Handles configuration and security setup
        /// </summary>
        /// <param name="services">The list of services to run</param>
        public void ConfigureServices(IServiceCollection services)
        {
            var fi = new FileInfo(Assembly.GetExecutingAssembly().Location);
            var ini = IniFileManager.Explore($"{fi.Directory.FullName}\\db location.ini", true, false, false,
                new IniSubsection("HDSL_DB", null,
                    new IniValue("DatabaseLocation", defaultValue: "file database.db"),
                    new IniValue("DisallowedHDSLStatements", defaultValue: "")));

            services.AddControllers();
            services.Add(new ServiceDescriptor(typeof(IDataHandler), new DataHandler(ini[@"HDSL_DB>DatabaseLocation"].Value)));
            services.Add(new ServiceDescriptor(typeof(IInitializationFileManager), ini));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="app"></param>
        /// <param name="env"></param>
        /// <param name="loggingBuilder"></param>
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ILoggerFactory loggerFactory)
        {
            app.UseRouting();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
