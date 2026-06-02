using CommunityToolkit.Mvvm.ComponentModel;
using TreeManager.App.Mappers;
using TreeManager.Core.Domain;

namespace TreeManager.App.ViewModels;

public sealed partial class NotesTabViewModel : ObservableObject
{
    [ObservableProperty]
    private string _notes = string.Empty;

    public void Reset(MeFile meFile)
    {
        var temp = meFile.ToNotesTabViewModel();
        Notes = temp.Notes;
    }
}
