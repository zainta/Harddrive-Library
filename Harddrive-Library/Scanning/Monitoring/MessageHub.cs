// Copyright (c) Zain Al-Ahmary.  All rights reserved.
// Licensed under the MIT License, (the "License"); you may not use this file except in compliance with the License. 
// You may obtain a copy of the License at https://mit-license.org/

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace HDDL.Scanning.Monitoring
{
    /// <summary>
    /// Handles reception of messages from all monitoring-related sources
    /// </summary>
    class MessageHub
    {
        internal delegate void OperationRecycleRequired(ReporterBase target);
        /// <summary>
        /// Occurs when something needs to be recycled
        /// </summary>
        public event OperationRecycleRequired RecycleRequired;

        internal delegate void OperationDiskActivityDetected(NonSpammingFileSystemWatcher target, FileSystemWatcherEventNatures nature, FileSystemEventArgs e);
        /// <summary>
        /// Occurs when a non-spamming file system watcher reporters activity
        /// </summary>
        public event OperationDiskActivityDetected DiskEventOccurred;

        private List<ReporterBase> _charges;
        /// <summary>
        /// The reporter base instances the message hub is monitoring
        /// </summary>
        public IReadOnlyCollection<ReporterBase> Charges
        {
            get
            {
                return _charges.AsReadOnly();
            }
        }

        /// <summary>
        /// The reporter base's backlog of events
        /// </summary>
        public ConcurrentQueue<EventMessageBase> Events { get; private set; }

        /// <summary>
        /// The thread that handles polling
        /// </summary>
        private Task _worker;

        /// <summary>
        /// If the message hub is active
        /// </summary>
        public bool Active { get; private set; }

        /// <summary>
        /// Creates a message hub
        /// </summary>
        public MessageHub()
        {
            Events = new ConcurrentQueue<EventMessageBase>();
            _charges = new List<ReporterBase>();
        }

        /// <summary>
        /// Starts the message hub monitoring its charges
        /// </summary>
        public void Start()
        {
            Active = true;
            _worker = Task.Run(() => WorkerMethod());
        }

        /// <summary>
        /// Stops the message hub monitoring
        /// </summary>
        public void Stop()
        {
            Active = false;
            _worker = null;
        }

        /// <summary>
        /// Adds a reporter base instance for monitoring
        /// </summary>
        /// <param name="charge">The reporter base to monitor</param>
        public void Add(ReporterBase charge)
        {
            _charges.Add(charge);
        }

        /// <summary>
        /// Removes a reporter base instance from monitoring
        /// </summary>
        /// <param name="charge"></param>
        public void Remove(ReporterBase charge)
        {
            _charges.Remove(charge);
        }

        /// <summary>
        /// The method used as the task body for the worker
        /// </summary>
        private void WorkerMethod()
        {
            while (Active)
            {
                ReporterBase focus = (from c in _charges where !c.Events.IsEmpty select c).FirstOrDefault();

                if (focus == null)
                {
                    Task.Delay(100).Wait();
                }
                else
                {
                    EventMessageBase evnt = null;
                    if (focus.Events.TryDequeue(out evnt))
                    {
                        // depending on the type of event received,
                        // look at its details and either queue it for reporting or act on its information

                        if (evnt is SymphonyEvent)
                        {
                            var iseEvnt = (SymphonyEvent)evnt;
                        }
                        else if (evnt is NonSpammingFileSystemWatcherEvent)
                        {
                            var nsfswEvnt = (NonSpammingFileSystemWatcherEvent)evnt;
                            if (nsfswEvnt.EventType == NonSpammingFileSystemWatcherEventTypes.DiskEvent)
                            {
                                Task.Run(() => {
                                    DiskEventOccurred?.Invoke((NonSpammingFileSystemWatcher)nsfswEvnt.Source, nsfswEvnt.Nature, nsfswEvnt.DiskEventDetails);
                                });

                                // don't report it
                                evnt = null;
                            }
                            else if (nsfswEvnt.EventType == NonSpammingFileSystemWatcherEventTypes.InternalError ||
                                nsfswEvnt.EventType == NonSpammingFileSystemWatcherEventTypes.InternalErrorWithException)
                            {
                                // still report it, but the watcher needs to be recycled
                                Task.Run(() => {
                                    RecycleRequired?.Invoke(nsfswEvnt.Source);
                                });
                            }
                        }
                        else if (evnt is MessageBundle)
                        {
                            var mbEvnt = (MessageBundle)evnt;
                            if (mbEvnt.Type == MessageTypes.Error)
                            {
                                Task.Run(() => {
                                    RecycleRequired?.Invoke(mbEvnt.Source);
                                });

                                // don't report it
                                evnt = null;
                            }
                        }

                        if (evnt != null)
                        {
                            Events.Enqueue(evnt);
                        }
                    }
                }
            }
        }
    }
}
