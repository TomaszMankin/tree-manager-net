using System;
using TreeManager.App.ViewModels;
using TreeManager.Core.Domain;

namespace TreeManager.App.Mappers;

public static class NotesTabViewModelMapper
{
    public static NotesTabViewModel ToNotesTabViewModel(this MeFile meFile)
    {
        ArgumentNullException.ThrowIfNull(meFile);

        return new NotesTabViewModel
        {
            Notes = meFile.Notes,
        };
    }

    public static MeFile ToMeFile(this NotesTabViewModel vm, MeFile existing = null)
    {
        ArgumentNullException.ThrowIfNull(vm);

        var baseFile = existing ?? new MeFile();

        return baseFile with
        {
            Notes = vm.Notes,
        };
    }
}
