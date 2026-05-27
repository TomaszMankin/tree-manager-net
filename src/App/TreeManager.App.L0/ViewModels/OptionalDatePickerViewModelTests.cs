using TreeManager.App.ViewModels;
using TreeManager.Common.TestUtilities;

namespace TreeManager.App.L0.ViewModels;

public class OptionalDatePickerViewModelTests
{
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

    [Theory]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    [InlineData(null, true)]
    [InlineData("", true)]
    [InlineData("1", true)]
    [InlineData("31", true)]
    [InlineData("0", false)]
    [InlineData("32", false)]
    [InlineData("abc", false)]
    public void IsDayValid_ReturnsExpected_WhenDayIsSet(string day, bool expected)
    {
        //Arrange
        var vm = new OptionalDatePickerViewModel { Day = day };

        //Act
        //Assert
        Assert.Equal(expected, vm.IsDayValid);
    }

    [Theory]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    [InlineData(null, true)]
    [InlineData("", true)]
    [InlineData("1", true)]
    [InlineData("12", true)]
    [InlineData("0", false)]
    [InlineData("13", false)]
    [InlineData("abc", false)]
    public void IsMonthValid_ReturnsExpected_WhenMonthIsSet(string month, bool expected)
    {
        //Arrange
        var vm = new OptionalDatePickerViewModel { Month = month };

        //Act
        //Assert
        Assert.Equal(expected, vm.IsMonthValid);
    }

    [Theory]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    [InlineData(null, true)]
    [InlineData("", true)]
    [InlineData("1947", true)]
    [InlineData("19--", true)]
    [InlineData("198-", true)]
    [InlineData("--47", true)]
    [InlineData("----", true)]
    [InlineData("abc", false)]
    [InlineData("12345", false)]
    [InlineData("19 4", false)]
    public void IsYearValid_ReturnsExpected_WhenYearIsSet(string year, bool expected)
    {
        //Arrange
        var vm = new OptionalDatePickerViewModel { Year = year };

        //Act
        //Assert
        Assert.Equal(expected, vm.IsYearValid);
    }
}
