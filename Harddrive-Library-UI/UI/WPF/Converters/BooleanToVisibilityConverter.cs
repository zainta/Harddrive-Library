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
    public class BooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool val = false;
            bool? invert = parameter == null ? false : bool.Parse((string)parameter);
            if (invert.HasValue && invert.Value)
            {
                val = !(bool)value;
            }
            else
            {
                val = (bool)value;
            }

            if (val)
            {
                return Visibility.Visible;
            }
            else
            {
                return Visibility.Collapsed;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            Visibility val = (Visibility)value;
            bool? invert = parameter == null ? false : (bool)parameter;
            bool positivityReturn = true;
            if (invert.HasValue && invert.Value)
            {
                positivityReturn = !positivityReturn;
            }

            bool result = false;
            switch (val)
            {
                case Visibility.Visible:
                    result = positivityReturn;
                    break;
                case Visibility.Hidden:
                    result = !positivityReturn;
                    break;
                case Visibility.Collapsed:
                    result = !positivityReturn;
                    break;
            }

            return result;
        }
    }
}
