using System;
using System.Globalization;
using System.Windows.Data;
using TreeManager.Core.Domain;

namespace TreeManager.App.Converters;

public sealed class SexToBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not Sex actual || parameter is not Sex target) return false;
        return actual == target;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is true && parameter is Sex target) return target;
        return Binding.DoNothing;
    }
}
