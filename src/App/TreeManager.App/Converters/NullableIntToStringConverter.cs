using System;
using System.Globalization;
using System.Windows.Data;

namespace TreeManager.App.Converters;

public sealed class NullableIntToStringConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is null) return "--";
        if (value is int i)
        {
            var fmt = parameter as string;
            if (fmt == "dd" || fmt == "mm") return i.ToString("D2");
            if (fmt == "yyyy") return i.ToString("D4");
            return i.ToString();
        }
        return value.ToString();
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
