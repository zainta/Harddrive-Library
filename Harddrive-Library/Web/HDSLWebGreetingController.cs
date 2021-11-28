// Copyright (c) Zain Al-Ahmary.  All rights reserved.
// Licensed under the MIT License, (the "License"); you may not use this file except in compliance with the License. 
// You may obtain a copy of the License at https://mit-license.org/

using Microsoft.AspNetCore.Mvc;

namespace HDDL.Web
{
    /// <summary>
    /// The HDSLWeb API controller
    /// </summary>
    [Route("hi")]
    [ApiController]
    public class HDSLWebGreetingController : Controller
    {
        /// <summary>
        /// Returns the text "hi" signaling that the service is functional.
        /// </summary>
        /// <returns></returns>
        [HttpGet()]
        public IActionResult Get()
        {
            return Ok("hi");
        }
    }
}
