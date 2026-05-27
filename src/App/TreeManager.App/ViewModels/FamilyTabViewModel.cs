using CommunityToolkit.Mvvm.ComponentModel;
using TreeManager.Core.Domain;

namespace TreeManager.App.ViewModels;

public sealed partial class FamilyTabViewModel : ObservableObject
{
    public MultiPersonPickerViewModel Parents { get; } = new();
    public MultiPersonPickerViewModel Children { get; } = new();
    public MultiPersonPickerViewModel Spouses { get; } = new();
    public MultiPersonPickerViewModel Siblings { get; } = new();

    [ObservableProperty]
    private Guid? _loadedPersonId;

    private readonly MultiPersonPickerViewModel[] _allPickers;

    public FamilyTabViewModel()
    {
        _allPickers = new[] { Parents, Children, Spouses, Siblings };
        foreach (var picker in _allPickers)
        {
            picker.Selected.CollectionChanged += (_, _) => RecomputeExclusions();
        }
    }

    public void LoadPeople(IEnumerable<PersonSummary> people)
    {
        var list = people.ToList();
        foreach (var picker in _allPickers)
        {
            picker.LoadPeople(list);
        }
        RecomputeExclusions();
    }

    partial void OnLoadedPersonIdChanged(Guid? value) => RecomputeExclusions();

    private void RecomputeExclusions()
    {
        foreach (var picker in _allPickers)
        {
            var othersSelected = _allPickers
                .Where(p => p != picker)
                .SelectMany(p => p.Selected.Select(s => s.UniqueIdentifier));
            var excluded = LoadedPersonId.HasValue
                ? othersSelected.Append(LoadedPersonId.Value)
                : othersSelected;
            picker.SetExclusions(excluded);
        }
    }
}
