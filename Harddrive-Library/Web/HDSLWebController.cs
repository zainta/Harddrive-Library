// Copyright (c) Zain Al-Ahmary.  All rights reserved.
// Licensed under the MIT License, (the "License"); you may not use this file except in compliance with the License. 
// You may obtain a copy of the License at https://mit-license.org/

using System;
using System.Linq;
using HDDL.Data;
using HDDL.HDSL;
using HDDL.HDSL.Results;
using HDDL.IO.Settings;
using Microsoft.AspNetCore.Mvc;

namespace HDDL.Web
{
    /// <summary>
    /// The HDSLWeb API controller
    /// </summary>
    [Route("q")]
    [ApiController]
    public class HDSLWebController : Controller
    {
        private IDataHandler _dh;
        private IInitializationFileManager _ini;
        private string[] _blacklist;

        public HDSLWebController(IDataHandler dh, IInitializationFileManager ini)
        {
            _dh = dh;
            _ini = ini;

            _blacklist = (
                from item in 
                    (_ini[@"HDSL_Web>DisallowedHDSLStatements"] == null ? string.Empty : _ini[@"HDSL_Web>DisallowedHDSLStatements"].Value).Split(",")
                where
                    !string.IsNullOrWhiteSpace(item)
                select item.Trim())
                .ToArray();
        }

        /// <summary>
        /// Executes an HDSL statement against the database
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet("{code}")]
        public IActionResult Get(string code)
        {
            IActionResult actionOutcome = Ok(HDSLProvider.ExecuteCode(code, _dh as DataHandler, _blacklist));
            return actionOutcome;
        }
    }
}
