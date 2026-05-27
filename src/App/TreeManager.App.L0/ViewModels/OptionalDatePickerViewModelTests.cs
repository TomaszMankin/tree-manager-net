using TreeManager.App.ViewModels;
using TreeManager.Common.TestUtilities;

namespace TreeManager.App.L0.ViewModels;

public class OptionalDatePickerViewModelTests
{
    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void DayOptions_ShrinksToTwentyEight_WhenMonthChangedToFebruary()
    {
        //Arrange
        var vm = new OptionalDatePickerViewModel { Month = 1, Day = 30 };

        //Act
        vm.Month = 2;

        //Assert
        Assert.Equal(29, vm.DayOptions.Count); // null + 1..28
        Assert.Null(vm.Day);
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void DayOptions_ContainsThirtyTwo_WhenMonthIsJanuary()
    {
        //Arrange
        var vm = new OptionalDatePickerViewModel { Month = 1 };

        //Act
        var count = vm.DayOptions.Count;

        //Assert
        Assert.Equal(32, count); // null + 1..31
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void DayOptions_ContainsThirtyOne_WhenMonthIsApril()
    {
        //Arrange
        var vm = new OptionalDatePickerViewModel { Month = 4 };

        //Act
        var count = vm.DayOptions.Count;

        //Assert
        Assert.Equal(31, count); // null + 1..30
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void DayOptions_FirstItemIsNull_WhenMonthIsAny()
    {
        //Arrange
        var vm = new OptionalDatePickerViewModel { Month = 3 };

        //Act
        var first = vm.DayOptions[0];

        //Assert
        Assert.Null(first);
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void Day_ResetsToNull_WhenMonthChangedMakesItOutOfRange()
    {
        //Arrange
        var vm = new OptionalDatePickerViewModel { Month = 3, Day = 31 };

        //Act
        vm.Month = 4; // April max = 30

        //Assert
        Assert.Null(vm.Day);
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void Day_Preserved_WhenMonthChangedAndDayStillValid()
    {
        //Arrange
        var vm = new OptionalDatePickerViewModel { Month = 1, Day = 15 };

        //Act
        vm.Month = 2;

        //Assert
        Assert.Equal(15, vm.Day);
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void MonthOptions_ContainsThirteen_WhenAccessed()
    {
        //Arrange
        //Act
        var count = OptionalDatePickerViewModel.MonthOptions.Count;

        //Assert
        Assert.Equal(13, count); // null + 1..12
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void IsEnabled_DefaultsToTrue_WhenInstantiated()
    {
        //Arrange
        //Act
        var vm = new OptionalDatePickerViewModel();

        //Assert
        Assert.True(vm.IsEnabled);
    }
}
