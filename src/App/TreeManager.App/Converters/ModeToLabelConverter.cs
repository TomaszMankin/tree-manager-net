using System;
using System.Globalization;
using System.Windows.Data;
using TreeManager.App.ViewModels;

namespace TreeManager.App.Converters;

public sealed class ModeToLabelConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not AppMode mode)
        {
            return string.Empty;
        }

        return mode switch
        {
            AppMode.Add => "Nowa osoba",
            AppMode.EditTree => "Edycja osoby",
            AppMode.EditDraft => "Edycja szkicu",
            _ => string.Empty
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException("ModeToLabelConverter is one-way.");
    }
}
