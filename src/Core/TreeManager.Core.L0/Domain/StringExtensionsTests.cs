using TreeManager.Common.TestUtilities;
using TreeManager.Core.Domain;

namespace TreeManager.Core.L0.Domain;

public class StringExtensionsTests
{
    [Theory]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    [InlineData("12|03|1947", 12, 3, "1947")]
    [InlineData("XX|03|1947", null, 3, "1947")]
    [InlineData("XX|XX|XXXX", null, null, null)]
    [InlineData("01|02|2020", 1, 2, "2020")]
    public void ParsePartialDate_ReturnsPartialDate_WhenSerializedStringIsValid(
        string input, int? day, int? month, string year)
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
    [InlineData("01/02/2020")]
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
        string input = null;
        Assert.Throws<ArgumentNullException>(() => input.ParsePartialDate());
    }
}
