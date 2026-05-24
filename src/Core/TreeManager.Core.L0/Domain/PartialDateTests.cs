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
    public void Parse_ValidSerialized_ReturnsExpected(string input, int? day, int? month, int? year)
    {
        var result = PartialDateExtensions.Parse(input);
        Assert.Equal(new PartialDate(day, month, year), result);
    }

    [Theory]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    [InlineData("")]
    [InlineData("garbage")]
    [InlineData("01/02/2020")]  // wrong separator
    public void Parse_MalformedInput_ReturnsDefault(string input)
    {
        var result = PartialDateExtensions.Parse(input);
        Assert.Equal(default(PartialDate), result);
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void Parse_Null_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => PartialDateExtensions.Parse(null!));
    }

    [Theory]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    [InlineData(12, 3, 1947, "12|03|1947")]
    [InlineData(null, 3, 1947, "XX|03|1947")]
    [InlineData(null, null, null, "XX|XX|XXXX")]
    [InlineData(1, 2, 2020, "01|02|2020")]
    public void ToSerializedString_Roundtrips(int? day, int? month, int? year, string expected)
    {
        var date = new PartialDate(day, month, year);
        Assert.Equal(expected, date.ToSerializedString());
    }

    [Theory]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    [InlineData(12, 3, 1947, "12/03/1947")]
    [InlineData(null, 3, 1947, "XX/03/1947")]
    [InlineData(null, null, null, "XX/XX/XXXX")]
    public void ToDateString_FormatsWithSlashSeparator(int? day, int? month, int? year, string expected)
    {
        var date = new PartialDate(day, month, year);
        Assert.Equal(expected, date.ToDateString());
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void Parse_ThenToSerializedString_RoundtripsAllKnownCases()
    {
        var cases = new[] { "12|03|1947", "XX|03|1947", "XX|XX|XXXX", "01|02|2020" };
        foreach (var input in cases)
        {
            var parsed = PartialDateExtensions.Parse(input);
            var serialized = parsed.ToSerializedString();
            Assert.Equal(input, serialized);
        }
    }
}
