using TreeManager.App.ViewModels;
using TreeManager.Common.TestUtilities;

namespace TreeManager.App.L0.ViewModels;

public class DatesTabViewModelTests
{
    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void DeathDate_IsDisabled_WhenIsDeceasedIsFalse()
    {
        //Arrange
        var vm = new DatesTabViewModel();

        //Act
        vm.IsDeceased = false;

        //Assert
        Assert.False(vm.DeathDate.IsEnabled);
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
}
