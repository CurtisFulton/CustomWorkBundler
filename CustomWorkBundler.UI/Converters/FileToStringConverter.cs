using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Data;

namespace CustomWorkBundler.UI
{
    public class FileToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return (value as FileInfo[])?.Select(f => Path.GetFileNameWithoutExtension(f.Name)).ToArray();
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new System.NotImplementedException();
        }
    }
}
