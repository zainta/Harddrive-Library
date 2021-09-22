// Copyright (c) Zain Al-Ahmary.  All rights reserved.
// Licensed under the MIT License, (the "License"); you may not use this file except in compliance with the License. 
// You may obtain a copy of the License at https://mit-license.org/

using System;

namespace HDDL.UI
{
    public class Spinner : TextualUIElementBase
    {
        private static char[] Stages = new char[] { '\\', '|', '/', '-' };

        /// <summary>
        /// Used to display the spinner
        /// </summary>
        private int _stage;

        /// <summary>
        /// Whether or not the spinner is active
        /// </summary>
        public bool Active { get; set; }

        /// <summary>
        /// Creates the spinner at the given location
        /// </summary>
        /// <param name="x">The initial X coordinate</param>
        /// <param name="y">The initial Y coordinate</param>
        /// <param name="started">Whether or not to start immediately</param>
        public Spinner(int x, int y, bool started = false) : base(x, y)
        {
            _stage = 0;
            Active = started;
        }

        /// <summary>
        /// Displays the Spinner's current frame
        /// </summary>
        public override void Display()
        {
            int origX = Console.CursorLeft, origY = Console.CursorTop;

            Console.CursorLeft = X;
            Console.CursorTop = Y;

            Console.Write(Stages[_stage]);

            Console.CursorLeft = origX;
            Console.CursorTop = origY;

            _stage++;
            if (_stage >= Stages.Length)
            {
                _stage = 0;
            }
        }
    }
}
