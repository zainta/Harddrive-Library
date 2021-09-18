using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HDDL.Scanning
{
    /// <summary>
    /// Defines the types of scan events that can occur during a scan operation
    /// </summary>
    public enum ScanEventType
    {
        /// <summary>
        /// Occurs when an error occurs during an add
        /// </summary>
        AddRequired,
        /// <summary>
        /// Occurs an add operation is queued
        /// </summary>
        UpdateRequired,
        /// <summary>
        /// Occurs an delete operation is queued
        /// </summary>
        KeyNotDeleted,
        /// <summary>
        /// Occurs when an error occurs during the record operation
        /// </summary>
        UnknownError,
        /// <summary>
        /// Occurs when an error related specifically to the database occurs
        /// </summary>
        DatabaseError
    }
}
