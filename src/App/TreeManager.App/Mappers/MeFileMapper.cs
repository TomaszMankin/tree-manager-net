using System;
using TreeManager.App.ViewModels;
using TreeManager.Core.Domain;

namespace TreeManager.App.Mappers;

public static class MeFileMapper
{
    private const string Unknown = "(nieznane)";

    public static PersonViewModel ToViewModel(this MeFile source)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));

        return new PersonViewModel
        {
            UniqueIdentifier = source.UniqueIdentifier,
            PersonName = source.PersonName,
            Location = source.Location,
            FirstName = source.FirstName,
            OtherFirstNames = source.OtherFirstNames,
            LastName = source.LastName,
            OtherLastNames = source.OtherLastNames,
            MaidenName = source.MaidenName,
            OtherMaidenNames = source.OtherMaidenNames,
            HasMaidenName = source.HasMaidenName,
            Sex = source.Sex,
        };
    }

    public static MeFile ToMeFile(this PersonViewModel source, MeFile existing = null)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));

        var baseFile = existing ?? new MeFile();

        return baseFile with
        {
            UniqueIdentifier = source.UniqueIdentifier == Guid.Empty
                ? baseFile.UniqueIdentifier
                : source.UniqueIdentifier,
            PersonName = source.PersonName,
            Location = source.Location,
            FirstName = string.IsNullOrEmpty(source.FirstName) ? Unknown : source.FirstName,
            OtherFirstNames = source.OtherFirstNames,
            LastName = string.IsNullOrEmpty(source.LastName) ? Unknown : source.LastName,
            OtherLastNames = source.OtherLastNames,
            MaidenName = source.MaidenName,
            OtherMaidenNames = source.OtherMaidenNames,
            HasMaidenName = source.HasMaidenName,
            Sex = source.Sex,
        };
    }
}
