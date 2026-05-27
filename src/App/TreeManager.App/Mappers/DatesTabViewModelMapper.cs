using System;
using TreeManager.App.ViewModels;
using TreeManager.Core.Domain;

namespace TreeManager.App.Mappers;

public static class DatesTabViewModelMapper
{
    public static DatesTabViewModel ToDatesTabViewModel(this MeFile meFile)
    {
        ArgumentNullException.ThrowIfNull(meFile, nameof(meFile));

        var vm = new DatesTabViewModel();

        if (!string.IsNullOrEmpty(meFile.DatesOfBirth))
        {
            var birth = meFile.DatesOfBirth.ToPartialDate();
            vm.BirthDate.Day = birth.Day;
            vm.BirthDate.Month = birth.Month;
            vm.BirthDate.Year = int.TryParse(birth.Year, out var birthYear) ? birthYear : (int?)null;
        }

        if (!string.IsNullOrEmpty(meFile.DatesOfDeath))
        {
            vm.IsDeceased = true;
            var death = meFile.DatesOfDeath.ToPartialDate();
            vm.DeathDate.Day = death.Day;
            vm.DeathDate.Month = death.Month;
            vm.DeathDate.Year = int.TryParse(death.Year, out var deathYear) ? deathYear : (int?)null;
        }

        return vm;
    }

    public static MeFile ToMeFile(this DatesTabViewModel vm, MeFile existing = null)
    {
        ArgumentNullException.ThrowIfNull(vm, nameof(vm));

        var baseFile = existing ?? new MeFile();
        var birth = new PartialDate(vm.BirthDate.Day, vm.BirthDate.Month, vm.BirthDate.Year?.ToString("D4"));
        var death = vm.IsDeceased
            ? new PartialDate(vm.DeathDate.Day, vm.DeathDate.Month, vm.DeathDate.Year?.ToString("D4"))
            : (PartialDate?)null;

        return baseFile with
        {
            DatesOfBirth = birth.ToSerializedString(),
            DatesOfDeath = death.HasValue ? death.Value.ToSerializedString() : string.Empty,
        };
    }
}
