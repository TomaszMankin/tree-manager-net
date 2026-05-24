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

    #region ToViewModel

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

    #endregion

    #region ToMeFile

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void ToMeFile_CopiesAllNameFields_WhenSourceIsValid()
    {
        //Arrange
        var vm = new PersonViewModel
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
        };

        //Act
        var result = vm.ToMeFile();

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
    public void ToMeFile_PreservesRelationshipLists_WhenExistingProvided()
    {
        //Arrange
        var existing = BuildSampleMeFile();
        var vm = new PersonViewModel
        {
            UniqueIdentifier = SampleGuid,
            FirstName = "Marek",
            LastName = SampleLastName,
            Sex = Sex.Male,
        };

        //Act
        var result = vm.ToMeFile(existing);

        //Assert — relationship lists and other untouched fields flow through
        Assert.Equal(existing.Spouse, result.Spouse);
        Assert.Equal(existing.SpouseId, result.SpouseId);
        Assert.Equal(existing.Children, result.Children);
        Assert.Equal(existing.ChildrenId, result.ChildrenId);
        Assert.Equal(existing.Parents, result.Parents);
        Assert.Equal(existing.ParentsId, result.ParentsId);
        Assert.Equal(existing.Siblings, result.Siblings);
        Assert.Equal(existing.SiblingsId, result.SiblingsId);
        Assert.Equal(existing.Notes, result.Notes);
        Assert.Equal(existing.DatesOfBirth, result.DatesOfBirth);
        Assert.Equal(existing.DatesOfDeath, result.DatesOfDeath);
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void ToMeFile_Throws_WhenSourceIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => ((PersonViewModel)null).ToMeFile());
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void ToMeFile_FillsUnknown_WhenFirstNameIsEmpty()
    {
        //Arrange
        var vm = new PersonViewModel { FirstName = string.Empty, LastName = "Kowalski", Sex = Sex.Male };

        //Act
        var result = vm.ToMeFile();

        //Assert
        Assert.Equal("(nieznane)", result.FirstName);
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void ToMeFile_FillsUnknown_WhenLastNameIsEmpty()
    {
        //Arrange
        var vm = new PersonViewModel { FirstName = "Jan", LastName = string.Empty, Sex = Sex.Male };

        //Act
        var result = vm.ToMeFile();

        //Assert
        Assert.Equal("(nieznane)", result.LastName);
    }

    #endregion

    #region Roundtrip

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void ToMeFile_PreservesRelationshipsAndUpdatesFields_WhenVmModifiedAfterLoad()
    {
        //Arrange
        var original = BuildSampleMeFile();
        var vm = original.ToViewModel();
        vm.FirstName = "Marek";

        //Act
        var result = vm.ToMeFile(original);

        //Assert — mutated field updated; relationship lists preserved; identifier preserved
        Assert.Equal("Marek", result.FirstName);
        Assert.Equal(original.Spouse, result.Spouse);
        Assert.Equal(original.Notes, result.Notes);
        Assert.Equal(SampleGuid, result.UniqueIdentifier);
    }

    #endregion

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
