using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace GitLite.ValueConverter;

public class BoolToFontColorConverter : DependencyObject, IValueConverter
{
    public object Convert(object value, Type targetType,
        object parameter, CultureInfo culture)
    {
        return ((bool)value) ? new SolidColorBrush(Colors.Black) : new SolidColorBrush(Colors.Gray);
    }

    public object ConvertBack(object value, Type targetType,
        object parameter, CultureInfo culture)
    {
        return Binding.DoNothing;
    }
}