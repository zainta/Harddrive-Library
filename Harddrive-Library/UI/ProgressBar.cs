// Copyright (c) Zain Al-Ahmary.  All rights reserved.
// Licensed under the MIT License, (the "License"); you may not use this file except in compliance with the License. 
// You may obtain a copy of the License at https://mit-license.org/

using System;

namespace HDDL.UI
{
    /// <summary>
    /// A bare bones console progress bar
    /// </summary>
    public class ProgressBar
    {
        public const char Filled = '█';
        public const char Unfilled = '░';

        /// <summary>
        /// The X coordinate
        /// </summary>
        public int X { get; set; }

        /// <summary>
        /// The Y coordinate
        /// </summary>
        public int Y { get; set; }

        /// <summary>
        /// The display width in columns
        /// </summary>
        public int Width { get; set; }

        private int _value, _minimum, _maximum;

        /// <summary>
        /// The current value
        /// </summary>
        public int Value
        {
            get
            {
                return _value;
            }
            set
            {
                if (value > Maximum)
                {
                    _value = Maximum;
                }
                else if (value < Minimum)
                {
                    _value = Minimum;
                }
                else
                {
                    _value = value;
                }
            }
        }

        /// <summary>
        /// The minimum value
        /// </summary>
        public int Minimum
        {
            get
            {
                return _minimum;
            }
            set
            {
                if (value >= Maximum)
                {
                    _minimum = Maximum - 1;
                }
                else if (value > Value)
                {
                    _minimum = value;
                    Value = _minimum;
                }
                else
                {
                    _minimum = value;
                }
            }
        }

        /// <summary>
        /// The maximum value
        /// </summary>
        public int Maximum
        {
            get
            {
                return _maximum;
            }
            set
            {
                if (value <= Minimum)
                {
                    _maximum = Minimum + 1;
                }
                else if (value < Value)
                {
                    _maximum = value;
                    Value = _maximum;
                }
                else
                {
                    _maximum = value;
                }
            }
        }

        
        /// <summary>
        /// Creates a Progress Bar
        /// </summary>
        /// <param name="x">The X coordinate to display at</param>
        /// <param name="y">The Y coordinate to display at</param>
        /// <param name="width">The number of columns wide the control should be</param>
        /// <param name="initial">The initial value</param>
        /// <param name="min">The minimum value</param>
        /// <param name="max">The maximum value</param>
        public ProgressBar(int x, int y, int width, int initial, int min, int max)
        {
            X = x;
            Y = y;
            Width = width;

            _value = initial;
            _minimum = min;
            _maximum = max;

            // force validation
            Value = Value;
            Minimum = Minimum;
            Maximum = Maximum;
        }

        /// <summary>
        /// Displays the progress bar
        /// </summary>
        public void Display()
        {
            var pointsPerBlock = Maximum / Width;
            var filled = pointsPerBlock > 0 ? Value / pointsPerBlock : Width;
            var empty = Width - filled;

            var origX = Console.CursorLeft;
            var origY = Console.CursorTop;

            if (X == -1)
            {
                X = Console.CursorLeft;
            }
            else
            {
                Console.CursorLeft = X;
            }
            if (Y == -1)
            {
                Y = Console.CursorTop;
            }
            else
            {
                Console.CursorTop = Y;
            }

            if (filled > 0)
            {
                Console.Write(new String(Filled, filled));
            }
            if (empty > 0)
            {
                Console.Write(new String(Unfilled, empty));
            }

            Console.CursorLeft = origX;
            Console.CursorTop = origY;
        }
    }
}
