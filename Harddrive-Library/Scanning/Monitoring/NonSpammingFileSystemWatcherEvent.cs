// Copyright (c) Zain Al-Ahmary.  All rights reserved.
// Licensed under the MIT License, (the "License"); you may not use this file except in compliance with the License. 
// You may obtain a copy of the License at https://mit-license.org/

using System;
using System.IO;

namespace HDDL.Scanning.Monitoring
{
    /// <summary>
    /// Represents something that happened in/to a non-spamming file sysem watcher
    /// </summary>
    class NonSpammingFileSystemWatcherEvent : EventMessageBase
    {
        /// <summary>
        /// The type of event represented
        /// </summary>
        public NonSpammingFileSystemWatcherEventTypes EventType { get; internal set; }

        /// <summary>
        /// The type of disk event being reported
        /// </summary>
        public FileSystemWatcherEventNatures Nature { get; internal set; }
        
        /// <summary>
        /// The details about the disk event
        /// </summary>
        public FileSystemEventArgs DiskEventDetails { get; internal set; }

        /// <summary>
        /// Any exception that occurred to cause a recycle's requirement
        /// </summary>
        public Exception Exception { get; internal set; }

        /// <summary>
        /// Creates an event for a disk event
        /// </summary>
        /// <param name="source"></param>
        /// <param name="nature"></param>
        /// <param name="e"></param>
        public NonSpammingFileSystemWatcherEvent(NonSpammingFileSystemWatcher source, FileSystemWatcherEventNatures nature, FileSystemEventArgs e) : base(source)
        {
            EventType = NonSpammingFileSystemWatcherEventTypes.DiskEvent;
            Nature = nature;
            DiskEventDetails = e;
        }

        /// <summary>
        /// Creates a recycle event for an exception
        /// </summary>
        /// <param name="source"></param>
        /// <param name="ex"></param>
        public NonSpammingFileSystemWatcherEvent(NonSpammingFileSystemWatcher source, Exception ex) : base(source)
        {
            EventType = NonSpammingFileSystemWatcherEventTypes.InternalErrorWithException;
            Exception = ex;
        }

        /// <summary>
        /// Creates an exceptionless event for a recycle
        /// </summary>
        /// <param name="source"></param>
        public NonSpammingFileSystemWatcherEvent(NonSpammingFileSystemWatcher source) : base(source)
        {
            EventType = NonSpammingFileSystemWatcherEventTypes.InternalError;
        }
    }
}
