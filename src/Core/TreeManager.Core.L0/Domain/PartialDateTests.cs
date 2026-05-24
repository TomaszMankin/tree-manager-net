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
    [InlineData("01|02|2020", 1, 2, 2020)]
    public void ParsePartialDate_ReturnsPartialDate_WhenSerializedStringIsValid(
        string input, int? day, int? month, int? year)
    {
        //Act
        var result = input.ParsePartialDate();

        //Assert
        Assert.Equal(new PartialDate(day, month, year), result);
    }

    [Theory]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    [InlineData("")]
    [InlineData("garbage")]
    [InlineData("01/02/2020")]  // wrong separator
    public void ParsePartialDate_ReturnsDefault_WhenInputIsMalformed(string input)
    {
        //Act
        var result = input.ParsePartialDate();

        //Assert
        Assert.Equal(default(PartialDate), result);
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void ParsePartialDate_Throws_WhenInputIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => ((string)null!).ParsePartialDate());
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
