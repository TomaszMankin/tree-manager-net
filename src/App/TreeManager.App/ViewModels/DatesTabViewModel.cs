using CommunityToolkit.Mvvm.ComponentModel;
using TreeManager.App.Mappers;
using TreeManager.Core.Domain;

namespace TreeManager.App.ViewModels;

public sealed partial class DatesTabViewModel : ObservableObject
{
    public OptionalDatePickerViewModel BirthDate { get; } = new();
    public OptionalDatePickerViewModel DeathDate { get; } = new() { IsEnabled = false };

    [ObservableProperty] private bool _isDeceased;

    public void Reset(MeFile meFile)
    {
        var temp = meFile.ToDatesTabViewModel();

        IsDeceased = temp.IsDeceased;

        BirthDate.Day = temp.BirthDate.Day;
        BirthDate.Month = temp.BirthDate.Month;
        BirthDate.Year = temp.BirthDate.Year;

        if (IsDeceased)
        {
            DeathDate.Day = temp.DeathDate.Day;
            DeathDate.Month = temp.DeathDate.Month;
            DeathDate.Year = temp.DeathDate.Year;
        }
    }

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
