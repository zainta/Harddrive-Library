// Copyright (c) Zain Al-Ahmary.  All rights reserved.
// Licensed under the MIT License, (the "License"); you may not use this file except in compliance with the License. 
// You may obtain a copy of the License at https://mit-license.org/

using HDDL.Data;
using HDDL.HDSL;
using HDDL.IO.Disk;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

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
        /// Creates a Scanner Kernal
        /// </summary>
        /// <param name="dbPath">The path to an existing database file</param>
        /// <param name="freshStartScript">The path to a script, or the actual HDSL code of a script to run if there is no existing database</param>
        /// <param name="messenging">The kinds and styles of messages that will be relayed</param>
        public ScannerKernal(string dbPath, string freshStartScript, MessagingModes messenging) : base(messenging)
        {
            if (!File.Exists(dbPath))
            {
                DataHandler.InitializeDatabase(dbPath);
                _dh = new DataHandler(dbPath);
                if (!string.IsNullOrWhiteSpace(freshStartScript))
                {
                    if (File.Exists(freshStartScript))
                    {
                        HDSLProvider.ExecuteScript(freshStartScript, _dh);
                    }
                    else
                    {
                        HDSLProvider.ExecuteCode(freshStartScript, _dh);
                    }
                }
            }
            else
            {
                _dh = new DataHandler(dbPath);
            }

            Initialize();
        }

        /// <summary>
        /// Creates a Scanner Kernal with an existing datahandler
        /// </summary>
        /// <param name="dh"></param>
        /// <param name="messenging">The kinds and styles of messages that will be relayed</param>
        public ScannerKernal(DataHandler dh, MessagingModes messenging) : base(messenging)
        {
            _dh = dh;
            Initialize();
        }

        /// <summary>
        /// Starts the kernal's sub components
        /// </summary>
        public void Start()
        {
            _monitor?.Start();
            _watchers?.ForEach(w => w.Start());
        }

        /// <summary>
        /// Stops the kernal's sub components
        /// </summary>
        public void Stop()
        {
            _monitor?.Stop();
            _watchers?.ForEach(w => w.Stop());
        }

        #region Utility Methods

        /// <summary>
        /// Reads the database and instatiates all of the monitoring systems
        /// </summary>
        private void Initialize()
        {
            _watchers = new List<NonSpammingFileSystemWatcher>();

            HandleWatches();
            //HandleWards();
        }

        /// <summary>
        /// Handles generating the wards and their passive monitors
        /// </summary>
        private void HandleWards()
        {
            _monitor = new IntegrityMonitorSymphony(_dh, GetMessagingMode());
            _monitor.MessageRelayed += _monitor_MessageRelayed;
        }

        /// <summary>
        /// Handles generating the watches and their passive monitors
        /// </summary>
        private void HandleWatches()
        {
            // get all watches that do not overlap
            var watches = new List<WatchItem>();
            foreach (var watch in _dh.GetWatches())
            {
                if (watches.Count > 0)
                {
                    if (!PathHelper.IsWithinPaths(watch.Path, from w in _dh.GetWatches() select w.Path))
                    {
                        watches.Add(watch);
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
        }

        /// <summary>
        /// Adds a FileSystemWatcher for the given watch to the list of watches
        /// </summary>
        /// <param name="watch">The watch to add a watcher for</param>
        private void AddWatch(WatchItem watch)
        {
            if (Directory.Exists(watch.Path))
            {
                if (!watch.InPassiveMode)
                {
                    HDSLProvider.ExecuteCode($"scan quiet '{watch.Path}';", _dh);
                    watch.InPassiveMode = true;
                    _dh.Update(watch);
                }

                try
                {
                    var watcher = new NonSpammingFileSystemWatcher(watch.Path, GetMessagingMode());
                    watcher.ReportDiskEvent += Watcher_ReportDiskEvent;
                    watcher.MessageRelayed += Watcher_MessageRelayed;
                    _watchers.Add(watcher);
                }
                catch (Exception ex)
                {
                    Error($"Failed to start watcher process for path '{watch.Path}'.", ex);
                }
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
                            Issue($"Failed a scan on '{e.FullPath}'.");
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

                watcher.ReportDiskEvent -= Watcher_ReportDiskEvent;
                watcher.MessageRelayed -= Watcher_MessageRelayed;
                _watchers.Remove(watcher);

                var watch = _dh.GetWatches().Where(w => w.Path == watcher.GetPath()).SingleOrDefault();
                if (watch != null)
                {
                    AddWatch(watch);
                    Inform($"Successfully recycled watcher for path '{watcher.GetPath()}'.");
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

                Inform($"Successfully recycled integrity scan queue monitor.");
            }
            else
            {
                Forward(message);
            }
        }

        #endregion

        /// <summary>
        /// Shuts down the Scanner Kernal
        /// </summary>
        public void Dispose()
        {
            foreach (var watcher in _watchers)
            {
                watcher.Stop();
            }

            _dh.Dispose();
        }
    }
}
