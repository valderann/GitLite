using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using GitLite.Services;

namespace GitLite.ValueConverter;

public class FileIconConverter : DependencyObject, IValueConverter
{
    public object Convert(object value, Type targetType,
        object parameter, CultureInfo culture)
    {
        if (value is not string fileName) return Binding.DoNothing;

        return IconService.FindIconForFilename(fileName, false);
    }

    public object ConvertBack(object value, Type targetType,
        object parameter, CultureInfo culture)
    {
        return Binding.DoNothing;
    }
}