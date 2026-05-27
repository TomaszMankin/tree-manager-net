using TreeManager.App.ViewModels;
using TreeManager.Common.TestUtilities;

namespace TreeManager.App.L0.ViewModels;

public class OptionalDatePickerViewModelTests
{
    [Theory]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    [InlineData(1, 32)]   // Jan: null + 31
    [InlineData(2, 29)]   // Feb (no year → no leap year): null + 28
    [InlineData(3, 32)]   // Mar: null + 31
    [InlineData(4, 31)]   // Apr: null + 30
    [InlineData(5, 32)]   // May: null + 31
    [InlineData(6, 31)]   // Jun: null + 30
    [InlineData(7, 32)]   // Jul: null + 31
    [InlineData(8, 32)]   // Aug: null + 31
    [InlineData(9, 31)]   // Sep: null + 30
    [InlineData(10, 32)]  // Oct: null + 31
    [InlineData(11, 31)]  // Nov: null + 30
    [InlineData(12, 32)]  // Dec: null + 31
    public void DayOptions_HasExpectedCount_WhenMonthIsSet(int month, int expectedCount)
    {
        //Arrange
        var vm = new OptionalDatePickerViewModel { Month = month };

        //Act
        var count = vm.DayOptions.Count;

        //Assert
        Assert.Equal(expectedCount, count);
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void DayOptions_ContainsThirty_WhenMonthIsFebruaryAndYearIsLeapYear()
    {
        //Arrange
        var vm = new OptionalDatePickerViewModel { Month = 2, Year = 2024 }; // 2024 is a leap year

        //Act
        var count = vm.DayOptions.Count;

        //Assert
        Assert.Equal(30, count); // null + 1..29
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void DayOptions_ShrinksBackToTwentyNine_WhenYearChangedFromLeapToNonLeap()
    {
        //Arrange
        var vm = new OptionalDatePickerViewModel { Month = 2, Year = 2024, Day = 29 }; // Feb 29 valid in 2024

        //Act
        vm.Year = 2023; // non-leap year

        //Assert
        Assert.Equal(29, vm.DayOptions.Count); // null + 1..28
        Assert.Null(vm.Day);                   // day 29 no longer valid, reset
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void DayOptions_ShrinksToTwentyNine_WhenMonthChangedToFebruary()
    {
        //Arrange
        var vm = new OptionalDatePickerViewModel { Month = 1, Day = 30 }; // no year → non-leap

        //Act
        vm.Month = 2;

        //Assert
        Assert.Equal(29, vm.DayOptions.Count); // null + 1..28
        Assert.Null(vm.Day);
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
