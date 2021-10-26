// Copyright (c) Zain Al-Ahmary.  All rights reserved.
// Licensed under the MIT License, (the "License"); you may not use this file except in compliance with the License. 
// You may obtain a copy of the License at https://mit-license.org/

using System;

namespace HDDL.Scanning.Monitoring
{
    /// <summary>
    /// Base class for all reporting event derivations
    /// </summary>
    public abstract class ReporterBase
    {
        public delegate void RelayMessageDelegate(ReporterBase origin, MessageBundle message);

        /// <summary>
        /// Occurs when a message is sent from the ReporterBase
        /// </summary>
        public event RelayMessageDelegate MessageRelayed;

        /// <summary>
        /// The kinds and styles of messages that will be relayed
        /// </summary>
        private MessagingModes _messenging;

        /// <summary>
        /// Creates a ReporterBase instance
        /// </summary>
        /// <param name="messenging">The kinds and styles of messages that will be relayed</param>
        public ReporterBase(MessagingModes messenging = MessagingModes.Errors)
        {
            _messenging = messenging;
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
        /// <param name="message">The error message</param>
        protected void Inform(string message)
        {
            if (_messenging.HasFlag(MessagingModes.Information))
            {
                MessageRelayed?.Invoke(this, new MessageBundle(this, message));
            }
        }

        /// <summary>
        /// Reports an exception along with a message
        /// </summary>
        /// <param name="message">The message</param>
        /// <param name="ex">The exception</param>
        protected void Error(string message, Exception ex)
        {
            if (_messenging.HasFlag(MessagingModes.Errors))
            {
                MessageRelayed?.Invoke(this, new MessageBundle(this, message, ex));
            }
        }

        /// <summary>
        /// Reports an issue that was successfully recovered from
        /// </summary>
        /// <param name="message">A description of the issue</param>
        protected void Warn(string message)
        {
            if (_messenging.HasFlag(MessagingModes.Errors))
            {
                MessageRelayed?.Invoke(this, new MessageBundle(this, message, MessageTypes.Warning));
            }
        }

        /// <summary>
        /// Mmoves a MessageBundle up the reporting chain
        /// </summary>
        /// <param name="bundle">The bundle to push</param>
        protected void Forward(MessageBundle bundle)
        {
            switch (bundle.Type)
            {
                case MessageTypes.Error:
                    if (_messenging.HasFlag(MessagingModes.Errors))
                    {
                        MessageRelayed?.Invoke(this, bundle);
                    }
                    break;
                case MessageTypes.Information:
                    if (_messenging.HasFlag(MessagingModes.Information))
                    {
                        MessageRelayed?.Invoke(this, bundle);
                    }
                    break;
                case MessageTypes.Warning:
                    if (_messenging.HasFlag(MessagingModes.Errors))
                    {
                        MessageRelayed?.Invoke(this, bundle);
                    }
                    break;
            }
        }
    }
}
