using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using TreeManager.App.ViewModels;

namespace TreeManager.App.Converters;

public sealed class ModeToBackgroundBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not AppMode mode)
        {
            return DependencyProperty.UnsetValue;
        }

        var resourceKey = mode switch
        {
            AppMode.Add => "AddModeBrush",
            AppMode.EditTree => "EditTreeModeBrush",
            AppMode.EditDraft => "EditDraftModeBrush",
            _ => null
        };

        if (resourceKey == null)
        {
            return DependencyProperty.UnsetValue;
        }

        return Application.Current?.TryFindResource(resourceKey) as Brush ?? DependencyProperty.UnsetValue;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException("ModeToBackgroundBrushConverter is one-way.");
    }
}
