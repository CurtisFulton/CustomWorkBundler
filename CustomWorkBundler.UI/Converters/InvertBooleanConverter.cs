using System;
using System.Globalization;
using System.Windows.Data;

namespace CustomWorkBundler.UI
{
    public class InvertBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            bool currentValue = System.Convert.ToBoolean(value);

            return !currentValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            bool currentValue = System.Convert.ToBoolean(value);

            return !currentValue;
        }
    }
}
