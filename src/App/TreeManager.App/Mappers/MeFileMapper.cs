using System;
using TreeManager.App.ViewModels;
using TreeManager.Core.Domain;

namespace TreeManager.App.Mappers;

public static class MeFileMapper
{
    public static PersonViewModel ToViewModel(this MeFile meFile)
    {
        ArgumentNullException.ThrowIfNull(meFile);

        var vm = new PersonViewModel
        {
            UniqueIdentifier = meFile.UniqueIdentifier,
            PersonName = meFile.PersonName,
            Location = meFile.Location,
            FirstName = meFile.FirstName,
            LastName = meFile.LastName,
            MaidenName = meFile.MaidenName,
            HasMaidenName = meFile.HasMaidenName,
            Sex = meFile.Sex,
        };
        vm.OtherFirstNames.Load(meFile.OtherFirstNames);
        vm.OtherLastNames.Load(meFile.OtherLastNames);
        vm.OtherMaidenNames.Load(meFile.OtherMaidenNames);
        return vm;
    }
}
