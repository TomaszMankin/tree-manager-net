using TreeManager.Common.TestUtilities;
using TreeManager.Core.Domain.Constants;

namespace TreeManager.Core.L0.Domain.Constants;

public class ForbiddenFoldersTests
{
    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void Default_ContainsAllFourForbiddenFolderNames_WhenLoaded()
    {
        //Assert
        Assert.Equal(4, ForbiddenFolders.Default.Count);
        Assert.Contains("Pozostałe nieuporządkowane", ForbiddenFolders.Default);
        Assert.Contains("Rutowscy - dane ogólne", ForbiddenFolders.Default);
        Assert.Contains("Do ustalenia", ForbiddenFolders.Default);
        Assert.Contains("Wspólne", ForbiddenFolders.Default);
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void Default_IsOrdinalCaseSensitive_WhenComparingPolishDiacritics()
    {
        //Assert — unaccented form and uppercase are not in the set
        Assert.DoesNotContain("Pozostale nieuporządkowane", ForbiddenFolders.Default);
        Assert.DoesNotContain("POZOSTAŁE NIEUPORZĄDKOWANE", ForbiddenFolders.Default);
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void PeopleListFolderName_EqualsListaOsobLiteral_WhenAccessed()
    {
        //Assert — length 10, preserves ó (U+00F3)
        Assert.Equal("Lista osób", ForbiddenFolders.PeopleListFolderName);
        Assert.Equal(10, ForbiddenFolders.PeopleListFolderName.Length);
        Assert.Equal('ó', ForbiddenFolders.PeopleListFolderName[^2]);
    }
}
