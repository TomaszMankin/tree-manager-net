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
            AppMode.Add => "Dodawanie nowej osoby",
            AppMode.EditTree => "Edycja osoby z drzewa",
            AppMode.EditDraft => "Edycja szkicu osoby",
            _ => string.Empty
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException("ModeToLabelConverter is one-way.");
    }
}
