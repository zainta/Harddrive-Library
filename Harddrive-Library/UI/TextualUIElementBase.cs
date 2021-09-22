// Copyright (c) Zain Al-Ahmary.  All rights reserved.
// Licensed under the MIT License, (the "License"); you may not use this file except in compliance with the License. 
// You may obtain a copy of the License at https://mit-license.org/

using System;

namespace HDDL.UI
{
    public abstract class TextualUIElementBase
    {
        /// <summary>
        /// The X coordinate
        /// </summary>
        public int X { get; set; }

        /// <summary>
        /// The Y coordinate
        /// </summary>
        public int Y { get; set; }

        protected TextualUIElementBase(int x, int y)
        {
            X = x;
            Y = y;
        }

        /// <summary>
        /// Displays the progress bar
        /// </summary>
        public virtual void Display()
        {
            throw new NotImplementedException();
        }
    }
}
