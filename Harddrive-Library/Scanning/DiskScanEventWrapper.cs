// Copyright (c) Zain Al-Ahmary.  All rights reserved.
// Licensed under the MIT License, (the "License"); you may not use this file except in compliance with the License. 
// You may obtain a copy of the License at https://mit-license.org/

using HDDL.Data;
using HDDL.UI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace HDDL.Scanning
{
    /// <summary>
    /// Wrapper class for the DiskScan that handles the events and outputting to the Console.
    /// </summary>
    public class DiskScanEventWrapper
    {
        /// <summary>
        /// The amount of time between updates to the progress display
        /// </summary>
        private const int DisplayUpdateInterval = 500;

        /// <summary>
        /// The wrapped scanner instance
        /// </summary>
        private DiskScan _scanner;

        /// <summary>
        /// Whether or not the wrapper is allowed to prompt for database recreation
        /// </summary>
        bool _allowRecreation;

        /// <summary>
        /// Stored connection string to the database
        /// </summary>
        string _dbPath;

        /// <summary>
        /// The paths to scan
        /// </summary>
        IEnumerable<string> _scanPaths;

        /// <summary>
        /// Primary method of feedback used to display progress
        /// </summary>
        private DiskScanEventWrapperDisplayModes _displayMode;

        /// <summary>
        /// The UI element used for feedback (if applicable)
        /// </summary>
        private TextualUIElementBase _uiFeedbackDisplay;

        /// <summary>
        /// Takes a premade DiskScan instance and wraps it
        /// </summary>
        /// <param name="premade">The diskscan instance to monitor</param>
        /// <param name="displayMode">The mode to use to output progress feedback</param>
        /// <param name="allowRecreation">Whether or not to prompt for, and perform, database recreation if it already exists</param>
        public DiskScanEventWrapper(DiskScan premade, bool allowRecreation, DiskScanEventWrapperDisplayModes displayMode)
        {
            _scanner = premade;
            _uiFeedbackDisplay = null;
            _displayMode = displayMode;

            _allowRecreation = allowRecreation;
            _dbPath = null;
            _scanPaths = null;
        }

        /// <summary>
        /// Takes a premade DiskScan instance and wraps it
        /// </summary>
        /// <param name="handler">The datahandler to use with the disk scan</param>
        /// <param name="scanPaths">The paths to start scans from</param>
        /// <param name="displayMode">The mode to use to output progress feedback</param>
        /// <param name="allowRecreation">Whether or not to prompt for, and perform, database recreation if it already exists</param>
        public DiskScanEventWrapper(DataHandler handler, IEnumerable<string> scanPaths, bool allowRecreation, DiskScanEventWrapperDisplayModes displayMode)
        {
            _scanner = new DiskScan(handler, scanPaths);
            _uiFeedbackDisplay = null;
            _displayMode = displayMode;

            _allowRecreation = allowRecreation;
            _dbPath = null;
            _scanPaths = scanPaths;
        }

        /// <summary>
        /// Takes a premade DiskScan instance and wraps it
        /// </summary>
        /// <param name="dbPath">Where the tracking database is located</param>
        /// <param name="scanPaths">The paths to start scans from</param>
        /// <param name="displayMode">The mode to use to output progress feedback</param>
        /// <param name="allowRecreation">Whether or not to prompt for, and perform, database recreation if it already exists</param>
        public DiskScanEventWrapper(string dbPath, IEnumerable<string> scanPaths, bool allowRecreation, DiskScanEventWrapperDisplayModes displayMode)
        {
            _scanner = null;
            _uiFeedbackDisplay = null;
            _displayMode = displayMode;

            _allowRecreation = allowRecreation;
            _dbPath = dbPath;
            _scanPaths = scanPaths;
        }

        /// <summary>
        /// Performs the scan
        /// </summary>
        /// <returns>true on completion, false othrewise</returns>
        public bool Go()
        {
            return Initialize(_allowRecreation, _dbPath, _scanPaths.Where(sp => File.Exists(sp) || Directory.Exists(sp)));
        }

        /// <summary>
        /// Performs initialization, binds to the scanner's events and ensures that everything is ready
        /// </summary>
        /// <param name="allowRecreation">Whether or not to prompt for, and perform, database recreation if it already exists</param>
        /// <param name="dbPath">Where the tracking database is located</param>
        /// <param name="scanPaths">The paths to start scans from</param>
        private bool Initialize(bool allowRecreation, string dbPath = null, IEnumerable<string> scanPaths = null)
        {
            var recreate = false;
            var allowPrompts = _displayMode != DiskScanEventWrapperDisplayModes.Displayless;
            if (allowRecreation && _displayMode != DiskScanEventWrapperDisplayModes.Displayless)
            {
                if (File.Exists(dbPath) &&
                    !string.IsNullOrWhiteSpace(dbPath))
                {
                    // If the file exists, the scan will be significantly slower.
                    // Ask the user if they would like to recreate the database, rather than update it.
                    Console.Write($"Database '{dbPath}' already exists.  This will significantly slow the operation.\nWould you like to recreate the database? (y/n/c): ");
                    ConsoleKeyInfo k;
                    int origX = Console.CursorLeft, origY = Console.CursorTop;
                    do
                    {
                        k = Console.ReadKey();
                        Console.CursorLeft = origX;
                        Console.CursorTop = origY;
                    } while (
                        char.ToLower(k.KeyChar) != 'y' &&
                        char.ToLower(k.KeyChar) != 'n' &&
                        char.ToLower(k.KeyChar) != 'c');

                    switch (char.ToLower(k.KeyChar))
                    {
                        case 'c':
                            return false;
                        case 'y':
                            recreate = true;
                            break;
                        case 'n':
                            recreate = false;
                            break;
                    }
                    Console.WriteLine();
                }
            }

            if (_scanner == null)
            {
                if (scanPaths.Count() > 0)
                {
                    var paths = (from p in scanPaths where !string.IsNullOrWhiteSpace(p) select p).ToArray();
                    _scanner = new DiskScan(dbPath, paths);

                    if (recreate && allowPrompts)
                    {
                        Console.Write($"Resetting database...");
                    }
                    DataHandler.InitializeDatabase(dbPath, recreate && allowRecreation);

                    if (recreate && allowPrompts)
                    {
                        Console.WriteLine("Done.");
                    }
                }
                else
                {
                    return false;
                }
            }

            if (allowPrompts)
            {
                switch (_displayMode)
                {
                    case DiskScanEventWrapperDisplayModes.ProgressBar:
                        Console.Write($"Performing scans on '{string.Join("\', \'", _scanner.ScanTargets)}\' - ");
                        break;
                    case DiskScanEventWrapperDisplayModes.Spinner:
                        Console.Write($"Performing scans on '{string.Join("\', \'", _scanner.ScanTargets)}\' - ");
                        break;
                    case DiskScanEventWrapperDisplayModes.Text:
                        Console.Write($"Performing scans on '{string.Join("\', \'", _scanner.ScanTargets)}\' - ");
                        break;
                }
            }

            if (_displayMode != DiskScanEventWrapperDisplayModes.Displayless)
            {
                _scanner.ScanEventOccurred += Scanner_ScanEventOccurred;
                _scanner.ScanEnded += Scanner_ScanEnded;
                _scanner.ScanStarted += Scanner_ScanStarted;
                _scanner.ScanDatabaseActivityCompleted += Scanner_ScanDatabaseActivityCompleted;
                _scanner.DeletionsOccurred += Scanner_DeletionsOccurred;
                _scanner.ScanExplorationBegins += _scanner_ScanExplorationBegins;
                _scanner.ScanExplorationEnds += _scanner_ScanExplorationEnds;
                _scanner.NoValidScanPaths += _scanner_NoValidScanPaths;
            }
            _scanner.StartScan();

            while (_scanner.Status == ScanStatus.InitiatingScan ||
                _scanner.Status == ScanStatus.Scanning ||
                _scanner.Status == ScanStatus.Deleting)
            {
                if (allowPrompts)
                {
                    if (_scanner.Status == ScanStatus.Scanning || 
                        _scanner.Status == ScanStatus.InitiatingScan)
                    {
                        if (_uiFeedbackDisplay != null)
                        {
                            _uiFeedbackDisplay.Display();
                        }
                    }
                }

                System.Threading.Thread.Sleep(DisplayUpdateInterval);
            }

            return true;
        }

        #region Events

        private void _scanner_NoValidScanPaths(DiskScan scanner)
        {
            switch (_displayMode)
            {
                case DiskScanEventWrapperDisplayModes.ProgressBar:
                case DiskScanEventWrapperDisplayModes.Spinner:
                case DiskScanEventWrapperDisplayModes.Text:
                    Console.WriteLine("No valid scanning paths were provided.");
                    break;
            }
        }

        private void _scanner_ScanExplorationEnds(DiskScan scanner)
        {
            _uiFeedbackDisplay = null;
            switch (_displayMode)
            {
                case DiskScanEventWrapperDisplayModes.ProgressBar:
                case DiskScanEventWrapperDisplayModes.Spinner:
                case DiskScanEventWrapperDisplayModes.Text:
                    Console.WriteLine("Ready!");
                    break;
            }
        }

        private void _scanner_ScanExplorationBegins(DiskScan scanner)
        {
            switch (_displayMode)
            {
                case DiskScanEventWrapperDisplayModes.ProgressBar:
                case DiskScanEventWrapperDisplayModes.Spinner:
                case DiskScanEventWrapperDisplayModes.Text:
                    _uiFeedbackDisplay = new Spinner(Console.CursorLeft, Console.CursorTop);
                    break;
            }
        }

        private void Scanner_ScanStarted(DiskScan scanner, long directoryCount, long fileCount)
        {
            switch (_displayMode)
            {
                case DiskScanEventWrapperDisplayModes.ProgressBar:
                    _uiFeedbackDisplay = new ProgressBar(Console.CursorLeft, Console.CursorTop, 60, 0, 0, fileCount + directoryCount);
                    break;
                case DiskScanEventWrapperDisplayModes.Spinner:
                    _uiFeedbackDisplay = new Spinner(Console.CursorLeft, Console.CursorTop);
                    break;
            }
        }

        private void Scanner_ScanEnded(DiskScan scanner, long totalDeleted, Timings elapsed, ScanOperationOutcome outcome)
        {
            switch (_displayMode)
            {
                case DiskScanEventWrapperDisplayModes.ProgressBar:
                    Console.WriteLine($"-- Done! {new String(' ', ((ProgressBar)_uiFeedbackDisplay).Width - 7)}");
                    break;
                case DiskScanEventWrapperDisplayModes.Spinner:
                    Console.WriteLine("-- Done!");
                    break;
                case DiskScanEventWrapperDisplayModes.Text:
                    Console.WriteLine(string.Format("Done -- Total time: {0}", elapsed.GetScanDuration()));
                    break;
            }
        }

        private void Scanner_ScanEventOccurred(DiskScan scanner, ScanEvent evnt)
        {
            var itemType = evnt.IsFile ? "file" : "directory";

            if (evnt.Nature == ScanEventType.KeyNotDeleted ||
                evnt.Nature == ScanEventType.UnknownError)
            {
                // We aren't interested in these right now.  We'll implement this later.
                Console.WriteLine($"An error occurred: {evnt.Error}");
            }
            else if (evnt.Nature == ScanEventType.AddRequired)
            {
                switch (_displayMode)
                {
                    case DiskScanEventWrapperDisplayModes.ProgressBar:
                        ((ProgressBar)_uiFeedbackDisplay).Value++;
                        break;
                    case DiskScanEventWrapperDisplayModes.Text:
                        Console.WriteLine($"Discovered {itemType} @ '{evnt.Path}'.");
                        break;
                }
            }
            else if (evnt.Nature == ScanEventType.UpdateRequired)
            {
                switch (_displayMode)
                {
                    case DiskScanEventWrapperDisplayModes.ProgressBar:
                        ((ProgressBar)_uiFeedbackDisplay).Value++;
                        break;
                    case DiskScanEventWrapperDisplayModes.Text:
                        Console.WriteLine($"Rediscovered {itemType} @ '{evnt.Path}'.");
                        break;
                }
            }
            else if (evnt.Nature == ScanEventType.DatabaseError)
            {
                switch (_displayMode)
                {
                    case DiskScanEventWrapperDisplayModes.ProgressBar:
                        ((ProgressBar)_uiFeedbackDisplay).Value++;
                        break;
                    case DiskScanEventWrapperDisplayModes.Text:
                        Console.WriteLine($"!!Database Error!! {evnt.Error.Message}");
                        break;
                }
            }
        }

        private void Scanner_ScanDatabaseActivityCompleted(DiskScan scanner, long additions, long updates, long deletions)
        {
            switch (_displayMode)
            {
                case DiskScanEventWrapperDisplayModes.Text:
                    if (additions > 0)
                    {
                        Console.WriteLine($"Successfully added {additions} records to the database.");
                    }
                    if (updates > 0)
                    {
                        Console.WriteLine($"Successfully updated {updates} records in the database.");
                    }
                    if (deletions > 0)
                    {
                        Console.WriteLine($"Successfully deleted {deletions} records from the database.");
                    }
                    break;
            }
        }

        private void Scanner_DeletionsOccurred(DiskScan scanner, long total)
        {
            switch (_displayMode)
            {
                case DiskScanEventWrapperDisplayModes.Text:
                    if (total > 0)
                    {
                        Console.WriteLine($"{total} Old entries were Successfully expunged from the database.");
                    }
                    break;
            }
        }

        #endregion
    }
}
