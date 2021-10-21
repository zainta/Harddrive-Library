// Copyright (c) Zain Al-Ahmary.  All rights reserved.
// Licensed under the MIT License, (the "License"); you may not use this file except in compliance with the License. 
// You may obtain a copy of the License at https://mit-license.org/

using System;
using System.IO;

namespace HDDL.Scanning.Monitoring
{
    /// <summary>
    /// A File System Watcher wrapper class that does not spam duplicate events
    /// </summary>
    class NonSpammingFileSystemWatcher : ReporterBase
    {
        public delegate void ReportingOccurrence(NonSpammingFileSystemWatcher origin, FileSystemWatcherEventNatures nature, FileSystemEventArgs e);

        /// <summary>
        /// Occurs when something happens in the monitored area
        /// </summary>
        public event ReportingOccurrence ReportDiskEvent;

        private const long MinOccuranceDifferenceValue = 500; // milliseconds
        private FileSystemWatcher _watcher;
        private DateTime _lastOccurance;
        private string _lastTopic;
        private string _path;

        /// <summary>
        /// Creates a non-spamming file system watcher to monitor the given location
        /// </summary>
        /// <param name="path">The path to monitor</param>
        /// <param name="messenging">The kinds and styles of messages that will be relayed</param>
        public NonSpammingFileSystemWatcher(string path, MessagingModes messenging) : base(messenging)
        {
            _path = path;
            _watcher = new FileSystemWatcher();
            if (!string.IsNullOrWhiteSpace(path) && Directory.Exists(path))
            {
                _watcher.InternalBufferSize = 1024 * 16; // 16 megs
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
                _watcher.Filter = "*.*";
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
            var lastOccurred = File.GetLastWriteTime(e.FullPath);
            if (lastOccurred.Subtract(_lastOccurance).TotalMilliseconds > MinOccuranceDifferenceValue ||
                _lastTopic != e.FullPath)
            {
                Inform($"'{e.FullPath}' was deleted.");
                _lastOccurance = lastOccurred;
                _lastTopic = e.FullPath;

                ReportDiskEvent?.Invoke(this, FileSystemWatcherEventNatures.Deletion, e);
            }
        }

        /// <summary>
        /// Handles the Changed event on FileSystemWatcher instances
        /// </summary>
        /// <param name="sender">The originating FileSystemWatcher</param>
        /// <param name="e">The details of the event</param>
        protected virtual void Watcher_Changed(object sender, FileSystemEventArgs e)
        {
            var lastOccurred = File.GetLastWriteTime(e.FullPath);
            if (lastOccurred.Subtract(_lastOccurance).TotalMilliseconds > MinOccuranceDifferenceValue ||
                _lastTopic != e.FullPath)
            {
                Inform($"'{e.FullPath}' was altered.");
                _lastOccurance = lastOccurred;
                _lastTopic = e.FullPath;

                ReportDiskEvent?.Invoke(this, FileSystemWatcherEventNatures.Alteration, e);
            }
        }

        /// <summary>
        /// Handles the Created event on FileSystemWatcher instances
        /// </summary>
        /// <param name="sender">The originating FileSystemWatcher</param>
        /// <param name="e">The details of the event</param>
        protected virtual void Watcher_Created(object sender, FileSystemEventArgs e)
        {
            var lastOccurred = File.GetLastWriteTime(e.FullPath);
            if (lastOccurred.Subtract(_lastOccurance).TotalMilliseconds > MinOccuranceDifferenceValue ||
                _lastTopic != e.FullPath)
            {
                Inform($"'{e.FullPath}' was created.");
                _lastOccurance = lastOccurred;
                _lastTopic = e.FullPath;

                ReportDiskEvent?.Invoke(this, FileSystemWatcherEventNatures.Creation, e);
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
        }

        #endregion
    }
}
