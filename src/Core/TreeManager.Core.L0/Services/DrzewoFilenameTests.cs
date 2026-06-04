using System;
using TreeManager.Common.TestUtilities;
using TreeManager.Core.Domain;
using TreeManager.Core.Services;

namespace TreeManager.Core.L0.Services;

public class DrzewoFilenameTests
{
    #region CoupleCode

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void CoupleCode_ReturnsA_WhenIndexZeroAndFiveCouples()
    {
        //Arrange + Act
        var result = DrzewoNaming.CoupleCode(0, 5);

        //Assert
        Assert.Equal("A", result);
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void CoupleCode_ReturnsAA_WhenTwentySevenCouples()
    {
        //Arrange + Act
        var result = DrzewoNaming.CoupleCode(0, 27);

        //Assert
        Assert.Equal("AA", result);
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void CoupleCode_ReturnsBA_WhenIndex26AndTwentySevenCouples()
    {
        //Arrange + Act
        var result = DrzewoNaming.CoupleCode(26, 27);

        //Assert
        Assert.Equal("BA", result);
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void CoupleCode_ReturnsAAA_WhenSixHundredSeventySevenCouples()
    {
        //Arrange + Act
        var result = DrzewoNaming.CoupleCode(0, 677);

        //Assert
        Assert.Equal("AAA", result);
    }

    #endregion

    #region FullName

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void FullName_AppendsZdMaiden_WhenHasMaidenName()
    {
        //Arrange
        var meFile = new MeFile
        {
            FirstName = "Anna",
            LastName = "Kowalska",
            HasMaidenName = true,
            MaidenName = "Nowak"
        };

        //Act
        var result = DrzewoNaming.FullName(meFile);

        //Assert
        Assert.Equal("Anna Kowalska zd. Nowak", result);
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void FullName_UsesUnknownSentinel_WhenFirstNameEmpty()
    {
        //Arrange
        var meFile = new MeFile
        {
            FirstName = string.Empty,
            LastName = "Kowalski"
        };

        //Act
        var result = DrzewoNaming.FullName(meFile);

        //Assert
        Assert.Contains("(nieznane)", result);
    }

    #endregion

    #region RenderFilename

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void RenderFilename_OmitsCoupleBracket_WhenGenerationZero()
    {
        //Arrange
        var member = BuildMember(gen: 0, coupleIndex: 0, total: 1, gender: "M", fullName: "Jan Kowalski");

        //Act
        var result = DrzewoNaming.RenderFilename(member);

        //Assert
        Assert.Equal("[50][0][M] Jan Kowalski.lnk", result);
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void RenderFilename_SignFlipsAndSetsSortKey_WhenAncestorGenPlusOne()
    {
        //Arrange
        var member = BuildMember(gen: 1, coupleIndex: 0, total: 1, gender: "M", fullName: "Piotr Kowalski");

        //Act
        var result = DrzewoNaming.RenderFilename(member);

        //Assert
        Assert.Equal("[51][-1][A][M] Piotr Kowalski.lnk", result);
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void RenderFilename_SignFlipsAndSetsSortKey_WhenDescendantGenMinusOne()
    {
        //Arrange
        var member = BuildMember(gen: -1, coupleIndex: 0, total: 1, gender: "F", fullName: "Kasia Nowak");

        //Act
        var result = DrzewoNaming.RenderFilename(member);

        //Assert
        Assert.Equal("[49][1][A][F] Kasia Nowak.lnk", result);
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void RenderFilename_DefaultsGenderToM_WhenGenderUnknown()
    {
        //Arrange
        var member = BuildMember(gen: 0, coupleIndex: 0, total: 1, gender: string.Empty, fullName: "Jan Kowalski");

        //Act
        var result = DrzewoNaming.RenderFilename(member);

        //Assert
        Assert.Contains("[M]", result);
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void RenderFilename_SanitizesForbiddenChars_WhenNameHasColon()
    {
        //Arrange
        var member = BuildMember(gen: 0, coupleIndex: 0, total: 1, gender: "M", fullName: "Jan:Kowalski");

        //Act
        var result = DrzewoNaming.RenderFilename(member);

        //Assert
        Assert.Contains("Jan_Kowalski", result);
        Assert.DoesNotContain("Jan:Kowalski", result);
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void RenderFilename_KeepsDiacritics_WhenNameHasLeczycki()
    {
        //Arrange
        var member = BuildMember(gen: 0, coupleIndex: 0, total: 1, gender: "M", fullName: "Władysław Łęczycki");

        //Act
        var result = DrzewoNaming.RenderFilename(member);

        //Assert
        Assert.Contains("Władysław Łęczycki", result);
    }

    #endregion

    private static FolderTreeMember BuildMember(int gen, int coupleIndex, int total, string gender, string fullName)
    {
        return new FolderTreeMember(
            Uid: Guid.NewGuid(),
            Generation: gen,
            CoupleIndex: coupleIndex,
            TotalCouplesInGeneration: total,
            Role: "ancestor",
            Gender: gender,
            FullName: fullName,
            TargetLocation: @"C:\fake\location");
    }
}
