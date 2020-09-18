using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace GitLite.ValueConverter;

internal class ColumnWidthConverter : DependencyObject, IValueConverter
{
    public object Convert(object value, Type targetType,
        object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }

    public object ConvertBack(object value, Type targetType,
        object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}