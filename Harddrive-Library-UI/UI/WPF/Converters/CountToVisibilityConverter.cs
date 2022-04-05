// Copyright (c) Zain Al-Ahmary.  All rights reserved.
// Licensed under the MIT License, (the "License"); you may not use this file except in compliance with the License. 
// You may obtain a copy of the License at https://mit-license.org/

using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace HDDL.UI.WPF.Converters
{
    [ValueConversion(typeof(bool), typeof(Visibility), ParameterType = typeof(bool))]
    public class CountToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var val = (int)value;
            bool? invert = parameter == null ? false : bool.Parse((string)parameter);
            var positive = invert == true ? Visibility.Visible : Visibility.Collapsed;

            if (val > 0)
            {
                
                return positive;
            }
            else
            {
                if (positive == Visibility.Visible)
                {
                    return Visibility.Collapsed;
                }
                else
                {
                    return Visibility.Visible;
                }
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
