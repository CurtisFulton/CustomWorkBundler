using System;
using System.Globalization;
using System.Windows.Data;

namespace CustomWorkBundler.WPF.Converters
{
    public class InvertBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) => !System.Convert.ToBoolean(value);
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) => !System.Convert.ToBoolean(value);
    }
}