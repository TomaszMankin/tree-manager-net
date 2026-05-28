using CommunityToolkit.Mvvm.ComponentModel;

namespace TreeManager.App.ViewModels;

public sealed partial class OptionalDatePickerViewModel : ObservableObject
{
    [ObservableProperty] private string _day;
    [ObservableProperty] private string _month;
    [ObservableProperty] private string _year;
    [ObservableProperty] private bool _isEnabled = true;

    partial void OnDayChanged(string value) => OnPropertyChanged(nameof(IsDayValid));
    partial void OnMonthChanged(string value) => OnPropertyChanged(nameof(IsMonthValid));
    partial void OnYearChanged(string value) => OnPropertyChanged(nameof(IsYearValid));

    public bool IsDayValid => IsIntFieldValid(Day, 1, 31);
    public bool IsMonthValid => IsIntFieldValid(Month, 1, 12);
    public bool IsYearValid => IsYearFieldValid(Year);

    private static bool IsIntFieldValid(string value, int min, int max)
    {
        if (string.IsNullOrEmpty(value)) { return true; }
        return int.TryParse(value, out var i) && i >= min && i <= max;
    }

    private static bool IsYearFieldValid(string value)
    {
        if (string.IsNullOrEmpty(value)) { return true; }
        if (value.Length != 4) { return false; }
        // Partial years are trailing-truncation only (e.g. "184-", "18--"): digits first, then dashes, no interleaving.
        var seenDash = false;
        foreach (var c in value)
        {
            if (c == '-') { seenDash = true; }
            else if (char.IsDigit(c))
            {
                if (seenDash) { return false; }
            }
            else { return false; }
        }
        return true;
    }
}
