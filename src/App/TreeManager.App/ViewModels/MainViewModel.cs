using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace TreeManager.App.ViewModels;

public sealed partial class MainViewModel : ObservableObject
{
    public PersonViewModel Person { get; }

    public MainViewModel(PersonViewModel person)
    {
        Person = person;
    }

    [ObservableProperty]
    private AppMode _currentMode = AppMode.Add;

    [RelayCommand]
    private void SwitchMode(AppMode targetMode) => CurrentMode = targetMode;
}
