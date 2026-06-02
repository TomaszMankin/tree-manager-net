using System;
using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using TreeManager.Core.Domain;

namespace TreeManager.App.ViewModels;

public sealed partial class PersonPickerViewModel : ObservableObject
{
    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private PersonSummary _selectedPerson;

    private IReadOnlyList<PersonSummary> _allPeople = Array.Empty<PersonSummary>();

    public IReadOnlyList<PersonSummary> FilteredPeople => ComputeFiltered();

    public void LoadPeople(IReadOnlyList<PersonSummary> people)
    {
        _allPeople = people ?? Array.Empty<PersonSummary>();
        OnPropertyChanged(nameof(FilteredPeople));
    }

    partial void OnSearchTextChanged(string value) => OnPropertyChanged(nameof(FilteredPeople));

    private IReadOnlyList<PersonSummary> ComputeFiltered()
    {
        if (string.IsNullOrEmpty(SearchText))
        {
            return _allPeople;
        }

        return _allPeople
            .Where(p => p.DisplayName.Contains(SearchText, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }
}
