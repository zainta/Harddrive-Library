// Copyright (c) Zain Al-Ahmary.  All rights reserved.
// Licensed under the MIT License, (the "License"); you may not use this file except in compliance with the License. 
// You may obtain a copy of the License at https://mit-license.org/

using HDDL.Data;
using HDDL.IO.Settings;
using ReddWare.Language.Json;
using Microsoft.AspNetCore.Mvc;

namespace HDDL.Web
{
    /// <summary>
    /// The HDSLWeb API controller
    /// </summary>
    [Route("mappings")]
    [ApiController]
    public class HDSLMappingJsonController : HDSLControllerBase
    {
        public HDSLMappingJsonController(IDataHandler dh, IInitializationFileManager ini) : base(dh, ini)
        {
        }

        /// <summary>
        /// Retrieves and returns the HDSL Mappings as they exist in the database
        /// </summary>
        /// <returns></returns>
        [HttpGet()]
        public IActionResult Get()
        {
            var result = _dh.GetAllColumnNameMappings();
            return Ok(JsonConverter.GetJson(result, _useTypeAnnotation, true));
        }

        /// <summary>
        /// Retrieves and returns the HDSL Mappings as they exist in the database
        /// </summary>
        /// <returns></returns>
        [HttpPost()]
        public IActionResult Post()
        {
            var result = _dh.GetAllColumnNameMappings();
            return Ok(JsonConverter.GetJson(result, _useTypeAnnotation, true));
        }
    }
}