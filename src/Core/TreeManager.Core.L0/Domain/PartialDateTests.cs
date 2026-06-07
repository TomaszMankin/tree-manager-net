using System;
using TreeManager.Common.TestUtilities;
using TreeManager.Core.Domain;

namespace TreeManager.Core.L0.Domain;

public class PartialDateTests
{
    [Theory]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    [InlineData("12|03|1947", 12, 3, "1947")]
    [InlineData("--|03|1947", null, 3, "1947")]
    [InlineData("--|--|----", null, null, null)]
    [InlineData("12|03|----", 12, 3, null)]
    [InlineData("12|--|1947", 12, null, "1947")]
    public void ToPartialDate_ReturnsExpectedDate_WhenInputIsValid(
        string input, int? expectedDay, int? expectedMonth, string expectedYear)
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
    [InlineData("12|03|--47")]
    [InlineData("12|03|19-7")]
    [InlineData("12|03|198-")]
    public void ToPartialDate_PreservesPartialYear_WhenRoundTripped(string input)
    {
        //Arrange
        //Act
        var result = input.ToPartialDate().ToSerializedString();

        //Assert
        Assert.Equal(input, result);
    }

    [Theory]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    [InlineData(12, 3, "1947", "12|03|1947")]
    [InlineData(null, 3, "1947", "--|03|1947")]
    [InlineData(null, null, null, "--|--|----")]
    [InlineData(1, 2, "2020", "01|02|2020")]
    public void ToSerializedString_ReturnsExpectedString_WhenPartialDateIsValid(
        int? day, int? month, string year, string expected)
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
    [InlineData(12, 3, "1947", "12/03/1947")]
    [InlineData(null, 3, "1947", "--/03/1947")]
    [InlineData(null, null, null, "--/--/----")]
    public void ToDateString_ReturnsSlashFormattedString_WhenPartialDateIsValid(
        int? day, int? month, string year, string expected)
    {
        //Arrange
        var date = new PartialDate(day, month, year);

        //Act
        var result = date.ToDateString();

        //Assert
        Assert.Equal(expected, result);
    }

    #region Qualifier prefix

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void ToSerializedString_PrependsApproxAndBeforePrefix_WhenBothQualifiersSet()
    {
        //Arrange
        var date = new PartialDate(12, 3, "1947") { IsBefore = true, IsApprox = true };

        //Act
        var result = date.ToSerializedString();

        //Assert
        Assert.Equal("~<12|03|1947", result);
    }

    [Theory]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    [InlineData(true, false, "<12|03|1947")]
    [InlineData(false, true, "~12|03|1947")]
    public void ToSerializedString_PrependsExpectedPrefix_WhenSingleQualifierSet(
        bool isBefore, bool isApprox, string expected)
    {
        //Arrange
        var date = new PartialDate(12, 3, "1947") { IsBefore = isBefore, IsApprox = isApprox };

        //Act
        var result = date.ToSerializedString();

        //Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void ToSerializedString_EmitsNoPrefix_WhenNoQualifierSet()
    {
        //Arrange
        var date = new PartialDate(12, 3, "1947");

        //Act
        var result = date.ToSerializedString();

        //Assert
        Assert.Equal("12|03|1947", result);
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void ToPartialDate_SetsBothQualifiers_WhenPrefixIsApproxBefore()
    {
        //Arrange
        const string Input = "~<--|--|1900";

        //Act
        var result = Input.ToPartialDate();

        //Assert
        Assert.True(result.IsApprox);
        Assert.True(result.IsBefore);
        Assert.Null(result.Day);
        Assert.Null(result.Month);
        Assert.Equal("1900", result.Year);
    }

    [Theory]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    [InlineData("<12|03|1947", true, false)]
    [InlineData("~--|04|1950", false, true)]
    public void ToPartialDate_SetsExpectedQualifier_WhenSinglePrefixPresent(
        string input, bool expectedIsBefore, bool expectedIsApprox)
    {
        //Arrange
        //Act
        var result = input.ToPartialDate();

        //Assert
        Assert.Equal(expectedIsBefore, result.IsBefore);
        Assert.Equal(expectedIsApprox, result.IsApprox);
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void ToPartialDate_LeavesBothQualifiersFalse_WhenNoPrefix()
    {
        //Arrange
        const string Input = "12|03|1947";

        //Act
        var result = Input.ToPartialDate();

        //Assert
        Assert.False(result.IsBefore);
        Assert.False(result.IsApprox);
    }

    [Theory]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    [InlineData("~<--|--|1900")]
    [InlineData("<12|03|1947")]
    [InlineData("~--|04|1950")]
    [InlineData("12|03|1947")]
    public void ToPartialDate_RoundTripsQualifiers_WhenReserialized(string input)
    {
        //Arrange
        //Act
        var result = input.ToPartialDate().ToSerializedString();

        //Assert
        Assert.Equal(input, result);
    }

    #endregion

    #region ToPartialDate edge cases

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void ToPartialDate_Throws_WhenInputIsNull()
    {
        string input = null;
        Assert.Throws<ArgumentNullException>(() => input.ToPartialDate());
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void ToPartialDate_ReturnsDefault_WhenChunkCountIsNotThree()
    {
        //Arrange
        const string Input = "12|03";

        //Act
        var result = Input.ToPartialDate();

        //Assert
        Assert.Equal(default(PartialDate), result);
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void ToPartialDate_ReturnsNullYear_WhenAllYearCharsAreWildcard()
    {
        //Arrange
        const string Input = "12|03|----";

        //Act
        var result = Input.ToPartialDate();

        //Assert
        Assert.Null(result.Year);
    }

    [Theory]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    [InlineData("--|03|1947")]
    [InlineData("12|--|1947")]
    public void ToPartialDate_ReturnsNullDayOrMonth_WhenSegmentIsNonNumeric(string input)
    {
        //Arrange + Act
        var result = input.ToPartialDate();

        //Assert
        Assert.True(result.Day == null || result.Month == null);
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void ToPartialDate_PreservesPartialYearString_WhenYearIsPartiallyKnown()
    {
        //Arrange
        const string Input = "12|03|184-";

        //Act
        var result = Input.ToPartialDate();

        //Assert
        Assert.Equal("184-", result.Year);
    }

    #endregion
}
