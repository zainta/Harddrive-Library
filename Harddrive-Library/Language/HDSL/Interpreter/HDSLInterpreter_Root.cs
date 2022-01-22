// Copyright (c) Zain Al-Ahmary.  All rights reserved.
// Licensed under the MIT License, (the "License"); you may not use this file except in compliance with the License. 
// You may obtain a copy of the License at https://mit-license.org/

using HDDL.Collections;
using HDDL.Data;
using HDDL.Language.HDSL.Results;
using System;
using System.Collections.Generic;
using System.Text;

namespace HDDL.Language.HDSL.Interpreter
{
    /// <summary>
    /// Contains the HDSLInterpreter class' fields, main methods, constructors, and destructors
    /// </summary>
    partial class HDSLInterpreter
    {
        /// <summary>
        /// The stored tokens
        /// </summary>
        private ListStack<HDSLToken> _tokens;

        /// <summary>
        /// The data handler instance used for operations
        /// </summary>
        private DataHandler _dh;

        /// <summary>
        /// The list of errors
        /// </summary>
        private List<LogItemBase> _encounteredErrors;

        /// <summary>
        /// The currently in progress statement
        /// </summary>
        private StringBuilder _currentStatement;

        /// <summary>
        /// Creates an interpreter using the provided tokenizer's tokens and the provided file database
        /// </summary>
        /// <param name="tokenizer">The tokenizer whose tokens should be consumed</param>
        /// <param name="dh">The data handler to use</param>
        public HDSLInterpreter(ListStack<HDSLToken> tokens, DataHandler dh)
        {
            _tokens = new ListStack<HDSLToken>(tokens.ToList());
            _dh = dh;
            _encounteredErrors = new List<LogItemBase>();
        }

        /// <summary>
        /// Interprets the tokens against the provided database
        /// </summary>
        /// <param name="closeDb">Whether or not to close the database upon completion</param>
        /// <returns>The results of the interpretation</returns>
        public HDSLOutcomeSet Interpret(bool closeDb)
        {
            HDSLOutcomeSet badOutcome = null;
            var successOutcome = new HDSLOutcomeSet();
            _currentStatement = new StringBuilder();

            try
            {
                var done = false;

                while (!_tokens.Empty && NoErrors() && !done)
                {
                    _currentStatement.Clear();

                    switch (Peek().Type)
                    {
                        case HDSLTokenTypes.MultiLineComment:
                        case HDSLTokenTypes.Comment:
                            Pop();
                            break;
                        case HDSLTokenTypes.BookmarkReference:
                            HandleBookmarkDefinitionStatement();
                            break;
                        case HDSLTokenTypes.Purge:
                            HandlePurgeStatement();
                            break;
                        case HDSLTokenTypes.Find:
                            {
                                var intermediate = HandleFindStatement(true);
                                if (intermediate != null)
                                {
                                    successOutcome.Add(AddStatement(intermediate.AsOutcome()));
                                }
                            }
                            break;
                        case HDSLTokenTypes.Scan:
                            successOutcome.Add(HandleScanStatement());
                            break;
                        case HDSLTokenTypes.EndOfFile:
                            Pop();
                            done = true;
                            break;
                        case HDSLTokenTypes.EndOfLine:
                            Pop();
                            break;
                        case HDSLTokenTypes.Include:
                            HandleIncludeStatement();
                            break;
                        case HDSLTokenTypes.Exclude:
                            HandleExcludeStatement();
                            break;
                        case HDSLTokenTypes.Check:
                            successOutcome.Add(HandleIntegrityCheck());
                            break;
                        case HDSLTokenTypes.Ward:
                            HandleWardDefinition();
                            break;
                        case HDSLTokenTypes.Watch:
                            HandleWatchDefinition();
                            break;
                        case HDSLTokenTypes.Set:
                            HandleSetStatement();
                            break;
                        case HDSLTokenTypes.Reset:
                            HandleResetStatement();
                            break;
                        default:
                            Report(new LogItemBase(Peek().Column, Peek().Row, $"Unexpected token '{Peek().Code}'."));
                            done = true;
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                badOutcome = new HDSLOutcomeSet(new LogItemBase[] { new LogItemBase(-1, -1, $"Exception thrown: {ex}") });
            }

            if (!NoErrors())
            {
                badOutcome = new HDSLOutcomeSet(_encounteredErrors);
            }

            if (!NoErrors())
            {
                return badOutcome;
            }
            else
            {
                return successOutcome;
            }
        }
    }
}
