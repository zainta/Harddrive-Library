// Copyright (c) Zain Al-Ahmary.  All rights reserved.
// Licensed under the MIT License, (the "License"); you may not use this file except in compliance with the License. 
// You may obtain a copy of the License at https://mit-license.org/

using HDDL.Data;
using HDDL.Language.HDSL;
using HDDL.Language.HDSL.Permissions;
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
        private HDSLListManager _grayManager;

        public HDSLWebController(IDataHandler dh, IInitializationFileManager ini)
        {
            _dh = dh;
            _ini = ini;
            _grayManager = new HDSLListManager(_ini);
        }

        /// <summary>
        /// Executes an HDSL statement against the database
        /// </summary>
        /// <param name="code">The code to execute</param>
        /// <returns></returns>
        [HttpGet("{code}")]
        public IActionResult Get(string code)
        {
            IActionResult actionOutcome = Json(HDSLProvider.ExecuteCode(code, _dh as DataHandler, _grayManager));
            return actionOutcome;
        }

        /// <summary>
        /// Executes an HDSL statement against the database
        /// </summary>
        /// <param name="code">The code to execute</param>
        /// <returns></returns>
        [HttpPost("{code}")]
        public IActionResult Post(string code)
        {
            IActionResult actionOutcome = Json(HDSLProvider.ExecuteCode(code, _dh as DataHandler, _grayManager));
            return actionOutcome;
        }
    }
}
