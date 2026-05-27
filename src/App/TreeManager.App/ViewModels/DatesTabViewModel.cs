using CommunityToolkit.Mvvm.ComponentModel;

namespace TreeManager.App.ViewModels;

public sealed partial class DatesTabViewModel : ObservableObject
{
    public OptionalDatePickerViewModel BirthDate { get; } = new();
    public OptionalDatePickerViewModel DeathDate { get; } = new() { IsEnabled = false };

    [ObservableProperty] private bool _isDeceased;

    partial void OnIsDeceasedChanged(bool value)
    {
        DeathDate.IsEnabled = value;
        if (!value)
        {
            DeathDate.Day = null;
            DeathDate.Month = null;
            DeathDate.Year = null;
        }
    }
}
