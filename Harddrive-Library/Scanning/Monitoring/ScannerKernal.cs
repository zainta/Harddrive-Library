﻿// Copyright (c) Zain Al-Ahmary.  All rights reserved.
// Licensed under the MIT License, (the "License"); you may not use this file except in compliance with the License. 
// You may obtain a copy of the License at https://mit-license.org/

using HDDL.Data;
using HDDL.HDSL;
using HDDL.IO.Disk;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace HDDL.Scanning.Monitoring
{
    /// <summary>
    /// A centralized logic hub for scanning.  Tracks an area, ensures exclusions are followed, takes Wards into account, and maintains integrity checks
    /// </summary>
    public class ScannerKernal : ReporterBase, IDisposable
    {
        /// <summary>
        /// The watcher used for monitoring
        /// </summary>
        private List<NonSpammingFileSystemWatcher> _watchers;

        /// <summary>
        /// The integrity check manager
        /// </summary>
        private IntegrityMonitorSymphony _monitor;

        /// <summary>
        /// The data handler used for database management
        /// </summary>
        private DataHandler _dh;

        /// <summary>
        /// The path to the database the data handler will consume
        /// </summary>
        private string _dbPath;

        /// <summary>
        /// How the kernal deals with side-loading
        /// </summary>
        private ScriptLoadingDetails _sideLoadDetails;

        /// <summary>
        /// Handles watching for scripts (only checks creation) for side-load scripts
        /// </summary>
        private NonSpammingFileSystemWatcher _sideLoadWatcher;

        /// <summary>
        /// Whether the ScannerKernal should narrate its internal processes
        /// </summary>
        private bool _narrateProgress;

        /// <summary>
        /// The scanner kernal's activity state
        /// </summary>
        public bool Active { get; private set; }

        /// <summary>
        /// Creates a Scanner Kernal
        /// </summary>
        /// <param name="dbPath">The path to an existing database file</param>
        /// <param name="sideLoadDetails">How the kernal should handle side-loading (loading scripts after the initial database setup and during execution)</param>
        /// <param name="messenging">The kinds and styles of messages that will be relayed</param>
        /// <param name="narrateProgress">Whether the ScannerKernal should narrate its internal processes</param>
        public ScannerKernal(
            string dbPath,
            ScriptLoadingDetails sideLoadDetails,
            MessagingModes messenging,
            bool narrateProgress) : base(messenging)
        {
            _narrateProgress = narrateProgress;
            _sideLoadWatcher = null;
            _dbPath = dbPath;
            _sideLoadDetails = sideLoadDetails;
            Active = false;
        }

        /// <summary>
        /// Creates a Scanner Kernal with an existing datahandler
        /// </summary>
        /// <param name="dh"></param>
        /// <param name="sideLoadDetails">How the kernal should handle side-loading (loading scripts after the initial database setup and during execution)</param>
        /// <param name="messenging">The kinds and styles of messages that will be relayed</param>
        public ScannerKernal(DataHandler dh, ScriptLoadingDetails sideLoadDetails, MessagingModes messenging) : base(messenging)
        {
            _sideLoadWatcher = null;
            _dh = dh;
            _sideLoadDetails = sideLoadDetails;
            Active = false;
        }

        #region Control

        /// <summary>
        /// Starts the kernal's sub components
        /// </summary>
        public void Start()
        {
            if (_narrateProgress)
            {
                Inform("Starting.");
            }
            SilentStart();
        }

        /// <summary>
        /// Stops the kernal's sub components
        /// </summary>
        public void Stop()
        {
            if (_narrateProgress)
            {
                Inform("Stopping.");
            }
            SilentStop();
        }

        /// <summary>
        /// Starts the kernal's sub components
        /// </summary>
        private void SilentStart()
        {
            _monitor?.Start();
            _watchers?.ForEach(w => w.Start());

            if (_sideLoadWatcher != null)
            {
                _sideLoadWatcher.Start();
            }

            Active = true;
        }

        /// <summary>
        /// Stops the kernal's sub components
        /// </summary>
        private void SilentStop()
        {
            _monitor?.Stop();
            _watchers?.ForEach(w => w.Stop());

            if (_sideLoadWatcher != null)
            {
                _sideLoadWatcher.Stop();
            }

            Active = false;
        }

        /// <summary>
        /// Purges all monitoring infrastructure for recreation
        /// </summary>
        private void Reset()
        {
            SilentStop();

            if (_monitor != null)
            {
                _monitor.MessageRelayed += _monitor_MessageRelayed;
                _monitor.Stop();
                _monitor = null;
            }

            if (_watchers != null)
            {
                var i = 0;
                while (i < _watchers.Count)
                {
                    RemoveWatcher(_watchers[i]);
                }
                _watchers = null;
            }

            if (_sideLoadWatcher != null)
            {
                _sideLoadWatcher.ReportDiskEvent -= _sideLoadWatcher_ReportDiskEvent;
                _sideLoadWatcher.MessageRelayed -= _sideLoadWatcher_MessageRelayed;
                _sideLoadWatcher.Stop();
                _sideLoadWatcher = null;
            }
        }

        #endregion

        #region Utility Methods

        /// <summary>
        /// Causes the ScannerKernal to execute a chunk of code
        /// </summary>
        /// <param name="code">The HDSL code to execute</param>
        /// <returns>The code's outcome</returns>
        public HDSLResult Execute(string code)
        {
            if (_narrateProgress)
            {
                Inform("Pausing for side-load...");
            }

            HDSLResult result = null;
            try
            {
                Reset();
                result = HDSLProvider.ExecuteCode(code, _dh);
                CallHandles();
            }
            catch (Exception ex)
            {
                Error($"An error occurred while performing a code request.", ex);
            }

            if (_narrateProgress)
            {
                Inform("Resuming...");
            }
            SilentStart();

            return result;
        }

        /// <summary>
        /// Reads the database and instatiates all of the monitoring systems
        /// </summary>
        /// <returns>True if initialization was successful, false otherwise</returns>
        public bool Initialize()
        {
            if (!File.Exists(_dbPath))
            {
                DataHandler.InitializeDatabase(_dbPath);
                _dh = new DataHandler(_dbPath);
                if (!string.IsNullOrWhiteSpace(_sideLoadDetails.InitialLoadSource))
                {
                    if (_narrateProgress)
                    {
                        Inform($"Executing initial database script: '{_sideLoadDetails.InitialLoadSource}'.");
                    }
                    HDSLResult result = null;
                    if (File.Exists(_sideLoadDetails.InitialLoadSource))
                    {
                        result = HDSLProvider.ExecuteScript(_sideLoadDetails.InitialLoadSource, _dh);
                    }
                    else
                    {
                        result = HDSLProvider.ExecuteCode(_sideLoadDetails.InitialLoadSource, _dh);
                    }
                    if (!ReportResultProblem(result, _sideLoadDetails.InitialLoadSource)) return false;
                }
            }
            else
            {
                _dh = new DataHandler(_dbPath);
            }

            if (!CallHandles()) return false;

            return true;
        }

        /// <summary>
        /// Handles execution of side-load scripts
        /// </summary>
        /// <returns>True upon successful execution of all scripts, false otherwise</returns>
        private bool HandleSideLoad()
        {
            // when side loading, we need to make sure everything is completely shutdown
            Reset();

            var failed = false;
            foreach (var file in _sideLoadDetails.GetScripts())
            {
                if (_narrateProgress)
                {
                    Inform($"Side-loading '{file}'.");
                }
                var result = HDSLProvider.ExecuteScript(file, _dh);
                if (ReportResultProblem(result, file))
                {
                    // if we successfully run the script then mark the file as consumed
                    if (_sideLoadDetails.DeleteSideLoadSource)
                    {
                        try
                        {
                            File.Delete(file);
                        }
                        catch (Exception ex)
                        {
                            Error($"Failed to delete side-load script '{file}'.", ex);
                            failed = true;
                        }
                    }
                    else
                    {
                        try
                        {
                            File.Move(file, $"{file}{_sideLoadDetails.SideLoadScriptCompletionExtension}");
                        }
                        catch (Exception ex)
                        {
                            Error($"Failed to rename side-load script '{file}' to '{file}{_sideLoadDetails.SideLoadScriptCompletionExtension}'.", ex);
                            failed = true;
                        }
                    }
                }
            }

            if (!failed && _sideLoadDetails.MonitorDuringRuntime)
            {
                AddSideLoadWatcher();
            }

            return !failed;
        }

        /// <summary>
        /// Handles generating the wards and their passive monitors
        /// </summary>
        private bool HandleWards()
        {
            if (_narrateProgress)
            {
                Inform("Loading passive integrity monitor.");
            }

            _monitor = new IntegrityMonitorSymphony(_dh, GetMessagingMode());
            _monitor.MessageRelayed += _monitor_MessageRelayed;

            return true;
        }

        /// <summary>
        /// Handles generating the watches and their passive monitors
        /// </summary>
        private bool HandleWatches()
        {
            if (_watchers == null)
            {
                _watchers = new List<NonSpammingFileSystemWatcher>();
            }

            if (_narrateProgress)
            {
                Inform("Loading watches.");
            }

            // get all watches that do not overlap
            var watches = new List<WatchItem>();
            foreach (var watch in _dh.GetWatches().OrderBy(w => w.Path))
            {
                if (watches.Count > 0)
                {
                    if (!PathHelper.IsWithinPaths(watch.Path, from w in watches select w.Path))
                    {
                        watches.Add(watch);
                    }
                    else if (_narrateProgress)
                    {
                        Inform($"Watch '{watch.Path}' was superseded and will not be loaded.");
                    }
                }
                else
                {
                    watches.Add(watch);
                }
            }

            // generate all necessary watchers
            foreach (var watch in watches)
            {
                AddWatch(watch);
            }

            _dh.WriteWatches();
            return true;
        }

        #endregion

        #region Management Methods

        /// <summary>
        /// Calls the handler methods
        /// </summary>
        private bool CallHandles()
        {
            if (!HandleSideLoad()) return false;
            if (!HandleWatches()) return false;
            if (!HandleWards()) return false;

            return true;
        }

        /// <summary>
        /// Adds a FileSystemWatcher for the given watch to the list of watches
        /// </summary>
        /// <param name="watch">The watch to add a watcher for</param>
        /// <returns>The watcher instance made for the given item</returns>
        private NonSpammingFileSystemWatcher AddWatch(WatchItem watch)
        {
            NonSpammingFileSystemWatcher watcherResult = null;
            if (_narrateProgress)
            {
                Inform($"Loading watcher for '{watch.Path}'.");
            }

            if (Directory.Exists(watch.Path))
            {
                if (!watch.InPassiveMode)
                {
                    if (_narrateProgress)
                    {
                        Inform($"Performing initial scan for watcher '{watch.Path}'.");
                    }
                    var result = HDSLProvider.ExecuteCode($"scan quiet '{watch.Path.Replace("\\", "\\\\")}';", _dh);
                    if (ReportResultProblem(result, watch.Path))
                    {
                        watch.InPassiveMode = true;
                        _dh.Update(watch);
                    }
                }

                try
                {
                    watcherResult = new NonSpammingFileSystemWatcher(
                        watch.Path,
                        GetMessagingMode(),
                        _dh.GetProcessedExclusions().Select(e => e.Path));

                    watcherResult.ReportDiskEvent += Watcher_ReportDiskEvent;
                    watcherResult.MessageRelayed += Watcher_MessageRelayed;
                    _watchers.Add(watcherResult);
                }
                catch (Exception ex)
                {
                    Error($"Failed to start watcher process for path '{watch.Path}'.", ex);
                }
            }

            return watcherResult;
        }

        /// <summary>
        /// Removes the provided file system watcher from the list and properly shuts it down
        /// </summary>
        /// <param name="watcher">The watcher to remove</param>
        private void RemoveWatcher(NonSpammingFileSystemWatcher watcher)
        {
            watcher.ReportDiskEvent -= Watcher_ReportDiskEvent;
            watcher.MessageRelayed -= Watcher_MessageRelayed;
            watcher.Stop();
            _watchers.Remove(watcher);
        }

        /// <summary>
        /// Adds passive monitoring for the Side Load Source as defined in the details
        /// </summary>
        private void AddSideLoadWatcher()
        {
            if (PathHelper.EndsInDirSeperator(_sideLoadDetails.SideLoadSource))
            {
                // it's a directory
                _sideLoadWatcher = new NonSpammingFileSystemWatcher(_sideLoadDetails.SideLoadSource, GetMessagingMode());
                _sideLoadWatcher.ReportDiskEvent += _sideLoadWatcher_ReportDiskEvent;
                _sideLoadWatcher.MessageRelayed += _sideLoadWatcher_MessageRelayed;

                if (_narrateProgress)
                {
                    Inform($"Loaded side-load watcher for directory '{_sideLoadDetails.SideLoadSource}'.");
                }
            }
            else
            {
                // it's a file
                FileInfo fi = new FileInfo(_sideLoadDetails.SideLoadSource);

                _sideLoadWatcher = new NonSpammingFileSystemWatcher(
                    fi.DirectoryName,
                    GetMessagingMode(),
                    filter: fi.Name);
                _sideLoadWatcher.ReportDiskEvent += _sideLoadWatcher_ReportDiskEvent;
                _sideLoadWatcher.MessageRelayed += _sideLoadWatcher_MessageRelayed;

                if (_narrateProgress)
                {
                    Inform($"Loaded side-load watcher for file '{_sideLoadDetails.SideLoadSource}'.");
                }
            }

            if (Active)
            {
                _monitor.Start();
            }
        }

        #endregion

        #region Side Loading Watcher Events

        /// <summary>
        /// Receives disk events relayed from the side-loading watcher
        /// </summary>
        /// <param name="origin"></param>
        /// <param name="message"></param>
        private void _sideLoadWatcher_ReportDiskEvent(NonSpammingFileSystemWatcher origin, FileSystemWatcherEventNatures nature, FileSystemEventArgs e)
        {
            if (_narrateProgress)
            {
                Inform("Pausing for side-load...");
            }
            CallHandles();
            if (_narrateProgress)
            {
                Inform("Resuming...");
            }
            SilentStart();
        }

        /// <summary>
        /// Receives messages relayed from the side-loading watcher
        /// </summary>
        /// <param name="origin"></param>
        /// <param name="nature"></param>
        /// <param name="e"></param>
        private void _sideLoadWatcher_MessageRelayed(ReporterBase origin, MessageBundle message)
        {
            var watcher = origin as NonSpammingFileSystemWatcher;
            if (watcher != null &&
                message.Type == MessageTypes.Error)
            {
                Forward(message);

                watcher.ReportDiskEvent -= Watcher_ReportDiskEvent;
                watcher.MessageRelayed -= Watcher_MessageRelayed;
                _sideLoadWatcher.Stop();
                _sideLoadWatcher = null;

                AddSideLoadWatcher();
                Warn($"Successfully recycled the side-load watcher for path '{watcher.GetPath()}'.");
            }
            else
            {
                Forward(message);
            }
        }

        #endregion

        #region File System Watcher Events

        /// <summary>
        /// Monitors the watches and updates disk items when they report activity
        /// </summary>
        /// <param name="origin"></param>
        /// <param name="nature"></param>
        /// <param name="e"></param>
        private void Watcher_ReportDiskEvent(NonSpammingFileSystemWatcher origin, FileSystemWatcherEventNatures nature, FileSystemEventArgs e)
        {
            DiskItem di = null;
            // when a disk event is detected, we want to update the target with the new information
            switch (nature)
            {
                case FileSystemWatcherEventNatures.Deletion:
                    {
                        di = _dh.GetDiskItemByPath(e.FullPath);
                        if (di != null)
                        {
                            _dh.Delete(di);
                            _dh.WriteDiskItems();
                        }
                    }
                    break;
                case FileSystemWatcherEventNatures.Creation:
                case FileSystemWatcherEventNatures.Alteration:
                    {
                        var scanWrapper = new DiskScanEventWrapper(_dh, new string[] { e.FullPath }, false, EventWrapperDisplayModes.Displayless);
                        if (!scanWrapper.Go())
                        {
                            Warn($"Failed a scan on '{e.FullPath}'.");
                        }
                    }
                    break;
            }
        }

        /// <summary>
        /// Monitors the watches and reacts to or relays their messages
        /// </summary>
        /// <param name="origin"></param>
        /// <param name="message"></param>
        private void Watcher_MessageRelayed(ReporterBase origin, MessageBundle message)
        {
            var watcher = origin as NonSpammingFileSystemWatcher;
            if (watcher != null &&
                message.Type == MessageTypes.Error)
            {
                Forward(message);
                RemoveWatcher(watcher);

                var watch = _dh.GetWatches().Where(w => w.Path == watcher.GetPath()).SingleOrDefault();
                if (watch != null)
                {
                    var newWatcher = AddWatch(watch);
                    if (Active && newWatcher != null)
                    {
                        newWatcher.Start();
                    }
                    Warn($"Successfully recycled watcher for path '{watcher.GetPath()}'.");
                }
            }
            else
            {
                Forward(message);
            }
        }

        /// <summary>
        /// Monitors the integrity scan manager and reacts to or relays their messages
        /// </summary>
        /// <param name="origin"></param>
        /// <param name="message"></param>
        private void _monitor_MessageRelayed(ReporterBase origin, MessageBundle message)
        {
            var monitor = origin as IntegrityMonitorSymphony;
            if (monitor != null &&
                message.Type == MessageTypes.Error)
            {
                Forward(message);

                // unbind from old monitor
                monitor.MessageRelayed -= _monitor_MessageRelayed;

                // create a new one
                _monitor = new IntegrityMonitorSymphony(_dh, GetMessagingMode());
                _monitor.MessageRelayed += _monitor_MessageRelayed;
                monitor = _monitor;
                if (Active)
                {
                    _monitor.Start();
                }

                Warn($"Successfully recycled integrity scan queue monitor.");
            }
            else
            {
                Forward(message);
            }
        }

        #endregion

        /// <summary>
        /// Takes an HDSLResult instance and throws an exception if it contains errors
        /// </summary>
        /// <param name="result">The instance to check</param>
        /// <param name="scriptPath">The script that failed to execute</param>
        /// <returns>True if no errors, false otherwise</returns>
        private bool ReportResultProblem(HDSLResult result, string scriptPath)
        {
            if (result.Errors.Length > 0)
            {
                var sb = new StringBuilder();
                foreach (var error in result.Errors)
                {
                    if (sb.Length > 0)
                    {
                        sb.Append(", ");
                    }
                    sb.Append(error.ToString());
                }

                Error($"Failed to correctly execute script '{scriptPath}'.\n\nErrors: {sb}",
                        new Exception($"Failed to correctly execute script '{scriptPath}'.\n\nErrors: {sb}"));

                return false;
            }

            return true;
        }

        /// <summary>
        /// Shuts down the Scanner Kernal
        /// </summary>
        public void Dispose()
        {
            Reset();
            _dh.Dispose();
        }
    }
}
