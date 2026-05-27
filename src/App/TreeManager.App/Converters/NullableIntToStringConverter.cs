using System;
using System.Globalization;
using System.Windows.Data;

namespace TreeManager.App.Converters;

public sealed class NullableIntToStringConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        // "--" is the UI label for unknown/unset. "XX" is the wire-format sentinel in PartialDate.
        // VM stores null; this converter shows "--" for null; mapper converts null→"XX" on save.
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
