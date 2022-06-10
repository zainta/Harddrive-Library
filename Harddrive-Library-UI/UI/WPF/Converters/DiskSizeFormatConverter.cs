// Copyright (c) Zain Al-Ahmary.  All rights reserved.
// Licensed under the MIT License, (the "License"); you may not use this file except in compliance with the License. 
// You may obtain a copy of the License at https://mit-license.org/

using HDDL.IO;
using System;
using System.Globalization;
using System.Windows.Data;

namespace HDDL.UI.WPF.Converters
{
    [ValueConversion(typeof(string), typeof(string))]
    public class DiskSizeFormatConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value != null &&
                value is System.Data.DataRowView drv && 
                parameter != null &&
                parameter is string key)
            {
                var val = drv[key];
                if (val is string &&
                    long.TryParse((string)val, out long v))
                {
                    return DiskHelper.ShortenSize(v);
                }
                else if (val is long)
                {
                    return DiskHelper.ShortenSize((long)val);
                }
            }

            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
