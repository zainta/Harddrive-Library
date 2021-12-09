// Copyright (c) Zain Al-Ahmary.  All rights reserved.
// Licensed under the MIT License, (the "License"); you may not use this file except in compliance with the License. 
// You may obtain a copy of the License at https://mit-license.org/

namespace HDDL.Language.HDSL
{
    /// <summary>
    /// Represents the different ways the HDSLQueryOutcome class can display the attributes column
    /// </summary>
    enum AttributeDisplayMethods
    {
        /// <summary>
        /// Causes the HDSLQueryOutcome class to output a single character representing each attribute as a single text block.
        /// Only displays Readonly, System, Archive, Hidden, Directory, and Normal.
        /// </summary>
        Simple,
        /// <summary>
        /// Causes the HDSLQueryOutcome class to output a single character representing each attribute as a single text block.
        /// Displays characters for all attributes.
        /// </summary>
        Extended,
        /// <summary>
        /// Causes the HDSLQueryOutcome class to output a three letter abbreviation representing each attribute as a comma seperated list.
        /// Only displays Readonly, System, Archive, Hidden, Directory, and Normal.
        /// </summary>
        ThreeCharacterSimple,
        /// <summary>
        /// Causes the HDSLQueryOutcome class to output a three letter abbreviation representing each attribute as a comma seperated list.
        /// Displays characters for all attributes.
        /// </summary>
        ThreeCharacterExtended
    }
}
