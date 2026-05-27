using System;
using TreeManager.App.Mappers;
using TreeManager.App.ViewModels;
using TreeManager.Common.TestUtilities;
using TreeManager.Core.Domain;

namespace TreeManager.App.L0.Mappers;

public class DatesTabViewModelMapperTests
{
    [Theory]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    [InlineData("12|03|1947", "12", "3", "1947")]
    [InlineData("--|03|1947", null, "3", "1947")]
    [InlineData("12|03|19--", "12", "3", "19--")]
    public void ToDatesTabViewModel_MapsBirthDateComponents_WhenDatesOfBirthIsProvided(
        string input, string expectedDay, string expectedMonth, string expectedYear)
    {
        //Arrange
        var meFile = new MeFile { DatesOfBirth = input };

        //Act
        var vm = meFile.ToDatesTabViewModel();

        //Assert
        Assert.Equal(expectedDay, vm.BirthDate.Day);
        Assert.Equal(expectedMonth, vm.BirthDate.Month);
        Assert.Equal(expectedYear, vm.BirthDate.Year);
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void ToDatesTabViewModel_SetsBirthDateToAllUnknown_WhenDatesOfBirthIsEmpty()
    {
        //Arrange
        var meFile = new MeFile { DatesOfBirth = string.Empty };

        //Act
        var vm = meFile.ToDatesTabViewModel();

        //Assert
        Assert.Null(vm.BirthDate.Day);
        Assert.Null(vm.BirthDate.Month);
        Assert.Null(vm.BirthDate.Year);
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void ToDatesTabViewModel_SetsIsDeceased_WhenDatesOfDeathIsNonEmpty()
    {
        //Arrange
        var meFile = new MeFile { DatesOfDeath = "--|--|----" };

        //Act
        var vm = meFile.ToDatesTabViewModel();

        //Assert
        Assert.True(vm.IsDeceased);
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void ToDatesTabViewModel_DoesNotSetIsDeceased_WhenDatesOfDeathIsEmpty()
    {
        //Arrange
        var meFile = new MeFile { DatesOfDeath = string.Empty };

        //Act
        var vm = meFile.ToDatesTabViewModel();

        //Assert
        Assert.False(vm.IsDeceased);
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void ToDatesTabViewModel_DoesNotSetIsDeceased_WhenDatesOfDeathIsNull()
    {
        //Arrange
        var meFile = new MeFile { DatesOfDeath = null };

        //Act
        var vm = meFile.ToDatesTabViewModel();

        //Assert
        Assert.False(vm.IsDeceased);
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void ToDatesTabViewModel_ThrowsArgumentNullException_WhenMeFileIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => ((MeFile)null).ToDatesTabViewModel());
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void ToMeFile_SerializesFullBirthDate_WhenAllComponentsKnown()
    {
        //Arrange
        var vm = new DatesTabViewModel();
        vm.BirthDate.Day = "12";
        vm.BirthDate.Month = "3";
        vm.BirthDate.Year = "1947";

        //Act
        var result = vm.ToMeFile();

        //Assert
        Assert.Equal("12|03|1947", result.DatesOfBirth);
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void ToMeFile_SerializesEmptyDatesOfDeath_WhenIsDeceasedIsFalse()
    {
        //Arrange
        var vm = new DatesTabViewModel { IsDeceased = false };

        //Act
        var result = vm.ToMeFile();

        //Assert
        Assert.Equal(string.Empty, result.DatesOfDeath);
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void ToMeFile_SerializesDeathDate_WhenIsDeceasedIsTrue()
    {
        //Arrange
        var vm = new DatesTabViewModel();
        vm.IsDeceased = true;
        vm.DeathDate.Day = null;
        vm.DeathDate.Month = null;
        vm.DeathDate.Year = null;

        //Act
        var result = vm.ToMeFile();

        //Assert
        Assert.Equal("--|--|----", result.DatesOfDeath);
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void ToMeFile_IgnoresDeathDateValues_WhenIsDeceasedToggledBackToFalse()
    {
        //Arrange
        var vm = new DatesTabViewModel();
        vm.IsDeceased = true;
        vm.DeathDate.Day = "5";
        vm.DeathDate.Month = "6";
        vm.DeathDate.Year = "2020";
        vm.IsDeceased = false;

        //Act
        var result = vm.ToMeFile();

        //Assert
        Assert.Equal(string.Empty, result.DatesOfDeath);
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void ToMeFile_PreservesExistingFields_WhenExistingMeFileIsProvided()
    {
        //Arrange
        var existing = new MeFile { FirstName = "Jan", LastName = "Kowalski" };
        var vm = new DatesTabViewModel();
        vm.BirthDate.Year = "1947";

        //Act
        var result = vm.ToMeFile(existing);

        //Assert
        Assert.Equal("Jan", result.FirstName);
        Assert.Equal("Kowalski", result.LastName);
        Assert.Equal("--|--|1947", result.DatesOfBirth);
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void ToMeFile_ThrowsArgumentNullException_WhenViewModelIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => ((DatesTabViewModel)null).ToMeFile());
    }
}
