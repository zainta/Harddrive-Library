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
        private HDDLDataContext _db;

        /// <summary>
        /// The list of errors
        /// </summary>
        private List<HDSLLogBase> _errors;

        /// <summary>
        /// Creates an interpreter using the provided tokenizer's tokens and the provided file database
        /// </summary>
        /// <param name="tokenizer">The tokenizer whose tokens should be consumed</param>
        /// <param name="db">The database to use</param>
        public HDSLInterpreter(ListStack<HDSLToken> tokens, HDDLDataContext db)
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
            var files = new List<string>();

            try
            {
                var done = false;

                while (!_tokens.Empty && !done)
                {
                    switch (Peek().Type)
                    {
                        case HDSLTokenTypes.Purge:
                            HandlePurgeStatement();
                            break;
                        case HDSLTokenTypes.Find:
                            files.AddRange(HandleFindStatement().Select(r => r.Path));
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

            if (_errors.Count == 0)
            {
                result = new HDSLResult(_errors);
            }
            else
            {
                result = new HDSLResult(files);
            }

            return result;
        }

        #region Utility Methods

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
        /// find <file regular expression> in <path> where *stuffs*
        /// </summary>
        private void HandlePurgeStatement()
        {
            // eat the purge
            Pop();

            _db.DiskItems.RemoveRange(_db.DiskItems);
            _db.SaveChanges();
        }

        /// <summary>
        /// Handles interpretation of a find statement
        /// 
        /// Purpose:
        /// Find statements query the database for files and return them
        /// 
        /// Syntax:
        /// find <file regular expression> in <path> where *stuffs*
        /// </summary>
        /// <returns>The results find statement</returns>
        private DiskItem[] HandleFindStatement()
        {
            List<DiskItem> results = new List<DiskItem>();

            if (More() && Peek().Type == HDSLTokenTypes.Find)
            {
                Pop();
                if (More() && Peek().Type == HDSLTokenTypes.String)
                {
                    var wildcardExpression = Pop().Literal;
                    
                    if (More() && Peek().Type == HDSLTokenTypes.In)
                    {
                        Pop();
                        if (More() && Peek().Type == HDSLTokenTypes.String)
                        {
                            var path = Pop().Literal;
                            var records = _db.DiskItems
                                .Where(r =>
                                    PathComparison.IsWithinPath(r.Path, path, true) &&
                                    LikeOperator.LikeString(r.ItemName, wildcardExpression, Microsoft.VisualBasic.CompareMethod.Binary))
                                .Select(r => r)
                                .ToArray();

                            results.AddRange(HandleWhereClause(records));
                        }
                        else
                        {
                            _errors.Add(new HDSLLogBase(Peek().Column, Peek().Row, $"'string' expected."));
                        }
                    }
                    else
                    {
                        _errors.Add(new HDSLLogBase(Peek().Column, Peek().Row, $"'in' expected."));
                    }
                }
                else
                {
                    _errors.Add(new HDSLLogBase(Peek().Column, Peek().Row, $"'string' token containing a file search string is expected."));
                }
            }
            else
            {
                _errors.Add(new HDSLLogBase(Peek().Column, Peek().Row, $"'find' expected."));
            }

            return results.ToArray();
        }

        /// <summary>
        /// Filters the given records based on the outstanding where clause
        /// </summary>
        /// <param name="records">The records to filter</param>
        /// <returns>The results of the where clause filtration</returns>
        private DiskItem[] HandleWhereClause(IEnumerable<DiskItem> records)
        {
            var results = new List<DiskItem>();

            if (More() && Peek().Type == HDSLTokenTypes.Where)
            {
                results.AddRange(records);
            }
            else
            {
                _errors.Add(new HDSLLogBase(Peek().Column, Peek().Row, $"'where' expected."));
            }

            return results.ToArray();
        }

        #endregion
    }
}
