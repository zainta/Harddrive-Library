// Copyright (c) Zain Al-Ahmary.  All rights reserved.
// Licensed under the MIT License, (the "License"); you may not use this file except in compliance with the License. 
// You may obtain a copy of the License at https://mit-license.org/

using HDDL.Data;
using HDDL.HDSL;
using HDDL.UI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HDDL.Scanning
{
    public class IntegrityScanEventWrapper
    {
        /// <summary>
        /// The amount of time between updates to the progress display
        /// </summary>
        private const int DisplayUpdateInterval = 500;

        /// <summary>
        /// The wrapped scanner instance
        /// </summary>
        private IntegrityScan _scanner;

        /// <summary>
        /// Whether or not the wrapper is allowed to prompt for an initial disk scan
        /// </summary>
        bool _allowScanRequest;

        /// <summary>
        /// The data handler used by all scanners
        /// </summary>
        DataHandler _dh;

        /// <summary>
        /// Tracks whether or not the scan has completed
        /// </summary>
        bool _done;

        /// <summary>
        /// The paths to scan
        /// </summary>
        IEnumerable<FilteredLocationItem> _scanPaths;

        /// <summary>
        /// Primary method of feedback used to display progress
        /// </summary>
        private EventWrapperDisplayModes _displayMode;

        /// <summary>
        /// The UI element used for feedback (if applicable)
        /// </summary>
        private TextualUIElementBase _uiFeedbackDisplay;

        /// <summary>
        /// The results of the integrity check
        /// </summary>
        public HDSLQueryOutcome Result { get; private set; }

        /// <summary>
        /// Takes a premade DiskScan instance and wraps it
        /// </summary>
        /// <param name="handler">The datahandler to use with the integrity scan</param>
        /// <param name="scanPaths">The paths to start scans from</param>
        /// <param name="displayMode">The mode to use to output progress feedback</param>
        /// <param name="allowScanRequest">Whether or not the wrapper is allowed to prompt for an initial disk scan</param>
        public IntegrityScanEventWrapper(DataHandler handler, IEnumerable<FilteredLocationItem> scanPaths, bool allowScanRequest, EventWrapperDisplayModes displayMode)
        {
            _scanner = new IntegrityScan(handler, scanPaths);
            _uiFeedbackDisplay = null;
            _displayMode = displayMode;
            _dh = handler;
            _done = false;
            Result = null;

            _allowScanRequest = allowScanRequest;
            _scanPaths = scanPaths;
        }

        /// <summary>
        /// Performs the scan
        /// </summary>
        /// <returns>true on completion, false otherwise</returns>
        public bool Go()
        {
            return Initialize(_allowScanRequest, _scanPaths);
        }

        /// <summary>
        /// Performs initialization, binds to the scanner's events and ensures that everything is ready
        /// </summary>
        /// <param name="allowScanRequest">Whether or not the wrapper is allowed to prompt for an initial disk scan</param>
        /// <param name="scanPaths">The paths to start scans from</param>
        private bool Initialize(bool allowScanRequest, IEnumerable<FilteredLocationItem> scanPaths = null)
        {
            _done = false;
            var performFreshScan = false;
            var allowPrompts = _displayMode != EventWrapperDisplayModes.Displayless;
            if (allowScanRequest && _displayMode != EventWrapperDisplayModes.Displayless)
            {
                if (_dh.GetDiskItemCount() == 0)
                {
                    Console.Write($"Database '{_dh.ConnectionString}' is empty.  Would you like to perform an initial scan on each of the provided locations? (y/n): ");
                    ConsoleKeyInfo k;
                    int origX = Console.CursorLeft, origY = Console.CursorTop;
                    do
                    {
                        k = Console.ReadKey();
                        Console.CursorLeft = origX;
                        Console.CursorTop = origY;
                    } while (
                        char.ToLower(k.KeyChar) != 'y' &&
                        char.ToLower(k.KeyChar) != 'n');

                    switch (char.ToLower(k.KeyChar))
                    {
                        case 'y':
                            Console.WriteLine();
                            performFreshScan = true;
                            break;
                        case 'n':
                            Console.WriteLine();
                            Console.WriteLine("Unable to perform integrity scan.  No records to populate.");
                            return false;
                    }
                }
            }

            var scanLocations = from sp in scanPaths select sp.Target;
            // do we need to do a disk scan first?
            if (performFreshScan)
            {
                DiskScanEventWrapper dsew = new DiskScanEventWrapper(_dh, scanLocations, false, _displayMode);
                if (!dsew.Go())
                {
                    return false;
                }
            }

            if (_scanner == null)
            {
                _scanner = new IntegrityScan(_dh, _scanPaths);
            }

            if (allowPrompts)
            {
                switch (_displayMode)
                {
                    case EventWrapperDisplayModes.ProgressBar:
                    case EventWrapperDisplayModes.Spinner:
                    case EventWrapperDisplayModes.Text:
                        Console.Write($"Performing integrity scans on '{string.Join("\', \'", scanLocations)}\' - ");
                        break;
                }
            }

            if (_displayMode != EventWrapperDisplayModes.Displayless)
            {
                _scanner.Preparing += _scanner_Preparing;
                _scanner.ScanStarts += _scanner_ScanStarts;
                _scanner.ScanEnds += _scanner_ScanEnds;
                _scanner.UnknownErrorOccurred += _scanner_UnknownErrorOccurred;
                _scanner.FileScanFailed += _scanner_FileScanFailed;
                _scanner.StartedScanningFile += _scanner_StartedScanningFile;
                _scanner.FinishedScanningFile += _scanner_FinishedScanningFile;
            }
            Task.Run(() => _scanner.StartScan(1));

            while (
                !_done &&
                (_scanner.Status == ScanStatus.Ready ||
                _scanner.Status == ScanStatus.InitiatingScan ||
                _scanner.Status == ScanStatus.Scanning))
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

        private void _scanner_FinishedScanningFile(IntegrityScan scanner, string target, long total, long current)
        {
            //switch (_displayMode)
            //{
            //    case EventWrapperDisplayModes.Text:
            //        Console.WriteLine($"Scanning '{target}'...");
            //        break;
            //}
        }

        private void _scanner_StartedScanningFile(IntegrityScan scanner, string target, long total, long current)
        {
            switch (_displayMode)
            {
                case EventWrapperDisplayModes.Text:
                    Console.WriteLine($"Finished scanning '{target}' -- Success.");
                    break;
            }
        }

        private void _scanner_FileScanFailed(IntegrityScan scanner, string target, Exception cause)
        {
            switch (_displayMode)
            {
                case EventWrapperDisplayModes.Text:
                    Console.WriteLine($"Failed to scan target '{target}' due to exception: {cause}");
                    break;
            }
        }

        private void _scanner_UnknownErrorOccurred(IntegrityScan scanner, Exception ex)
        {
            switch (_displayMode)
            {
                case EventWrapperDisplayModes.Text:
                    Console.WriteLine($"An error occurred: {ex}");
                    break;
            }
        }

        private void _scanner_ScanEnds(IntegrityScan scanner, IEnumerable<DiskItem> changedFiles, IEnumerable<DiskItem> scannedFiles)
        {
            switch (_displayMode)
            {
                case EventWrapperDisplayModes.ProgressBar:
                    Console.WriteLine($"-- Done! {new String(' ', ((ProgressBar)_uiFeedbackDisplay).Width - 7)}");
                    break;
                case EventWrapperDisplayModes.Spinner:
                case EventWrapperDisplayModes.Text:
                    Console.WriteLine("-- Done!");
                    break;
            }
            Result = new IntegrityScanResultSet(changedFiles, scannedFiles);

            _done = true;
        }

        private void _scanner_ScanStarts(IntegrityScan scanner, int total)
        {
            switch (_displayMode)
            {
                case EventWrapperDisplayModes.ProgressBar:
                    _uiFeedbackDisplay = new ProgressBar(_uiFeedbackDisplay.X, _uiFeedbackDisplay.Y, 60, 0, 0, total);
                    break;
                case EventWrapperDisplayModes.Spinner:
                    _uiFeedbackDisplay = new Spinner(_uiFeedbackDisplay.X, _uiFeedbackDisplay.Y);
                    break;
            }
        }

        private void _scanner_Preparing(IntegrityScan scanner, IEnumerable<string> targets)
        {
            switch (_displayMode)
            {
                case EventWrapperDisplayModes.ProgressBar:
                case EventWrapperDisplayModes.Spinner:
                case EventWrapperDisplayModes.Text:
                    _uiFeedbackDisplay = new Spinner(Console.CursorLeft, Console.CursorTop);
                    break;
            }
        }

        #endregion
    }
}
