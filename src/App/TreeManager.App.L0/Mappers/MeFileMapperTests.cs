using System;
using System.Collections.Generic;
using TreeManager.App.Mappers;
using TreeManager.App.ViewModels;
using TreeManager.Common.TestUtilities;
using TreeManager.Core.Domain;

namespace TreeManager.App.L0.Mappers;

public class MeFileMapperTests
{
    private const string SampleFirstName = "Jan";
    private const string SampleLastName = "Kowalski";
    private const string SamplePersonName = "Jan Kowalski";
    private const string SampleLocation = @"C:\fake\tree\Kowalski_Jan";
    private static readonly Guid SampleGuid = Guid.Parse("aaaaaaaa-1111-2222-3333-bbbbbbbbbbbb");

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void ToViewModel_CopiesAllNameFields_WhenSourceIsValid()
    {
        //Arrange
        var source = BuildSampleMeFile();

        //Act
        var result = source.ToViewModel();

        //Assert
        Assert.Equal(SampleGuid, result.UniqueIdentifier);
        Assert.Equal(SamplePersonName, result.PersonName);
        Assert.Equal(SampleLocation, result.Location);
        Assert.Equal(SampleFirstName, result.FirstName);
        Assert.Equal("Adam", result.OtherFirstNames);
        Assert.Equal(SampleLastName, result.LastName);
        Assert.Equal("Kowalsky", result.OtherLastNames);
        Assert.Equal("Nowak", result.MaidenName);
        Assert.Equal(string.Empty, result.OtherMaidenNames);
        Assert.True(result.HasMaidenName);
        Assert.Equal(Sex.Female, result.Sex);
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void ToViewModel_PreservesSexEnum_WhenSourceSexIsFemale()
    {
        //Arrange
        var source = BuildSampleMeFile();

        //Act
        var result = source.ToViewModel();

        //Assert
        Assert.Equal(Sex.Female, result.Sex);
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void ToViewModel_Throws_WhenSourceIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => ((MeFile)null).ToViewModel());
    }

    private static MeFile BuildSampleMeFile() => new()
    {
        UniqueIdentifier = SampleGuid,
        PersonName = SamplePersonName,
        Location = SampleLocation,
        FirstName = SampleFirstName,
        OtherFirstNames = "Adam",
        LastName = SampleLastName,
        OtherLastNames = "Kowalsky",
        MaidenName = "Nowak",
        OtherMaidenNames = string.Empty,
        HasMaidenName = true,
        Sex = Sex.Female,
        Spouse = new List<string> { "Anna Nowak" },
        SpouseId = new List<Guid> { Guid.Parse("11111111-1111-1111-1111-111111111111") },
        Children = new List<string> { "Adam Kowalski" },
        ChildrenId = new List<Guid> { Guid.Parse("22222222-2222-2222-2222-222222222222") },
        Parents = new List<string> { "Piotr Kowalski" },
        ParentsId = new List<Guid> { Guid.Parse("33333333-3333-3333-3333-333333333333") },
        Siblings = new List<string> { "Maria Kowalska" },
        SiblingsId = new List<Guid> { Guid.Parse("44444444-4444-4444-4444-444444444444") },
        Notes = "Test notes",
        DatesOfBirth = "15|03|1950",
        DatesOfDeath = string.Empty,
    };
}
