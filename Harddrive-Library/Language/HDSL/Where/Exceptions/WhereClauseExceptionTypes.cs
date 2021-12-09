// Copyright (c) Zain Al-Ahmary.  All rights reserved.
// Licensed under the MIT License, (the "License"); you may not use this file except in compliance with the License. 
// You may obtain a copy of the License at https://mit-license.org/

namespace HDDL.Language.HDSL.Where.Exceptions
{
    /// <summary>
    /// Describes the nature of the exception
    /// </summary>
    public enum WhereClauseExceptionTypes
    {
        /// <summary>
        /// A term was found where it shouldn't be
        /// </summary>
        InvalidTermPosition,
        /// <summary>
        /// The Has and Has Not operators are only valid with attributes, and only in conjunction with the File System (DiskItem)
        /// </summary>
        InvalidUseofHasOrHasNot,
        /// <summary>
        /// Type comparisons in a where clause must be made between two terms of the same type
        /// </summary>
        TypeMismatch,
        /// <summary>
        /// The where operator is of an unknown variety
        /// </summary>
        UnknownOperatorType,
        /// <summary>
        /// The column's type is unknown
        /// </summary>
        InvalidColumnTypeReferenced,
        /// <summary>
        /// The column does not exist on the current context type
        /// </summary>
        UnknownColumnReferenced,
        /// <summary>
        /// The Like operator (~) can only be used with two strings
        /// </summary>
        InvalidUseOfLike,
        /// <summary>
        /// The operator cannot act on the given operands' type
        /// </summary>
        OperatorTypeMismatch
    }
}
