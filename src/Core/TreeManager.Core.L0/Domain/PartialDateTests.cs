using TreeManager.Common.TestUtilities;
using TreeManager.Core.Domain;

namespace TreeManager.Core.L0.Domain;

public class PartialDateTests
{
    [Theory]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    [InlineData("12|03|1947", 12, 3, 1947)]
    [InlineData("XX|03|1947", null, 3, 1947)]
    [InlineData("XX|XX|XXXX", null, null, null)]
    public void ToPartialDate_ReturnsExpectedDate_WhenInputIsValid(
        string input, int? expectedDay, int? expectedMonth, int? expectedYear)
    {
        //Arrange
        //Act
        var result = input.ToPartialDate();

        //Assert
        Assert.Equal(expectedDay, result.Day);
        Assert.Equal(expectedMonth, result.Month);
        Assert.Equal(expectedYear, result.Year);
    }

    [Theory]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    [InlineData(12, 3, 1947, "12|03|1947")]
    [InlineData(null, 3, 1947, "XX|03|1947")]
    [InlineData(null, null, null, "XX|XX|XXXX")]
    [InlineData(1, 2, 2020, "01|02|2020")]
    public void ToSerializedString_ReturnsExpectedString_WhenPartialDateIsValid(
        int? day, int? month, int? year, string expected)
    {
        //Arrange
        var date = new PartialDate(day, month, year);

        //Act
        var result = date.ToSerializedString();

        //Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    [InlineData(12, 3, 1947, "12/03/1947")]
    [InlineData(null, 3, 1947, "XX/03/1947")]
    [InlineData(null, null, null, "XX/XX/XXXX")]
    public void ToDateString_ReturnsSlashFormattedString_WhenPartialDateIsValid(
        int? day, int? month, int? year, string expected)
    {
        //Arrange
        var date = new PartialDate(day, month, year);

        //Act
        var result = date.ToDateString();

        //Assert
        Assert.Equal(expected, result);
    }
}
