// Copyright (c) Zain Al-Ahmary.  All rights reserved.
// Licensed under the MIT License, (the "License"); you may not use this file except in compliance with the License. 
// You may obtain a copy of the License at https://mit-license.org/

using HDDL.IO.Disk;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;

namespace HDDL.Scanning.Monitoring
{
    /// <summary>
    /// A File System Watcher wrapper class that does not spam duplicate events
    /// </summary>
    class NonSpammingFileSystemWatcher : ReporterBase
    {
        private const long MinOccuranceDifferenceValue = 100; // milliseconds

        /// <summary>
        /// The monitoring file system instance
        /// </summary>
        private FileSystemWatcher _watcher;

        /// <summary>
        /// The time a disk event occurred
        /// </summary>
        private DateTime _lastOccurance;

        /// <summary>
        /// the item on disk the last disk event referenced
        /// </summary>
        private string _lastTopic;

        /// <summary>
        /// The way in which the last item to be altered was altered
        /// </summary>
        private WatcherChangeTypes _lastAlteration;

        /// <summary>
        /// The path being monitored
        /// </summary>
        private string _path;

        /// <summary>
        /// Items that are excluded from reporting
        /// </summary>
        private IEnumerable<string> _exclusions;

        /// <summary>
        /// The non-spamming file system watcher's backlog of events
        /// </summary>
        public ConcurrentQueue<NonSpammingFileSystemWatcherEvent> Events { get; private set; }

        /// <summary>
        /// Creates a non-spamming file system watcher to monitor the given location
        /// </summary>
        /// <param name="path">The path to monitor</param>
        /// <param name="messenging">The kinds and styles of messages that will be relayed</param>
        /// <param name="filter">The file filter to use</param>
        /// <param name="exclusions">A list of files and directories to ignore modifications to</param>
        public NonSpammingFileSystemWatcher(string path, MessagingModes messenging, IEnumerable<string> exclusions = null, string filter = "*.*") : base(messenging)
        {
            Events = new ConcurrentQueue<NonSpammingFileSystemWatcherEvent>();
            _exclusions = exclusions == null ? new string[] { } : exclusions;
            _path = path;
            _watcher = new FileSystemWatcher();
            if (!string.IsNullOrWhiteSpace(path) && Directory.Exists(path))
            {
                _watcher.InternalBufferSize = 1024 * 64; // 64 megs
                _watcher.Path = path;
                _watcher.NotifyFilter = 
                    NotifyFilters.FileName | 
                    NotifyFilters.LastWrite | 
                    NotifyFilters.LastAccess |
                    NotifyFilters.DirectoryName | 
                    NotifyFilters.Size | 
                    NotifyFilters.Attributes | 
                    NotifyFilters.CreationTime | 
                    NotifyFilters.Security;
                _watcher.Filter = filter;
                _watcher.Changed += Watcher_Changed;
                _watcher.Deleted += Watcher_Deleted;
                _watcher.Created += Watcher_Created;
                _watcher.Error += Watcher_Error;
                _watcher.IncludeSubdirectories = true;
                _watcher.EnableRaisingEvents = false;
            }
            else
            {
                throw new DirectoryNotFoundException($"Could not find path '{path}'.");
            }
        }

        /// <summary>
        /// Starts the watcher
        /// </summary>
        public void Start()
        {
            _watcher.EnableRaisingEvents = true;
        }

        /// <summary>
        /// Stops the watcher
        /// </summary>
        public void Stop()
        {
            _watcher.EnableRaisingEvents = false;
        }

        /// <summary>
        /// Returns the watched path
        /// </summary>
        /// <returns></returns>
        public string GetPath()
        {
            return _path;
        }

        #region Watcher Events

        /// <summary>
        /// Handles the Deleted event on FileSystemWatcher instances
        /// </summary>
        /// <param name="sender">The originating FileSystemWatcher</param>
        /// <param name="e">The details of the event</param>
        protected virtual void Watcher_Deleted(object sender, FileSystemEventArgs e)
        {
            if (!PathHelper.IsWithinPaths(e.FullPath, _exclusions))
            {
                try
                {
                    var lastOccurred = File.GetLastWriteTime(e.FullPath);
                    if (lastOccurred.Subtract(_lastOccurance).TotalMilliseconds >= MinOccuranceDifferenceValue ||
                        _lastTopic != e.FullPath ||
                        _lastAlteration != e.ChangeType)
                    {
                        Inform($"'{e.FullPath}' was deleted.");
                        _lastOccurance = lastOccurred;
                        _lastTopic = e.FullPath;
                        _lastAlteration = e.ChangeType;

                        Events.Enqueue(new NonSpammingFileSystemWatcherEvent(this, FileSystemWatcherEventNatures.Deletion, e));
                    }
                }
                catch (Exception ex)
                {
                    Error(ex.Message, ex);
                }
            }
        }

        /// <summary>
        /// Handles the Changed event on FileSystemWatcher instances
        /// </summary>
        /// <param name="sender">The originating FileSystemWatcher</param>
        /// <param name="e">The details of the event</param>
        protected virtual void Watcher_Changed(object sender, FileSystemEventArgs e)
        {
            if (!PathHelper.IsWithinPaths(e.FullPath, _exclusions))
            {
                try
                {
                    var lastOccurred = File.GetLastWriteTime(e.FullPath);
                    if (lastOccurred.Subtract(_lastOccurance).TotalMilliseconds >= MinOccuranceDifferenceValue ||
                        _lastTopic != e.FullPath ||
                        _lastAlteration != e.ChangeType)
                    {
                        Inform($"'{e.FullPath}' was {e.ChangeType.ToString().ToLower()}.");
                        _lastOccurance = lastOccurred;
                        _lastTopic = e.FullPath;
                        _lastAlteration = e.ChangeType;

                        Events.Enqueue(new NonSpammingFileSystemWatcherEvent(this, FileSystemWatcherEventNatures.Alteration, e));
                    }
                }
                catch (Exception ex)
                {
                    Error(ex.Message, ex);
                }
            }
        }

        /// <summary>
        /// Handles the Created event on FileSystemWatcher instances
        /// </summary>
        /// <param name="sender">The originating FileSystemWatcher</param>
        /// <param name="e">The details of the event</param>
        protected virtual void Watcher_Created(object sender, FileSystemEventArgs e)
        {
            if (!PathHelper.IsWithinPaths(e.FullPath, _exclusions))
            {
                try
                {
                    var lastOccurred = File.GetLastWriteTime(e.FullPath);
                    if (lastOccurred.Subtract(_lastOccurance).TotalMilliseconds >= MinOccuranceDifferenceValue ||
                        _lastTopic != e.FullPath ||
                        _lastAlteration != e.ChangeType)
                    {
                        Inform($"'{e.FullPath}' was created.");
                        _lastOccurance = lastOccurred;
                        _lastTopic = e.FullPath;
                        _lastAlteration = e.ChangeType;

                        Events.Enqueue(new NonSpammingFileSystemWatcherEvent(this, FileSystemWatcherEventNatures.Creation, e));
                    }
                }
                catch (Exception ex)
                {
                    Error(ex.Message, ex);
                }
            }
        }

        /// <summary>
        /// Handles the Error event on FileSystemWatcher instances
        /// </summary>
        /// <param name="sender">The originating FileSystemWatcher</param>
        /// <param name="e">The details of the event</param>
        private void Watcher_Error(object sender, ErrorEventArgs e)
        {
            Error("An error was encountered.", e.GetException());
            Events.Enqueue(new NonSpammingFileSystemWatcherEvent(this, e.GetException()));
        }

        #endregion
    }
}
