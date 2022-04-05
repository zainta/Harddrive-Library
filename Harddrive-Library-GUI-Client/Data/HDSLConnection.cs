// Copyright (c) Zain Al-Ahmary.  All rights reserved.
// Licensed under the MIT License, (the "License"); you may not use this file except in compliance with the License. 
// You may obtain a copy of the License at https://mit-license.org/

using HDDL.Web;
using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;

namespace HDDLC.Data
{
    /// <summary>
    /// Represents a connection to an HDSL service
    /// </summary>
    public class HDSLConnection : DependencyObject, INotifyPropertyChanged
    {
        public delegate void WasDeletedDelegate(HDSLConnection target);
        /// <summary>
        /// Occurs when the HDSLConnection is deleted via the UI
        /// </summary>
        public event WasDeletedDelegate WasDeleted;

        /// <summary>
        /// Occurs when a property changes
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        #region ConnectionAddress

        /// <summary>
        /// ConnectionAddress Dependency Property
        /// </summary>
        public static readonly DependencyProperty ConnectionAddressProperty =
            DependencyProperty.Register("ConnectionAddress", typeof(string), typeof(HDSLConnection),
                new FrameworkPropertyMetadata(@"http://127.0.0.1:5000/",
                    new PropertyChangedCallback(OnConnectionAddressChanged)));

        /// <summary>
        /// The target HDSL service's address 
        /// </summary>
        public string ConnectionAddress
        {
            get { return (string)GetValue(ConnectionAddressProperty); }
            set { SetValue(ConnectionAddressProperty, value); }
        }

        /// <summary>
        /// Handles changes to the ConnectionAddress property.
        /// </summary>
        private static void OnConnectionAddressChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            HDSLConnection target = (HDSLConnection)d;
            string oldConnectionAddress = (string)e.OldValue;
            string newConnectionAddress = target.ConnectionAddress;
            target.OnConnectionAddressChanged(oldConnectionAddress, newConnectionAddress);
        }

        /// <summary>
        /// Provides derived classes an opportunity to handle changes to the ConnectionAddress property.
        /// </summary>
        protected virtual void OnConnectionAddressChanged(string oldConnectionAddress, string newConnectionAddress)
        {
            if (oldConnectionAddress != newConnectionAddress)
            {
                OnPropertyChanged("ConnectionAddress");
            }
        }

        #endregion

        #region IsValid

        /// <summary>
        /// IsValid Dependency Property
        /// </summary>
        public static readonly DependencyProperty IsValidProperty =
            DependencyProperty.Register("IsValid", typeof(bool?), typeof(HDSLConnection),
                new FrameworkPropertyMetadata(null,
                    new PropertyChangedCallback(OnIsValidChanged)));

        /// <summary>
        /// Whether or not the connection address is valid
        /// </summary>
        public bool? IsValid
        {
            get { return (bool?)GetValue(IsValidProperty); }
            set { SetValue(IsValidProperty, value); }
        }

        /// <summary>
        /// Handles changes to the IsValid property.
        /// </summary>
        private static void OnIsValidChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            HDSLConnection target = (HDSLConnection)d;
            bool? oldIsValid = (bool?)e.OldValue;
            bool? newIsValid = target.IsValid;
            target.OnIsValidChanged(oldIsValid, newIsValid);
        }

        /// <summary>
        /// Provides derived classes an opportunity to handle changes to the IsValid property.
        /// </summary>
        protected virtual void OnIsValidChanged(bool? oldIsValid, bool? newIsValid)
        {
        }

        #endregion

        #region NeedsValidation

        /// <summary>
        /// NeedsValidation Dependency Property
        /// </summary>
        public static readonly DependencyProperty NeedsValidationProperty =
            DependencyProperty.Register("NeedsValidation", typeof(bool), typeof(HDSLConnection),
                new FrameworkPropertyMetadata((bool)false,
                    new PropertyChangedCallback(OnNeedsValidationChanged)));

        /// <summary>
        /// Whether or not the connection requires validation
        /// </summary>
        public bool NeedsValidation
        {
            get { return (bool)GetValue(NeedsValidationProperty); }
            set { SetValue(NeedsValidationProperty, value); }
        }

        /// <summary>
        /// Handles changes to the NeedsValidation property.
        /// </summary>
        private static void OnNeedsValidationChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            HDSLConnection target = (HDSLConnection)d;
            bool oldNeedsValidation = (bool)e.OldValue;
            bool newNeedsValidation = target.NeedsValidation;
            target.OnNeedsValidationChanged(oldNeedsValidation, newNeedsValidation);
        }

        /// <summary>
        /// Provides derived classes an opportunity to handle changes to the NeedsValidation property.
        /// </summary>
        protected virtual void OnNeedsValidationChanged(bool oldNeedsValidation, bool newNeedsValidation)
        {
            
        }

        #endregion

        /// <summary>
        /// Create a default connection
        /// </summary>
        public HDSLConnection()
        {

        }

        /// <summary>
        /// Returns an equivalent HDSL service connection
        /// </summary>
        /// <returns></returns>
        public HDSLWebClient AsConnection()
        {
            var address = ConnectionAddress.EndsWith('/') ? ConnectionAddress.Substring(0, ConnectionAddress.Length - 1) : ConnectionAddress;
            return new HDSLWebClient(address);
        }

        /// <summary>
        /// Raises an event to signal that the HDSLConnection should be deleted
        /// </summary>
        public void Delete()
        {
            WasDeleted?.Invoke(this);
        }

        /// <summary>
        /// Validates the current address
        /// </summary>
        /// <returns></returns>
        public void PerformValidation()
        {
            //if (!NeedsValidation) return;
            IsValid = null;

            bool outcome = false;
            NeedsValidation = false;
            string address = ConnectionAddress;

            var t = Task.Run(() => 
                {
                    try
                    {
                        if (!string.IsNullOrWhiteSpace(address))
                        {
                            if (HDSLWebClient.Hi(address))
                            {
                                outcome = true;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                    }
                }).ContinueWith((t) =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        if (t.IsCompleted && !t.IsFaulted)
                        {
                            IsValid = outcome;
                        }
                        else
                        {
                            IsValid = false;
                        }
                    });
                });
        }

        /// <summary>
        /// Safely calls the PropertyChanged event
        /// </summary>
        /// <param name="propertyName"></param>
        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
