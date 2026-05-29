using System;
using TreeManager.App.Services;
using TreeManager.App.ViewModels;
using TreeManager.Common.TestUtilities;

namespace TreeManager.App.L0.Services;

public class FolderNameGeneratorTests
{
    #region ToFolderName

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void ToFolderName_ReturnsFullName_WhenAllFieldsSet()
    {
        //Arrange
        var vm = new PersonViewModel
        {
            FirstName = "Jan",
            LastName = "Kowalski",
        };

        //Act
        var result = vm.ToFolderName();

        //Assert
        Assert.Equal("Jan Kowalski", result);
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void ToFolderName_UsesUnknown_WhenFirstNameIsEmpty()
    {
        //Arrange
        var vm = new PersonViewModel
        {
            FirstName = string.Empty,
            LastName = "Kowalski",
        };

        //Act
        var result = vm.ToFolderName();

        //Assert
        Assert.Equal("(nieznane) Kowalski", result);
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void ToFolderName_UsesUnknown_WhenLastNameIsEmpty()
    {
        //Arrange
        var vm = new PersonViewModel
        {
            FirstName = "Jan",
            LastName = string.Empty,
        };

        //Act
        var result = vm.ToFolderName();

        //Assert
        Assert.Equal("Jan (nieznane)", result);
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void ToFolderName_AppendsMaidenName_WhenHasMaidenNameIsTrue()
    {
        //Arrange
        var vm = new PersonViewModel
        {
            FirstName = "Maria",
            LastName = "Nowak",
            HasMaidenName = true,
            MaidenName = "Wiśniewska",
        };

        //Act
        var result = vm.ToFolderName();

        //Assert
        Assert.Equal("Maria Nowak zd. Wiśniewska", result);
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void ToFolderName_SkipsMaidenName_WhenHasMaidenNameIsFalse()
    {
        //Arrange
        var vm = new PersonViewModel
        {
            FirstName = "Maria",
            LastName = "Nowak",
            HasMaidenName = false,
            MaidenName = "Wiśniewska",
        };

        //Act
        var result = vm.ToFolderName();

        //Assert
        Assert.Equal("Maria Nowak", result);
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void ToFolderName_AppendsSemicolonOtherLastNames_WhenPresent()
    {
        //Arrange
        var vm = new PersonViewModel
        {
            FirstName = "Jan",
            LastName = "Kowalski",
            OtherLastNames = "Nowak",
        };

        //Act
        var result = vm.ToFolderName();

        //Assert
        Assert.Equal("Jan Kowalski;Nowak", result);
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void ToFolderName_HandlesPolishDiacritics_WhenNameContainsPolishChars()
    {
        //Arrange
        var vm = new PersonViewModel
        {
            FirstName = "Łukasz",
            LastName = "Żółkowski",
        };

        //Act
        var result = vm.ToFolderName();

        //Assert
        Assert.Equal("Łukasz Żółkowski", result);
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void ToFolderName_AppendsOtherMaidenNames_WhenPresent()
    {
        //Arrange
        var vm = new PersonViewModel
        {
            FirstName = "Maria",
            LastName = "Nowak",
            HasMaidenName = true,
            MaidenName = "Wiśniewska",
            OtherMaidenNames = "Kowalska",
        };

        //Act
        var result = vm.ToFolderName();

        //Assert
        Assert.Equal("Maria Nowak zd. Wiśniewska;Kowalska", result);
    }

    #endregion
}
