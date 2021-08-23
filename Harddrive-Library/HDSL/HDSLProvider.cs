using HDDL.Collections;
using HDDL.Data;
using HDDL.HDSL.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HDDL.HDSL
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
        /// <returns>An HDSLResult containing either errors or the results of the query</returns>
        public static HDSLResult ExecuteCode(string code, string dbPath)
        {
            HDSLResult result;
            var t = new HDSLTokenizer(true);
            var logs = t.Tokenize(code);
            if (logs.Length == 0)
            {
                result = Execute(t.Tokens, dbPath);
            }
            else
            {
                result = new HDSLResult(logs);
            }

            return result;
        }

        /// <summary>
        /// Runs the contents of a file as a query against the indexed files in the database
        /// </summary>
        /// <param name="path">The script file to execute</param>
        /// <param name="dbPath">The file index database to use</param>
        /// <returns>An HDSLResult containing either errors or the results of the query</returns>
        public static HDSLResult ExecuteScript(string path, string dbPath)
        {
            string code = null;
            HDSLResult result = null;
            try
            {
                if (File.Exists(path))
                {
                    code = File.ReadAllText(path);
                }
                else
                {
                    result = new HDSLResult(new HDSLLogBase[] { new HDSLLogBase(-1, -1, $"Script file not found. '{path}'") });
                }
            }
            catch (Exception ex)
            {
                result = new HDSLResult(new HDSLLogBase[] { new HDSLLogBase(-1, -1, $"Unable to load script file '{path}': {ex}") });
            }

            if (result == null)
            {
                var results = new List<string>();
                var t = new HDSLTokenizer(true);
                var logs = t.Tokenize(code);
                if (logs.Length == 0)
                {
                    result = Execute(t.Tokens, dbPath);
                }
                else
                {
                    result = new HDSLResult(logs);
                }
            }

            return result;
        }

        /// <summary>
        /// Uses an HDSLInterpreter to execute the provided tokens, returns the results and populates the out variable
        /// </summary>
        /// <param name="tokens">A set of HDSLTokens generated from a script</param>
        /// <param name="dbPath">The path to the indexed files database</param>
        /// <returns>An HDSLResult containing either errors or the results of the query</returns>
        private static HDSLResult Execute(ListStack<HDSLToken> tokens, string dbPath)
        {
            HDSLResult result;
            using (var db = new HDDLDataContext(dbPath))
            {
                var interpreter = new HDSLInterpreter(tokens, db);
                result = interpreter.Interpret(false);
            }

            return result;
        }
    }
}
