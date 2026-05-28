using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TreeManager.Core.Domain;

namespace TreeManager.App.ViewModels;

public sealed partial class MultiPersonPickerViewModel : ObservableObject
{
    private readonly List<PersonSummary> _allPeople = new();
    private HashSet<Guid> _excludedIds = new();

    [ObservableProperty]
    private string _searchText = string.Empty;

    public ObservableCollection<PersonSummary> Selected { get; } = new();

    public IReadOnlyList<PersonSummary> Candidates => ComputeCandidates();

    public MultiPersonPickerViewModel()
    {
        Selected.CollectionChanged += (_, _) => OnPropertyChanged(nameof(Candidates));
    }

    public void LoadPeople(IEnumerable<PersonSummary> people)
    {
        _allPeople.Clear();
        _allPeople.AddRange(people);
        OnPropertyChanged(nameof(Candidates));
    }

    public void SetExclusions(IEnumerable<Guid> excludedIds)
    {
        _excludedIds = new HashSet<Guid>(excludedIds);
        OnPropertyChanged(nameof(Candidates));
    }

    partial void OnSearchTextChanged(string value) => OnPropertyChanged(nameof(Candidates));

    [RelayCommand]
    private void Add(PersonSummary person)
    {
        if (person != null && !Selected.Any(s => s.UniqueIdentifier == person.UniqueIdentifier))
        {
            Selected.Add(person);
        }
    }

    [RelayCommand]
    private void Remove(PersonSummary person)
    {
        if (person == null) { return; }
        var existing = Selected.FirstOrDefault(s => s.UniqueIdentifier == person.UniqueIdentifier);
        if (existing != null)
        {
            Selected.Remove(existing);
        }
    }

    private IReadOnlyList<PersonSummary> ComputeCandidates()
    {
        var selectedIds = Selected.Select(p => p.UniqueIdentifier).ToHashSet();
        return _allPeople
            .Where(p => !_excludedIds.Contains(p.UniqueIdentifier))
            .Where(p => !selectedIds.Contains(p.UniqueIdentifier))
            .Where(p => string.IsNullOrEmpty(SearchText) ||
                        p.DisplayName.Contains(SearchText, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }
}
