using System.Collections.Generic;
using TreeManager.Common.TestUtilities;
using TreeManager.Core.Domain;
using TreeManager.Core.Services;

namespace TreeManager.Core.L0.Services;

public class FolderTreeNamingTests
{
    #region Sanitize

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void Sanitize_ReturnsNull_WhenInputIsNull()
    {
        //Arrange
        string input = null;

        //Act
        var result = FolderTreeNaming.Sanitize(input);

        //Assert
        Assert.Null(result);
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void Sanitize_ReturnsEmpty_WhenInputIsEmpty()
    {
        //Arrange + Act
        var result = FolderTreeNaming.Sanitize(string.Empty);

        //Assert
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void Sanitize_ReturnsUnchanged_WhenNameHasNoForbiddenChars()
    {
        //Arrange
        const string Input = "Jan Kowalski";

        //Act
        var result = FolderTreeNaming.Sanitize(Input);

        //Assert
        Assert.Equal(Input, result);
    }

    [Theory]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    [InlineData('"')]
    [InlineData('<')]
    [InlineData('>')]
    [InlineData('|')]
    [InlineData(':')]
    [InlineData('*')]
    [InlineData('?')]
    [InlineData('\\')]
    [InlineData('/')]
    public void Sanitize_ReplacesChar_WhenCharIsForbidden(char forbidden)
    {
        //Arrange
        var input = $"before{forbidden}after";

        //Act
        var result = FolderTreeNaming.Sanitize(input);

        //Assert
        Assert.Equal("before_after", result);
    }

    #endregion

    #region Deduplicate

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void Deduplicate_ReturnsOriginal_WhenFilenameNotInSeen()
    {
        //Arrange
        var seen = new HashSet<string>();
        const string Filename = "Jan Kowalski.lnk";

        //Act
        var result = FolderTreeNaming.Deduplicate(Filename, seen);

        //Assert
        Assert.Equal(Filename, result);
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void Deduplicate_AppendsTwoSuffix_WhenFilenameAlreadySeen()
    {
        //Arrange
        const string Filename = "Jan Kowalski.lnk";
        var seen = new HashSet<string> { Filename };

        //Act
        var result = FolderTreeNaming.Deduplicate(Filename, seen);

        //Assert
        Assert.Equal("Jan Kowalski (2).lnk", result);
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void Deduplicate_AppendsThreeSuffix_WhenBothOriginalAndTwoAlreadySeen()
    {
        //Arrange
        const string Filename = "Jan Kowalski.lnk";
        var seen = new HashSet<string>
        {
            Filename,
            "Jan Kowalski (2).lnk"
        };

        //Act
        var result = FolderTreeNaming.Deduplicate(Filename, seen);

        //Assert
        Assert.Equal("Jan Kowalski (3).lnk", result);
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void Deduplicate_StripsLnkExtension_BeforeAppendingSuffix()
    {
        //Arrange
        const string Filename = "Jan Kowalski.lnk";
        var seen = new HashSet<string> { Filename };

        //Act
        var result = FolderTreeNaming.Deduplicate(Filename, seen);

        //Assert
        Assert.EndsWith(".lnk", result);
        Assert.DoesNotContain(".lnk (2)", result);
    }

    #endregion

    #region GenderToken

    [Theory]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    [InlineData(Sex.Male, "M")]
    [InlineData(Sex.Female, "F")]
    [InlineData(Sex.Unknown, "")]
    public void GenderToken_ReturnsExpectedToken_WhenSexIsKnown(Sex sex, string expected)
    {
        //Arrange + Act
        var result = FolderTreeNaming.GenderToken(sex);

        //Assert
        Assert.Equal(expected, result);
    }

    #endregion

    #region CoupleCode

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void CoupleCode_ReturnsA_WhenTotalCouplesIsZero()
    {
        //Arrange + Act
        var result = FolderTreeNaming.CoupleCode(0, 0);

        //Assert
        Assert.Equal("A", result);
    }

    [Theory]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    [InlineData(0, 1, "A")]
    [InlineData(25, 26, "Z")]
    [InlineData(0, 27, "AA")]
    [InlineData(0, 677, "AAA")]
    public void CoupleCode_ReturnsCorrectCode_WhenIndexAndTotalAreValid(int index, int total, string expected)
    {
        //Arrange + Act
        var result = FolderTreeNaming.CoupleCode(index, total);

        //Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void CoupleCode_ReturnsWiderCode_WhenTotalExceedsFourDigitBoundary()
    {
        //Arrange
        const int Total = 500000;

        //Act
        var result = FolderTreeNaming.CoupleCode(0, Total);

        //Assert
        Assert.True(result.Length >= 5);
    }

    #endregion
}
