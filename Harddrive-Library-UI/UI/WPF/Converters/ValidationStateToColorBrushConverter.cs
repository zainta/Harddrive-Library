// Copyright (c) Zain Al-Ahmary.  All rights reserved.
// Licensed under the MIT License, (the "License"); you may not use this file except in compliance with the License. 
// You may obtain a copy of the License at https://mit-license.org/

using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace HDDL.UI.WPF.Converters
{
    [ValueConversion(typeof(bool), typeof(bool))]
    public class ValidationStateToColorBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool? validationState = value as bool?;
            SolidColorBrush brush = null;
            if (validationState.HasValue)
            {
                if (validationState.Value)
                {
                    brush = new SolidColorBrush(Colors.LightGreen);
                }
                else
                {
                    brush = new SolidColorBrush(Colors.Red);
                }
            }

            return brush;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            SolidColorBrush brush = value as SolidColorBrush;
            bool? result = null;
            if (brush != null)
            {
                if (brush.Color == Colors.LightGreen)
                {
                    result = true;
                }
                else if (brush.Color == Colors.Red)
                {
                    result = false;
                }
            }

            return result;
        }
    }
}
