// Copyright (c) Zain Al-Ahmary.  All rights reserved.
// Licensed under the MIT License, (the "License"); you may not use this file except in compliance with the License. 
// You may obtain a copy of the License at https://mit-license.org/

namespace HDDL.Language.HDSL
{
    /// <summary>
    /// The types of find starting points
    /// </summary>
    enum FindQueryDepths
    {
        /// <summary>
        /// Immediately inside of the target path
        /// </summary>
        In,
        /// <summary>
        /// Immediately inside of the target path and under all subdirectories, to an infinite depth
        /// </summary>
        Within,
        /// <summary>
        /// Immediately inside of all subdirectories, to an infinite depth
        /// </summary>
        Under
    }
}
