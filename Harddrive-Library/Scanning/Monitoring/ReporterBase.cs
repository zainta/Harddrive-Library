// Copyright (c) Zain Al-Ahmary.  All rights reserved.
// Licensed under the MIT License, (the "License"); you may not use this file except in compliance with the License. 
// You may obtain a copy of the License at https://mit-license.org/

using System;
using System.Collections.Concurrent;

namespace HDDL.Scanning.Monitoring
{
    /// <summary>
    /// Base class for all reporting event derivations
    /// </summary>
    public abstract class ReporterBase
    {
        /// <summary>
        /// The reporter base's identifying guid
        /// </summary>
        internal Guid Id { get; private set; }

        private ConcurrentQueue<EventMessageBase> _events;
        private readonly ConcurrentQueue<EventMessageBase> _muteEvents = new ConcurrentQueue<EventMessageBase>();
        /// <summary>
        /// The reporter base's backlog of events
        /// </summary>
        internal ConcurrentQueue<EventMessageBase> Events 
        { 
            get
            {
                if (IsMuted)
                {
                    return _muteEvents;
                }
                else
                {
                    return _events;
                }
            }
        }

        /// <summary>
        /// The kinds and styles of messages that will be relayed
        /// </summary>
        private MessagingModes _messenging;

        /// <summary>
        /// If true, mutes the ReporterBase instance, always returning an empty event queue rather than the actual queue
        /// </summary>
        public bool IsMuted { get; set; }

        /// <summary>
        /// Creates a ReporterBase instance
        /// </summary>
        /// <param name="messenging">The kinds and styles of messages that will be relayed</param>
        public ReporterBase(MessagingModes messenging = MessagingModes.Error)
        {
            Id = Guid.NewGuid();
            _messenging = messenging;
            _events = new ConcurrentQueue<EventMessageBase>();
        }

        /// <summary>
        /// Returns the messaging mode in use
        /// </summary>
        /// <returns></returns>
        protected MessagingModes GetMessagingMode()
        {
            return _messenging;
        }

        /// <summary>
        /// Relays an informatory message
        /// </summary>
        /// <param name="message">The message</param>
        protected void Inform(string message)
        {
            Relay(message, MessagingModes.Information);
        }

        /// <summary>
        /// Relays a verbose informatory message
        /// </summary>
        /// <param name="message">The message</param>
        protected void Verbose(string message)
        {
            Relay(message, MessagingModes.VerboseInformation);
        }

        /// <summary>
        /// Reports an error message
        /// </summary>
        /// <param name="message">The message</param>
        protected void Error(string message)
        {
            Relay(message, MessagingModes.Error);
        }

        /// <summary>
        /// Reports an exception along with a message
        /// </summary>
        /// <param name="message">The message</param>
        /// <param name="ex">The exception</param>
        protected void Error(string message, Exception ex)
        {
            Relay(message, MessagingModes.Error, ex);
        }

        /// <summary>
        /// Reports an issue
        /// </summary>
        /// <param name="message">A description of the issue</param>
        protected void Warn(string message)
        {
            Relay(message, MessagingModes.Warning);
        }

        /// <summary>
        /// Reports an issue coupled with an exception
        /// </summary>
        /// <param name="message">A description of the issue</param>
        /// <param name="ex">The exception</param>
        protected void Warn(string message, Exception ex)
        {
            Relay(message, MessagingModes.Warning, ex);
        }

        /// <summary>
        /// Relays a message
        /// </summary>
        /// <param name="message">The message</param>
        /// <param name="requiredModes">The modes required for this message to be transmitted</param>
        /// <param name="ex">Any relevant exception</param>
        protected void Relay(string message, MessagingModes requiredModes, Exception ex = null)
        {
            if (ex != null)
            {
                if (_messenging.HasFlag(MessagingModes.Error) &&
                    requiredModes.HasFlag(MessagingModes.Error))
                {
                    _events.Enqueue(new MessageBundle(this, message, ex));
                }
            }
            else
            {
                if (_messenging.HasFlag(MessagingModes.Error) &&
                    requiredModes.HasFlag(MessagingModes.Error))
                {
                    _events.Enqueue(new MessageBundle(this, message, MessageTypes.Error));
                }
                else if (_messenging.HasFlag(MessagingModes.Warning) &&
                    requiredModes.HasFlag(MessagingModes.Warning))
                {
                    _events.Enqueue(new MessageBundle(this, message, MessageTypes.Information));
                }
                else if (_messenging.HasFlag(MessagingModes.Information) &&
                    requiredModes.HasFlag(MessagingModes.Information))
                {
                    _events.Enqueue(new MessageBundle(this, message, MessageTypes.Information));
                }
                else if ((_messenging.HasFlag(MessagingModes.Information) || _messenging.HasFlag(MessagingModes.VerboseInformation)) &&
                    (requiredModes.HasFlag(MessagingModes.Information) || requiredModes.HasFlag(MessagingModes.VerboseInformation)))
                {
                    _events.Enqueue(new MessageBundle(this, message, MessageTypes.VerboseInformation));
                }
            }
        }
    }
}
