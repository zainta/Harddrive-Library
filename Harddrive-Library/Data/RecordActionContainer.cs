using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        /// Creates an
        /// </summary>
        public RecordActionContainer()
        {
            Inserts = new ConcurrentBag<T>();
            Updates = new ConcurrentBag<T>();
            Deletions = new ConcurrentBag<T>();
        }
    }
}
