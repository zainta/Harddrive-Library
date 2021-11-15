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
        public ReporterBase(MessagingModes messenging = MessagingModes.Error)
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
                    MessageRelayed?.Invoke(this, new MessageBundle(this, message, ex));
                }
            }
            else
            {
                if (_messenging.HasFlag(MessagingModes.Error) &&
                    requiredModes.HasFlag(MessagingModes.Error))
                {
                    MessageRelayed?.Invoke(this, new MessageBundle(this, message, MessageTypes.Error));
                }
                else if (_messenging.HasFlag(MessagingModes.Warning) &&
                    requiredModes.HasFlag(MessagingModes.Warning))
                {
                    MessageRelayed?.Invoke(this, new MessageBundle(this, message, MessageTypes.Information));
                }
                else if (_messenging.HasFlag(MessagingModes.Information) &&
                    requiredModes.HasFlag(MessagingModes.Information))
                {
                    MessageRelayed?.Invoke(this, new MessageBundle(this, message, MessageTypes.Information));
                }
                else if ((_messenging.HasFlag(MessagingModes.Information) || _messenging.HasFlag(MessagingModes.VerboseInformation)) &&
                    (requiredModes.HasFlag(MessagingModes.Information) || requiredModes.HasFlag(MessagingModes.VerboseInformation)))
                {
                    MessageRelayed?.Invoke(this, new MessageBundle(this, message, MessageTypes.VerboseInformation));
                }
            }
        }

        /// <summary>
        /// Passes a relayed message onward up the reporting chain
        /// </summary>
        /// <param name="bundle">The bundle to pass</param>
        protected void Relay(MessageBundle bundle)
        {
            switch (bundle.Type)
            {
                case MessageTypes.Error:
                    if (_messenging.HasFlag(MessagingModes.Error))
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
                case MessageTypes.VerboseInformation:
                    if (_messenging.HasFlag(MessagingModes.VerboseInformation))
                    {
                        MessageRelayed?.Invoke(this, bundle);
                    }
                    break;
                case MessageTypes.Warning:
                    if (_messenging.HasFlag(MessagingModes.Warning))
                    {
                        MessageRelayed?.Invoke(this, bundle);
                    }
                    break;
            }
        }
    }
}
