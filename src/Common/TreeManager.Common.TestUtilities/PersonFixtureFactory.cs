using System;
using System.Collections.Generic;
using System.Linq;
using TreeManager.Core.Domain;

namespace TreeManager.Common.TestUtilities;

/// <summary>Shared factory for building in-memory <see cref="MeFile"/> test fixtures.</summary>
public static class PersonFixtureFactory
{
    private const string DefaultRoot = @"C:\fake\root";

    /// <summary>Builds a <see cref="MeFile"/> with the given identity and relationship lists.</summary>
    public static MeFile Build(
        Guid id,
        string firstName,
        string lastName,
        Sex sex,
        List<Guid> spouseIds = null,
        List<Guid> parentIds = null,
        List<Guid> childrenIds = null,
        bool hasMaidenName = false,
        string maidenName = "",
        string root = DefaultRoot)
    {
        return new MeFile
        {
            UniqueIdentifier = id,
            FirstName = firstName,
            LastName = lastName,
            Sex = sex,
            HasMaidenName = hasMaidenName,
            MaidenName = maidenName,
            Location = root + $@"\Lista osób\{firstName} {lastName}",
            SpouseId = spouseIds ?? [],
            ParentsId = parentIds ?? [],
            ChildrenId = childrenIds ?? [],
        };
    }

    /// <summary>Builds a <see cref="IReadOnlyDictionary{Guid,MeFile}"/> keyed by <see cref="MeFile.UniqueIdentifier"/>.</summary>
    public static IReadOnlyDictionary<Guid, MeFile> BuildMap(params MeFile[] people)
    {
        return people.ToDictionary(p => p.UniqueIdentifier);
    }
}
