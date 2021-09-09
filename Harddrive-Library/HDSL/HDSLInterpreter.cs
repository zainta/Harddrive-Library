using HDDL.Collections;
using HDDL.HDSL.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HDDL.Scanning;
using Microsoft.VisualBasic.CompilerServices;
using HDDL.IO.Disk;
using HDDL.Data;
using System.IO;
using LiteDB;

namespace HDDL.HDSL
{
    /// <summary>
    /// Executes tokens according to language requirements
    /// </summary>
    class HDSLInterpreter
    {
        /// <summary>
        /// The stored tokens
        /// </summary>
        private ListStack<HDSLToken> _tokens;

        /// <summary>
        /// The database
        /// </summary>
        private LiteDatabase _db;

        /// <summary>
        /// The list of errors
        /// </summary>
        private List<HDSLLogBase> _errors;

        /// <summary>
        /// Creates an interpreter using the provided tokenizer's tokens and the provided file database
        /// </summary>
        /// <param name="tokenizer">The tokenizer whose tokens should be consumed</param>
        /// <param name="db">The database to use</param>
        public HDSLInterpreter(ListStack<HDSLToken> tokens, LiteDatabase db)
        {
            _tokens = new ListStack<HDSLToken>(tokens.ToList());
            _db = db;
            _errors = new List<HDSLLogBase>();
        }

        /// <summary>
        /// Interprets the tokens against the provided database
        /// </summary>
        /// <param name="closeDb">Whether or not to close the database upon completion</param>
        /// <returns>The results of the interpretation</returns>
        public HDSLResult Interpret(bool closeDb)
        {
            HDSLResult result = null;
            var results = new List<DiskItem>();

            try
            {
                var done = false;

                while (!_tokens.Empty && _errors.Count == 0 && !done)
                {
                    switch (Peek().Type)
                    {
                        case HDSLTokenTypes.Purge:
                            HandlePurgeStatement();
                            break;
                        case HDSLTokenTypes.Find:
                            results.AddRange(HandleFindStatement());
                            break;
                        case HDSLTokenTypes.EndOfFile:
                            Pop();
                            done = true;
                            break;
                        case HDSLTokenTypes.EndOfLine:
                            Pop();
                            break;
                        default:
                            _errors.Add(new HDSLLogBase(Peek().Column, Peek().Row, $"Unexpected token '{Peek().Code}'."));
                            done = true;
                            break;
                    }

                    // Get rid of semicolons
                    if (More() && Peek().Type == HDSLTokenTypes.EndOfLine)
                    {
                        Pop();
                    }
                }
            }
            catch (Exception ex)
            {
                result = new HDSLResult(new HDSLLogBase[] { new HDSLLogBase(-1, -1, $"Exception thrown: {ex}") });
            }

            if (_errors.Count > 0)
            {
                result = new HDSLResult(_errors);
            }
            else
            {
                result = new HDSLResult(results);
            }

            return result;
        }

        #region Utility Methods

        /// <summary>
        /// Returns the DiskItemRecord table
        /// </summary>
        /// <returns></returns>
        private ILiteCollection<DiskItem> GetTable()
        {
            var records = _db.GetCollection<DiskItem>(DiskScan.TableName);
            return records;
        }

        /// <summary>
        /// Returns a value indicating whether or not there are more tokens beyond the given minimum
        /// </summary>
        /// <param name="min">The minimum number of tokens to test for</param>
        /// <returns>True if there are more than min, false otherwise</returns>
        private bool More(int min = 0)
        {
            return _tokens.Count > min;
        }

        /// <summary>
        /// Peeks at the token at the given index
        /// </summary>
        /// <param name="offset">The index to look at</param>
        /// <returns>The token at the given location</returns>
        /// <exception cref="IndexOutOfRangeException" />
        private HDSLToken Peek(int offset = 0)
        {
            return _tokens.Peek(offset);
        }

        /// <summary>
        /// Removes and returns the number of tokens
        /// </summary>
        /// <param name="count">The number of tokens to return</param>
        /// <returns>The set of tokens</returns>
        /// <exception cref="IndexOutOfRangeException" />
        private HDSLToken[] Pop(int count)
        {
            if (count == 0) return new HDSLToken[] { };

            var t = new List<HDSLToken>();
            for (int i = 0; i < count; i++)
            {
                t.Add(_tokens.Pop());
            }

            return t.ToArray();
        }

        /// <summary>
        /// Removes and returns the first token
        /// </summary>
        /// <returns>The token</returns>
        /// <exception cref="IndexOutOfRangeException" />
        private HDSLToken Pop()
        {
            return _tokens.Pop();
        }

        #endregion

        #region Statement Handlers

        /// <summary>
        /// Handles interpretation of a purge statement
        /// 
        /// Purpose:
        /// A purge statement removes entries from the database
        /// 
        /// Syntax:
        /// purge [where clause];
        /// </summary>
        private void HandlePurgeStatement()
        {
            // eat the purge
            Pop();

            _db.BeginTrans();
            IEnumerable<DiskItem> targets;
            // the where clause is optional.
            // If pressent, it further filters the files selected from the path
            if (More() && Peek().Type == HDSLTokenTypes.Where)
            {
                targets = HandleWhereClause(null);
                var records = GetTable();
                foreach (var r in targets)
                {
                    records.Delete(r.Id);
                }
            }
            else
            {
                GetTable().DeleteAll();
            }

            _db.Commit();
        }

        /// <summary>
        /// Handles interpretation of a find statement
        /// 
        /// Purpose:
        /// Find statements query the database for files and return them
        /// 
        /// Syntax:
        /// find <file regular expression> in <path> where *stuffs*
        /// 
        /// find [file pattern] [in [path[, path, path]] - defaults to current] [where clause];
        /// </summary>
        /// <returns>The results find statement</returns>
        private DiskItem[] HandleFindStatement()
        {
            if (More() && Peek().Type == HDSLTokenTypes.Find)
            {
                Pop();
                // the wildcard expression defaults to "*.*".  Defining it explicitly is optional
                var wildcardExpression = "*.*";
                if (More() && Peek().Type == HDSLTokenTypes.String)
                {
                    wildcardExpression = Pop().Literal;
                }

                // the in clause is optional, and can take a comma seperated list of paths.
                // if left out then the system assumes the current directory.
                var targetPaths = new List<string>();
                if (More() && Peek().Type == HDSLTokenTypes.In)
                {
                    Pop();
                    // get the list of paths
                    while (More() && Peek().Type == HDSLTokenTypes.String)
                    {
                        targetPaths.Add(Pop().Literal);

                        // check if we have at least 2 more tokens remaining, one is a comma and the next is a string
                        // if so, then this is a list
                        if (More(2) &&
                            Peek().Type == HDSLTokenTypes.Comma &&
                            Peek(1).Type == HDSLTokenTypes.String)
                        {
                            // strip the comma so the loop continues
                            Pop();
                        }
                    }

                    // validate the list of paths to ensure they exist
                    foreach (var target in targetPaths)
                    {
                        try
                        {
                            if (!Directory.Exists(target))
                            {
                                _errors.Add(new HDSLLogBase(Peek().Column, Peek().Row, $"Invalid path: '{target}'."));
                            }
                        }
                        catch (IOException ex)
                        {
                            _errors.Add(new HDSLLogBase(Peek().Column, Peek().Row, $"Bad path: '{target}'."));
                        }
                    }

                    if (_errors.Count > 0)
                    {
                        return new DiskItem[] { };
                    }
                }

                var results = new List<DiskItem>();
                try
                {
                    results.AddRange
                        (from di in GetTable().FindAll().AsEnumerable()
                         where
                            targetPaths.Where(tp => di.Path.StartsWith(tp)).Any() &&
                            LikeOperator.LikeString(di.ItemName, wildcardExpression, Microsoft.VisualBasic.CompareMethod.Binary)
                         select di);
                }
                catch (Exception ex)
                {
                    _errors.Add(new HDSLLogBase(Peek().Column, Peek().Row, $"Exception encountered: '{ex}'."));
                }

                // the where clause is optional.
                // If pressent, it further filters the files selected from the path
                if (More() && Peek().Type == HDSLTokenTypes.Where)
                {
                    results = HandleWhereClause(results).ToList();
                }

                // Done
                return results.ToArray();
            }
            else
            {
                _errors.Add(new HDSLLogBase(Peek().Column, Peek().Row, "'find' expected."));
            }

            return new DiskItem[] { };
        }

        /// <summary>
        /// Consumes a where clause, filtering the provided disk items
        /// 
        /// If the items set is null, then it will query the entire database directly
        /// </summary>
        /// <param name="items">The disk items to filter</param>
        private IEnumerable<DiskItem> HandleWhereClause(IEnumerable<DiskItem> items)
        {
            if (items != null)
            {
                return items;
            }
            else
            {
                return GetTable().FindAll().AsEnumerable();
            }
        }

        #endregion
    }
}
