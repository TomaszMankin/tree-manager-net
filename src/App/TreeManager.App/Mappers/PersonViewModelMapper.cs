using System;
using TreeManager.App.ViewModels;
using TreeManager.Core.Domain;

namespace TreeManager.App.Mappers;

public static class PersonViewModelMapper
{
    private const string Unknown = "(nieznane)";

    public static MeFile ToMeFile(this PersonViewModel vm, MeFile existing = null)
    {
        ArgumentNullException.ThrowIfNull(vm);

        var baseFile = existing ?? new MeFile();

        return baseFile with
        {
            UniqueIdentifier = vm.UniqueIdentifier == Guid.Empty
                ? baseFile.UniqueIdentifier
                : vm.UniqueIdentifier,
            PersonName = vm.PersonName,
            Location = vm.Location,
            FirstName = string.IsNullOrEmpty(vm.FirstName) ? Unknown : vm.FirstName,
            OtherFirstNames = vm.OtherFirstNames,
            LastName = string.IsNullOrEmpty(vm.LastName) ? Unknown : vm.LastName,
            OtherLastNames = vm.OtherLastNames,
            MaidenName = vm.MaidenName,
            OtherMaidenNames = vm.OtherMaidenNames,
            HasMaidenName = vm.HasMaidenName,
            Sex = vm.Sex,
        };
    }
}
