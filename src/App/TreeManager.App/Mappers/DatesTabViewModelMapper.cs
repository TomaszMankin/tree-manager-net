using System;
using TreeManager.App.ViewModels;
using TreeManager.Core.Domain;

namespace TreeManager.App.Mappers;

public static class DatesTabViewModelMapper
{
    public static DatesTabViewModel ToDatesTabViewModel(this MeFile meFile)
    {
        if (meFile == null) throw new ArgumentNullException(nameof(meFile));

        var vm = new DatesTabViewModel();

        if (!string.IsNullOrEmpty(meFile.DatesOfBirth))
        {
            var birth = meFile.DatesOfBirth.ToPartialDate();
            vm.BirthDate.Day = birth.Day;
            vm.BirthDate.Month = birth.Month;
            vm.BirthDate.Year = birth.Year;
        }

        if (!string.IsNullOrEmpty(meFile.DatesOfDeath))
        {
            vm.IsDeceased = true;
            var death = meFile.DatesOfDeath.ToPartialDate();
            vm.DeathDate.Day = death.Day;
            vm.DeathDate.Month = death.Month;
            vm.DeathDate.Year = death.Year;
        }

        return vm;
    }

    public static MeFile WithDatesFrom(this MeFile baseFile, DatesTabViewModel vm)
    {
        if (baseFile == null) throw new ArgumentNullException(nameof(baseFile));
        if (vm == null) throw new ArgumentNullException(nameof(vm));

        var birth = new PartialDate(vm.BirthDate.Day, vm.BirthDate.Month, vm.BirthDate.Year);
        var death = vm.IsDeceased
            ? new PartialDate(vm.DeathDate.Day, vm.DeathDate.Month, vm.DeathDate.Year)
            : (PartialDate?)null;

        return baseFile with
        {
            DatesOfBirth = birth.ToSerializedString(),
            DatesOfDeath = death.HasValue ? death.Value.ToSerializedString() : string.Empty,
        };
    }
}
