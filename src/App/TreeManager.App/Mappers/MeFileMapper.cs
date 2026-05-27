using TreeManager.App.ViewModels;
using TreeManager.Core.Domain;

namespace TreeManager.App.Mappers;

public static class MeFileMapper
{
    public static PersonViewModel ToViewModel(this MeFile meFile)
    {
        ArgumentNullException.ThrowIfNull(meFile);

        return new PersonViewModel
        {
            UniqueIdentifier = meFile.UniqueIdentifier,
            PersonName = meFile.PersonName,
            Location = meFile.Location,
            FirstName = meFile.FirstName,
            OtherFirstNames = meFile.OtherFirstNames,
            LastName = meFile.LastName,
            OtherLastNames = meFile.OtherLastNames,
            MaidenName = meFile.MaidenName,
            OtherMaidenNames = meFile.OtherMaidenNames,
            HasMaidenName = meFile.HasMaidenName,
            Sex = meFile.Sex,
        };
    }
}
