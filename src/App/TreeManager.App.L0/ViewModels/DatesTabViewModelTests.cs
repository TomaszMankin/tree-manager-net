using TreeManager.App.Mappers;
using TreeManager.App.ViewModels;
using TreeManager.Common.TestUtilities;
using TreeManager.Core.Domain;

namespace TreeManager.App.L0.ViewModels;

public class DatesTabViewModelTests
{
    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void IsDeceased_DisablesAndClearDeathDate_WhenToggledToFalse()
    {
        //Arrange
        var vm = new DatesTabViewModel();
        vm.IsDeceased = true;
        vm.DeathDate.Day = "10";
        vm.DeathDate.Month = "4";
        vm.DeathDate.Year = "1990";

        //Act
        vm.IsDeceased = false;

        //Assert
        Assert.False(vm.DeathDate.IsEnabled);
        Assert.Null(vm.DeathDate.Day);
        Assert.Null(vm.DeathDate.Month);
        Assert.Null(vm.DeathDate.Year);
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void DeathDate_IsEnabled_WhenIsDeceasedIsTrue()
    {
        //Arrange
        var vm = new DatesTabViewModel();

        //Act
        vm.IsDeceased = true;

        //Assert
        Assert.True(vm.DeathDate.IsEnabled);
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void DeathDate_ClearsAllComponents_WhenIsDeceasedToggledToFalse()
    {
        //Arrange
        var vm = new DatesTabViewModel();
        vm.IsDeceased = true;
        vm.DeathDate.Day = "5";
        vm.DeathDate.Month = "6";
        vm.DeathDate.Year = "2000";

        //Act
        vm.IsDeceased = false;

        //Assert
        Assert.Null(vm.DeathDate.Day);
        Assert.Null(vm.DeathDate.Month);
        Assert.Null(vm.DeathDate.Year);
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void BirthDate_IsEnabledByDefault_WhenInstantiated()
    {
        //Arrange
        //Act
        var vm = new DatesTabViewModel();

        //Assert
        Assert.True(vm.BirthDate.IsEnabled);
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void IsDeceased_DefaultsFalse_WhenInstantiated()
    {
        //Arrange
        //Act
        var vm = new DatesTabViewModel();

        //Assert
        Assert.False(vm.IsDeceased);
    }

    #region Reset

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void Reset_SetsIsDeceased_WhenMeFileHasDatesOfDeath()
    {
        //Arrange
        var vm = new DatesTabViewModel();
        var meFile = new MeFile { DatesOfDeath = "10|04|1990" };

        //Act
        vm.Reset(meFile);

        //Assert
        Assert.True(vm.IsDeceased);
        Assert.Equal("10", vm.DeathDate.Day);
        Assert.Equal("4", vm.DeathDate.Month);
        Assert.Equal("1990", vm.DeathDate.Year);
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void Reset_ClearsDatesAndIsDeceased_WhenMeFileHasNoDatesOfDeath()
    {
        //Arrange
        var vm = new DatesTabViewModel();
        vm.IsDeceased = true;
        vm.DeathDate.Day = "5";
        vm.DeathDate.Month = "6";
        vm.DeathDate.Year = "2000";
        var meFile = new MeFile { DatesOfBirth = "01|01|1950" };

        //Act
        vm.Reset(meFile);

        //Assert
        Assert.False(vm.IsDeceased);
        Assert.Null(vm.DeathDate.Day);
        Assert.Null(vm.DeathDate.Month);
        Assert.Null(vm.DeathDate.Year);
        Assert.Equal("1", vm.BirthDate.Day);
        Assert.Equal("1", vm.BirthDate.Month);
        Assert.Equal("1950", vm.BirthDate.Year);
    }

    #endregion
}
