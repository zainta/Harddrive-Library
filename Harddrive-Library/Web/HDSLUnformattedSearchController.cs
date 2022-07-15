// Copyright (c) Zain Al-Ahmary.  All rights reserved.
// Licensed under the MIT License, (the "License"); you may not use this file except in compliance with the License. 
// You may obtain a copy of the License at https://mit-license.org/

using HDDL.Data;
using HDDL.Language.HDSL;
using HDDL.Language.HDSL.Permissions;
using ReddWare.IO.Settings;
using Microsoft.AspNetCore.Mvc;
using ReddWare.Language.Json;

namespace HDDL.Web
{
    /// <summary>
    /// The HDSLWeb API controller
    /// </summary>
    [Route("su")]
    [ApiController]
    public class HDSLUnformattedSearchController : HDSLControllerBase
    {
        private HDSLListManager _grayManager;

        public HDSLUnformattedSearchController(IDataHandler dh, IInitializationFileManager ini) : base(dh, ini)
        {
            _grayManager = new HDSLListManager(_ini);
        }

        /// <summary>
        /// Performs a wide query against all disk items and bookmarks
        /// </summary>
        /// <param name="text">The search text</param>
        /// <returns></returns>
        [HttpGet("{pageIndex}/{text}")]
        public IActionResult Get(string text, int pageIndex = 0)
        {
            var result = _dh.WideSearch(text, pageIndex, 300);
            return Ok(JsonConverter.GetJson(result, _useTypeAnnotation, false));
        }

        /// <summary>
        /// Performs a wide query against all disk items and bookmarks
        /// </summary>
        /// <param name="text">The search text</param>
        /// <returns></returns>
        [HttpPost("{pageIndex}/{text}")]
        public IActionResult Post(string text, int pageIndex = 0)
        {
            var result = _dh.WideSearch(text, pageIndex, 300);
            return Ok(JsonConverter.GetJson(result, _useTypeAnnotation, false));
        }
    }
}
