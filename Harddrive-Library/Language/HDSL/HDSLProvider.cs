// Copyright (c) Zain Al-Ahmary.  All rights reserved.
// Licensed under the MIT License, (the "License"); you may not use this file except in compliance with the License. 
// You may obtain a copy of the License at https://mit-license.org/

using HDDL.Collections;
using HDDL.Data;
using HDDL.Language.HDSL.Interpreter;
using HDDL.Language.HDSL.Permissions;
using HDDL.Language.HDSL.Results;
using System;
using System.IO;

namespace HDDL.Language.HDSL
{
    /// <summary>
    /// Provides external access to the HDSL language via static methods
    /// </summary>
    public class HDSLProvider
    {
        /// <summary>
        /// Runs a chunk of code as a query against the indexed files in the database
        /// </summary>
        /// <param name="code">The code to execute</param>
        /// <param name="dbPath">The file index database to use</param>
        /// <param name="tokenPermissions">An optional list instance defining which tokens can be generated</param>
        /// <returns>An HDSLResult containing either errors or the results of the query</returns>
        public static HDSLOutcomeSet ExecuteCode(string code, string dbPath, HDSLListManager tokenPermissions = null)
        {
            HDSLOutcomeSet result;
            using (var dh = DataHandler.Get(dbPath))
            {
                var t = new HDSLTokenizer(true, dh);
                var logs = t.Tokenize(code, tokenPermissions);
                if (logs.Length == 0)
                {
                    result = Execute(t.Tokens, dh);
                }
                else
                {
                    result = new HDSLOutcomeSet(logs);
                }
            }

            return result;
        }

        /// <summary>
        /// Runs a chunk of code as a query against the indexed files in the database
        /// </summary>
        /// <param name="code">The code to execute</param>
        /// <param name="dh">The datahandler to use</param>
        /// <param name="tokenPermissions">An optional list instance defining which tokens can be generated</param>
        /// <returns>An HDSLResult containing either errors or the results of the query</returns>
        public static HDSLOutcomeSet ExecuteCode(string code, DataHandler dh, HDSLListManager tokenPermissions = null)
        {
            HDSLOutcomeSet result;
            var t = new HDSLTokenizer(true, dh);
            var logs = t.Tokenize(code, tokenPermissions);
            if (logs.Length == 0)
            {
                result = Execute(t.Tokens, dh);
            }
            else
            {
                result = new HDSLOutcomeSet(logs);
            }

            return result;
        }

        /// <summary>
        /// Runs the contents of a file as a query against the indexed files in the database
        /// </summary>
        /// <param name="path">The script file to execute</param>
        /// <param name="dbPath">The file index database to use</param>
        /// <param name="tokenPermissions">An optional list instance defining which tokens can be generated</param>
        /// <returns>An HDSLResult containing either errors or the results of the query</returns>
        public static HDSLOutcomeSet ExecuteScript(string path, string dbPath, HDSLListManager tokenPermissions = null)
        {
            string code = null;
            HDSLOutcomeSet result = null;
            try
            {
                if (File.Exists(path))
                {
                    code = File.ReadAllText(path);
                }
                else
                {
                    result = new HDSLOutcomeSet(new LogItemBase[] { new LogItemBase(-1, -1, $"Script file not found. '{path}'") });
                }
            }
            catch (Exception ex)
            {
                result = new HDSLOutcomeSet(new LogItemBase[] { new LogItemBase(-1, -1, $"Unable to load script file '{path}': {ex}") });
            }

            if (result == null)
            {
                result = ExecuteCode(code, dbPath, tokenPermissions);
            }

            return result;
        }

        /// <summary>
        /// Runs the contents of a file as a query against the indexed files in the database
        /// </summary>
        /// <param name="path">The script file to execute</param>
        /// <param name="dh">The datahandler to use</param>
        /// <param name="tokenPermissions">An optional list instance defining which tokens can be generated</param>
        /// <returns>An HDSLResult containing either errors or the results of the query</returns>
        public static HDSLOutcomeSet ExecuteScript(string path, DataHandler dh, HDSLListManager tokenPermissions = null)
        {
            string code = null;
            HDSLOutcomeSet result = null;
            try
            {
                if (File.Exists(path))
                {
                    code = File.ReadAllText(path);
                }
                else
                {
                    result = new HDSLOutcomeSet(new LogItemBase[] { new LogItemBase(-1, -1, $"Script file not found. '{path}'") });
                }
            }
            catch (Exception ex)
            {
                result = new HDSLOutcomeSet(new LogItemBase[] { new LogItemBase(-1, -1, $"Unable to load script file '{path}': {ex}") });
            }

            if (result == null)
            {
                result = ExecuteCode(code, dh, tokenPermissions);
            }

            return result;
        }

        /// <summary>
        /// Uses an HDSLInterpreter to execute the provided tokens, returns the results and populates the out variable
        /// </summary>
        /// <param name="tokens">A set of HDSLTokens generated from a script</param>
        /// <param name="dh">The datahandler to use</param>
        /// <returns>An HDSLResult containing either errors or the results of the query</returns>
        private static HDSLOutcomeSet Execute(ListStack<HDSLToken> tokens, DataHandler dh)
        {
            HDSLOutcomeSet result;
            var interpreter = new HDSLInterpreter(tokens, dh);
            result = interpreter.Interpret(false);

            return result;
        }
    }
}
