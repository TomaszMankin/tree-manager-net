using System;
using System.Collections.Generic;
using TreeManager.App.Services;
using TreeManager.App.ViewModels;
using TreeManager.Common.TestUtilities;
using TreeManager.Core.Domain;

namespace TreeManager.App.L0.Services;

public class PersonViewModelMapperTests
{
    private readonly PersonViewModelMapper _sut;

    private const string SampleFirstName = "Jan";
    private const string SampleLastName = "Kowalski";
    private const string SamplePersonName = "Jan Kowalski";
    private const string SampleLocation = @"C:\fake\tree\Kowalski_Jan";
    private static readonly Guid SampleGuid = Guid.Parse("aaaaaaaa-1111-2222-3333-bbbbbbbbbbbb");

    public PersonViewModelMapperTests()
    {
        _sut = new PersonViewModelMapper();
    }

    #region FromMeFile

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void FromMeFile_CopiesAllNameFields_WhenSourceIsValid()
    {
        //Arrange
        var source = BuildSampleMeFile();

        //Act
        var result = _sut.FromMeFile(source);

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
    public void FromMeFile_PreservesSexEnum_WhenSourceSexIsFemale()
    {
        //Arrange
        var source = BuildSampleMeFile();

        //Act
        var result = _sut.FromMeFile(source);

        //Assert
        Assert.Equal(Sex.Female, result.Sex);
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void FromMeFile_Throws_WhenSourceIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => _sut.FromMeFile(null));
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
        var result = _sut.ToMeFile(vm);

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
        var result = _sut.ToMeFile(vm, existing);

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
        Assert.Throws<ArgumentNullException>(() => _sut.ToMeFile(null));
    }

    #endregion

    #region Roundtrip

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void Roundtrip_PreservesAllFields_WhenMappedThroughVm()
    {
        //Arrange
        var original = BuildSampleMeFile();

        //Act
        var vm = _sut.FromMeFile(original);
        vm.FirstName = "Marek";
        var result = _sut.ToMeFile(vm, original);

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
