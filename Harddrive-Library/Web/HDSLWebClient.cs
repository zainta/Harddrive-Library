// Copyright (c) Zain Al-Ahmary.  All rights reserved.
// Licensed under the MIT License, (the "License"); you may not use this file except in compliance with the License. 
// You may obtain a copy of the License at https://mit-license.org/

using HDDL.Language.HDSL.Results;
using System;
using System.IO;
using System.Net;
using ReddWare.Language.Json;
using HDDL.Data;
using System.Collections.Generic;

namespace HDDL.Web
{
    /// <summary>
    /// Handles communicating with an HDSL service host
    /// </summary>
    public class HDSLWebClient
    {
        /// <summary>
        /// The service's address
        /// </summary>
        private string _address;

        /// <summary>
        /// The valid address of a service endpoint to communicate with
        /// </summary>
        /// <param name="address">The address</param>
        public HDSLWebClient(string address)
        {
            _address = address;
        }

        /// <summary>
        /// Sends a Hi request to the assigned address
        /// </summary>
        /// <returns></returns>
        public bool Hi()
        {
            return Hi(_address);
        }

        /// <summary>
        /// Executes a query remotely via the service
        /// </summary>
        /// <param name="code">The HDSL code to execute</param>
        /// <param name="formatted">Request formatted or unformatted json</param>
        /// <returns>The result instance</returns>
        public HDSLOutcomeSet Query(string code, bool formatted = false)
        {
            var formattingType = formatted ? "qf" : "qu";
            WebRequest request = WebRequest.Create($"{_address}/{formattingType}/{Uri.EscapeUriString(code)}");
            request.Method = "GET";
            WebResponse response = request.GetResponse();

            var json = ReadEntirety(response);
            var result = JsonConverter.GetObject<HDSLOutcomeSet>(json);

            return result;
        }

        /// <summary>
        /// Executes a WideSearch remotely via the service
        /// </summary>
        /// <param name="query">The query text to send</param>
        /// <param name="pageIndex">The query result page to return</param>
        /// <param name="formatted">Request formatted or unformatted json</param>
        /// <returns>The result instance</returns>
        public HDSLRecord[] Search(string query, int pageIndex = 0, bool formatted = false)
        {
            var formattingType = formatted ? "sf" : "su";
            WebRequest request = WebRequest.Create($"{_address}/{formattingType}/{pageIndex}/{Uri.EscapeUriString(query)}");
            request.Method = "GET";
            WebResponse response = request.GetResponse();

            var json = ReadEntirety(response);
            var result = JsonConverter.GetObject<HDSLRecord[]>(json);

            return result;
        }

        /// <summary>
        /// Remotely retrieves and returns the defined column mappings
        /// </summary>
        /// <returns>The column mappings array</returns>
        public List<ColumnNameMappingItem> GetMappings()
        {
            WebRequest request = WebRequest.Create($"{_address}/mappings");
            request.Method = "GET";
            WebResponse response = request.GetResponse();

            var json = ReadEntirety(response);
            var result = JsonConverter.GetObject<List<ColumnNameMappingItem>>(json);

            return result;
        }

        /// <summary>
        /// Sends a Hi request to the provided address
        /// </summary>
        /// <param name="address">The address to Hi test</param>
        /// <returns>Returns true upon the expected response, false otherwise</returns>
        public static bool Hi(string address)
        {
            try
            {
                WebRequest request = WebRequest.Create($"{address}{(address.EndsWith('/') ? string.Empty : "/")}hi");
                request.Method = "GET";
                WebResponse response = request.GetResponse();

                return ReadEntirety(response).ToLower() == "hi";
            }
            catch
            {
                // we don't actually care why it fails, we only want to know if it succeeded.
                // eat the exception and move on.
            }

            return false;
        }

        /// <summary>
        /// Reads the body of a response as a single string
        /// </summary>
        /// <param name="response">The response to read</param>
        /// <returns>The resulting string</returns>
        private static string ReadEntirety(WebResponse response)
        {
            string result = null;

            using (StreamReader sr = new StreamReader(response.GetResponseStream()))
            {
                result = sr.ReadToEnd();
            };

            return result;
        }
    }
}
