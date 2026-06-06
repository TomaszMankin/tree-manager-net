using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace TreeManager.App.ViewModels;

public sealed partial class StringListViewModel : ObservableObject
{
    public ObservableCollection<string> Items { get; } = new();

    [ObservableProperty]
    private string _input = string.Empty;

    [RelayCommand]
    private void Add()
    {
        var trimmed = (Input ?? string.Empty).Trim();
        if (trimmed.Length == 0)
        {
            return;
        }

        Items.Add(trimmed);
        Input = string.Empty;
    }

    [RelayCommand]
    private void Remove(string item) => Items.Remove(item);

    public string Serialize() => string.Join("\n", Items);

    public void Load(string serialized)
    {
        Items.Clear();
        if (string.IsNullOrEmpty(serialized))
        {
            return;
        }

        foreach (var part in serialized.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            Items.Add(part);
        }
    }
}
