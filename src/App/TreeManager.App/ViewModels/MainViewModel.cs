using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace TreeManager.App.ViewModels;

public sealed partial class MainViewModel : ObservableObject
{
    public PersonViewModel Person { get; }
    public DatesTabViewModel Dates { get; }

    public MainViewModel(PersonViewModel person, DatesTabViewModel dates)
    {
        Person = person;
        Dates = dates;
    }

    [ObservableProperty]
    private AppMode _currentMode = AppMode.Add;

    [RelayCommand]
    private void SwitchMode(AppMode targetMode) => CurrentMode = targetMode;
}
