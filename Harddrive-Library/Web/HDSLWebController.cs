using System;
using System.Collections.Generic;
using System.Linq;
using HDDL.Data;
using HDDL.HDSL;
using Microsoft.AspNetCore.Authorization;
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

        public HDSLWebController(IDataHandler dh)
        {
            _dh = dh;
        }

        /// <summary>
        /// Executes a find against the database
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet("{query}")]
        public IActionResult Get(string query)
        {
            IActionResult actionOutcome = null;
            if (!query.StartsWith("find", StringComparison.InvariantCultureIgnoreCase))
            {
                actionOutcome = Ok("Unsupported command.");
            }
            else
            {
                var result = HDSLProvider.ExecuteCode(query, _dh as DataHandler);
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

            return actionOutcome;
        }
    }
}
