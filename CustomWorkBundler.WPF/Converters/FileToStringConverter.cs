using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Data;

namespace CustomWorkBundler.WPF.Converters
{
    public class FileToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is FileInfo[] files) {
                return files.Select(f => Path.GetFileNameWithoutExtension(f.Name)).ToArray();
            } else if (value is FileInfo file) {
                return Path.GetFileNameWithoutExtension(file.Name);
            }

            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }
}