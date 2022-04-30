using HDDL.IO;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace HDDL.UI.WPF.Converters
{
    [ValueConversion(typeof(string), typeof(string))]
    public class DiskSizeFormatConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value != null)
            {
                if (value is string &&
                    long.TryParse((string)value, out long val))
                {
                    return DiskHelper.ShortenSize(val);
                }
                else if (value is long)
                {
                    return DiskHelper.ShortenSize((long)value);
                }
            }

            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
