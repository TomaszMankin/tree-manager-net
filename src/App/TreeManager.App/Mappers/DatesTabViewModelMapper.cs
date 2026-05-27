using System;
using TreeManager.App.ViewModels;
using TreeManager.Core.Domain;

namespace TreeManager.App.Mappers;

public static class DatesTabViewModelMapper
{
    public static DatesTabViewModel ToDatesTabViewModel(this MeFile meFile)
    {
        ArgumentNullException.ThrowIfNull(meFile);

        var vm = new DatesTabViewModel();

        if (!string.IsNullOrEmpty(meFile.DatesOfBirth))
        {
            var birth = meFile.DatesOfBirth.ToPartialDate();
            vm.BirthDate.Day = birth.Day?.ToString();
            vm.BirthDate.Month = birth.Month?.ToString();
            vm.BirthDate.Year = birth.Year;
        }

        if (!string.IsNullOrEmpty(meFile.DatesOfDeath))
        {
            vm.IsDeceased = true;
            var death = meFile.DatesOfDeath.ToPartialDate();
            vm.DeathDate.Day = death.Day?.ToString();
            vm.DeathDate.Month = death.Month?.ToString();
            vm.DeathDate.Year = death.Year;
        }

        return vm;
    }

    public static MeFile ToMeFile(this DatesTabViewModel vm, MeFile existing = null)
    {
        ArgumentNullException.ThrowIfNull(vm);

        var baseFile = existing ?? new MeFile();
        var birth = new PartialDate(
            int.TryParse(vm.BirthDate.Day, out var bd) ? bd : (int?)null,
            int.TryParse(vm.BirthDate.Month, out var bm) ? bm : (int?)null,
            vm.BirthDate.Year);

        var death = vm.IsDeceased
            ? new PartialDate(
                int.TryParse(vm.DeathDate.Day, out var dd) ? dd : (int?)null,
                int.TryParse(vm.DeathDate.Month, out var dm) ? dm : (int?)null,
                vm.DeathDate.Year)
            : (PartialDate?)null;

        return baseFile with
        {
            DatesOfBirth = birth.ToSerializedString(),
            DatesOfDeath = death.HasValue ? death.Value.ToSerializedString() : string.Empty,
        };
    }
}
