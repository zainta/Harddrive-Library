﻿// Copyright (c) Zain Al-Ahmary.  All rights reserved.
// Licensed under the MIT License, (the "License"); you may not use this file except in compliance with the License. 
// You may obtain a copy of the License at https://mit-license.org/

using HDDL.Data;
using HDDL.Language.HDSL;
using HDDL.Language.HDSL.Permissions;
using HDDL.IO.Settings;
using Microsoft.AspNetCore.Mvc;
using HDDL.Language.Json;

namespace HDDL.Web
{
    /// <summary>
    /// The HDSLWeb API controller
    /// </summary>
    [Route("su")]
    [ApiController]
    public class HDSLUnformattedSearchController : Controller
    {
        private IDataHandler _dh;
        private IInitializationFileManager _ini;
        private HDSLListManager _grayManager;

        public HDSLUnformattedSearchController(IDataHandler dh, IInitializationFileManager ini)
        {
            _dh = dh;
            _ini = ini;
            _grayManager = new HDSLListManager(_ini);
        }

        /// <summary>
        /// Performs a wide query against all disk items and bookmarks
        /// </summary>
        /// <param name="text">The search text</param>
        /// <returns></returns>
        [HttpGet("{text}")]
        public IActionResult Get(string text)
        {
            var result = _dh.WideSearch(text);
            return Ok(JsonConverter.GetJson(result, false));
        }

        /// <summary>
        /// Performs a wide query against all disk items and bookmarks
        /// </summary>
        /// <param name="text">The search text</param>
        /// <returns></returns>
        [HttpPost("{text}")]
        public IActionResult Post(string text)
        {
            var result = _dh.WideSearch(text);
            return Ok(JsonConverter.GetJson(result, false));
        }
    }
}
