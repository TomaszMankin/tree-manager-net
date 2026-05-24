using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace TreeManager.App.ViewModels;

public sealed partial class MainViewModel : ObservableObject
{
    [ObservableProperty]
    private AppMode _currentMode = AppMode.Add;

    [RelayCommand]
    private void SwitchMode(AppMode targetMode) => CurrentMode = targetMode;
}
