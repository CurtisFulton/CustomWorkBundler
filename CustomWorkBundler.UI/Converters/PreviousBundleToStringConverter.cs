﻿using System;
using System.IO;
using System.Linq;
using System.Windows.Data;

namespace CustomWorkBundler.UI
{
    public class PreviousBundleToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return (value as DirectoryInfo[])?.Select(f => f.Parent.Name).ToArray();
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new System.NotImplementedException();
        }
    }
}