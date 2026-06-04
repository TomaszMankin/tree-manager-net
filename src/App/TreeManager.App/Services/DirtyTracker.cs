using TreeManager.App.Mappers;
using TreeManager.App.ViewModels;
using TreeManager.Core.Domain;

namespace TreeManager.App.Services;

public sealed class DirtyTracker : IDirtyTracker
{
    // Pristine baseline: MeFile assembled from default/empty VMs through the same mapper chain
    // used by the assembly helper. PersonViewModelMapper coerces empty names to "(nieznane)",
    // so a raw new MeFile() would NOT equal this baseline — must route through the mappers.
    private static readonly MeFile PristineBaseline = BuildPristineBaseline();

    public bool IsDirty(MeFile snapshot, MeFile current)
    {
        var compareTo = snapshot ?? PristineBaseline;
        return !current.Equals(compareTo);
    }

    private static MeFile BuildPristineBaseline()
    {
        var personVm = new PersonViewModel();
        var datesVm  = new DatesTabViewModel();
        var familyVm = new FamilyTabViewModel();
        var notesVm  = new NotesTabViewModel();

        var meFile = personVm.ToMeFile();
        meFile = datesVm.ToMeFile(meFile);
        meFile = familyVm.ToMeFile(meFile);
        meFile = notesVm.ToMeFile(meFile);
        return meFile;
    }
}
