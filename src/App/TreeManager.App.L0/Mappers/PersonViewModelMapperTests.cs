using System;
using AutoBogus;
using TreeManager.App.Mappers;
using TreeManager.App.ViewModels;
using TreeManager.Common.TestUtilities;
using TreeManager.Core.Domain;

namespace TreeManager.App.L0.Mappers;

public class PersonViewModelMapperTests
{
    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void ToMeFile_CopiesAllNameFields_WhenSourceIsValid()
    {
        //Arrange
        var vm = new AutoFaker<PersonViewModel>()
            .RuleFor(x => x.FirstName, f => f.Name.FirstName())
            .RuleFor(x => x.LastName, f => f.Name.LastName())
            .Generate();

        //Act
        var result = vm.ToMeFile();

        //Assert
        Assert.Equal(vm.UniqueIdentifier, result.UniqueIdentifier);
        Assert.Equal(vm.PersonName, result.PersonName);
        Assert.Equal(vm.Location, result.Location);
        Assert.Equal(vm.FirstName, result.FirstName);
        Assert.Equal(vm.OtherFirstNames, result.OtherFirstNames);
        Assert.Equal(vm.LastName, result.LastName);
        Assert.Equal(vm.OtherLastNames, result.OtherLastNames);
        Assert.Equal(vm.MaidenName, result.MaidenName);
        Assert.Equal(vm.OtherMaidenNames, result.OtherMaidenNames);
        Assert.Equal(vm.HasMaidenName, result.HasMaidenName);
        Assert.Equal(vm.Sex, result.Sex);
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void ToMeFile_PreservesRelationshipLists_WhenExistingProvided()
    {
        //Arrange
        var existing = AutoFaker.Generate<MeFile>();
        var vm = new PersonViewModel
        {
            UniqueIdentifier = existing.UniqueIdentifier,
            FirstName = "Marek",
            LastName = "Kowalski",
            Sex = Sex.Male,
        };

        //Act
        var result = vm.ToMeFile(existing);

        //Assert
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

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void ToMeFile_PreservesRelationshipsAndUpdatesFields_WhenVmModifiedAfterLoad()
    {
        //Arrange
        var original = AutoFaker.Generate<MeFile>();
        var vm = original.ToViewModel();
        vm.FirstName = "Marek";

        //Act
        var result = vm.ToMeFile(original);

        //Assert
        Assert.Equal("Marek", result.FirstName);
        Assert.Equal(original.Spouse, result.Spouse);
        Assert.Equal(original.Notes, result.Notes);
        Assert.Equal(original.UniqueIdentifier, result.UniqueIdentifier);
    }
}
