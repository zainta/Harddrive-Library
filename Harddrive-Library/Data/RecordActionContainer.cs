// Copyright (c) Zain Al-Ahmary.  All rights reserved.
// Licensed under the MIT License, (the "License"); you may not use this file except in compliance with the License. 
// You may obtain a copy of the License at https://mit-license.org/

using System.Collections.Concurrent;

namespace HDDL.Data
{
    /// <summary>
    /// A generic store for insert and update operations
    /// </summary>
    class RecordActionContainer<T>
    {
        /// <summary>
        /// Represents the work of a single bulk insert transaction
        /// </summary>
        public ConcurrentBag<T> Inserts { get; private set; }

        /// <summary>
        /// Represents the work of a single bulk update transaction
        /// </summary>
        public ConcurrentBag<T> Updates { get; private set; }

        /// <summary>
        /// Represents the work of a single bulk delete transaction
        /// </summary>
        public ConcurrentBag<T> Deletions { get; private set; }

        /// <summary>
        /// Whether or not any actions are required for this container
        /// </summary>
        public bool HasWork
        {
            get
            {
                return Inserts.Count > 0 || Updates.Count > 0 || Deletions.Count > 0;
            }
        }

        /// <summary>
        /// Creates a RecordActionContainer to track items of type <typeparamref name="T"/>
        /// </summary>
        public RecordActionContainer()
        {
            Inserts = new ConcurrentBag<T>();
            Updates = new ConcurrentBag<T>();
            Deletions = new ConcurrentBag<T>();
        }

        public override string ToString()
        {
            return $"Contains {Inserts.Count} inserts, {Updates.Count} updates, and {Deletions.Count} deletions";
        }
    }
}
