// Copyright (c) Zain Al-Ahmary.  All rights reserved.
// Licensed under the MIT License, (the "License"); you may not use this file except in compliance with the License. 
// You may obtain a copy of the License at https://mit-license.org/

using System;
using System.Linq;
using HDDL.Data;
using HDDL.HDSL;
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
            IActionResult actionOutcome = null;

            try
            {
                var result = HDSLProvider.ExecuteCode(code, _dh as DataHandler, _blacklist);
                if (result.Errors.Length == 0 && result.Results.Count() == 1)
                {
                    var resultSet = result.Results.First() as FindQueryResultSet;
                    if (resultSet != null)
                    {
                        actionOutcome = Ok(resultSet.Items);
                    }
                }
                else if (result.Errors.Length == 0 && result.Results.Count() > 1)
                {
                    // just here for the time being
                    actionOutcome = Ok("Multiple result sets were returned.  Please query for a single result set at a time.");
                }
                else if (result.Errors.Length > 0)
                {
                    // for now, dump the error listing at them
                    actionOutcome = Ok(string.Join(", ", result.Errors.Select(log => log.ToString())));
                }
            }
            catch (Exception ex)
            {
                actionOutcome = BadRequest("Failed to execute request.");
            }

            return actionOutcome;
        }
    }
}
