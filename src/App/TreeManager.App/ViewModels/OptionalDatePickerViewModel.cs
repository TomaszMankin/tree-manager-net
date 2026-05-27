using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.Generic;

namespace TreeManager.App.ViewModels;

public sealed partial class OptionalDatePickerViewModel : ObservableObject
{
    [ObservableProperty] private int? _day;
    [ObservableProperty] private int? _month;
    [ObservableProperty] private int? _year;
    [ObservableProperty] private bool _isEnabled = true;

    partial void OnMonthChanged(int? value)
    {
        var maxDay = GetMaxDay(value);
        if (Day.HasValue && Day.Value > maxDay)
            Day = null;
        OnPropertyChanged(nameof(DayOptions));
    }

    public IReadOnlyList<int?> DayOptions => BuildDayOptions(Month);

    public static IReadOnlyList<int?> MonthOptions { get; } = BuildNullableRange(1, 12);

    public static IReadOnlyList<int?> YearOptions { get; } = BuildYearOptions();

    private static IReadOnlyList<int?> BuildDayOptions(int? month)
    {
        var max = GetMaxDay(month);
        return BuildNullableRange(1, max);
    }

    private static int GetMaxDay(int? month) => month switch
    {
        2 => 28,
        4 or 6 or 9 or 11 => 30,
        _ => 31
    };

    private static IReadOnlyList<int?> BuildNullableRange(int from, int to)
    {
        var list = new List<int?> { null };
        for (int i = from; i <= to; i++)
            list.Add(i);
        return list;
    }

    private static IReadOnlyList<int?> BuildYearOptions()
    {
        var list = new List<int?> { null };
        for (int i = 2100; i >= 1000; i--)
            list.Add(i);
        return list;
    }
}
