using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Data;

namespace CustomWorkBundler.WPF.Converters
{
    public class DirectoryToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {

            if (value is DirectoryInfo[] directories) {
                return directories.Select(dir => dir.Name).ToArray();
            } else if (value is DirectoryInfo directory) {
                return directory.Name;
            } else if (value is string[] directoryPaths) {
                return directoryPaths.Select(dir => new DirectoryInfo(dir).Name).ToArray();
            } else if (value is string directoryPath) {
                return new DirectoryInfo(directoryPath).Name;
            }

            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }
}