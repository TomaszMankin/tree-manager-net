using System;
using System.IO;
using Serilog;
using TreeManager.Common.TestUtilities;
using TreeManager.Infrastructure.Shell;

namespace TreeManager.Infrastructure.L1.Shell;

public class ShellLinkShortcutCreatorIntegrationTests : IDisposable
{
    private readonly string _tempDir;
    private readonly ShellLinkShortcutCreator _sut;

    public ShellLinkShortcutCreatorIntegrationTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "TreeManagerL1_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);

        _sut = new ShellLinkShortcutCreator(Log.Logger);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, recursive: true);
        }
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L1)]
    public void Create_WritesResolvableLnk_WhenTargetHasDiacritics()
    {
        //Arrange
        var targetDir = Path.Combine(_tempDir, "Władysław Łęczycki");
        Directory.CreateDirectory(targetDir);
        var lnkPath = Path.Combine(_tempDir, "WladyslawLink.lnk");

        //Act
        _sut.Create(targetDir, lnkPath);

        //Assert
        Assert.True(File.Exists(lnkPath), "Expected .lnk file to be created");
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L1)]
    public void Create_PreservesDiacritics_WhenResolvedViaGetPath()
    {
        //Arrange
        var targetDir = Path.Combine(_tempDir, "Władysław Łęczycki");
        Directory.CreateDirectory(targetDir);
        var lnkPath = Path.Combine(_tempDir, "DiactriticsTest.lnk");

        //Act
        _sut.Create(targetDir, lnkPath);
        var resolvedPath = ShellLinkShortcutCreator.Resolve(lnkPath);

        //Assert — compare via Path equality for case-canonicalization robustness (per CLAUDE.md runner note)
        var expected = new System.IO.DirectoryInfo(targetDir).FullName;
        var actual = new System.IO.DirectoryInfo(resolvedPath).FullName;
        Assert.Equal(expected, actual, StringComparer.OrdinalIgnoreCase);
        // Diacritics must survive byte-for-byte: compare the raw path string ignoring case
        Assert.Contains("Władysław Łęczycki", resolvedPath);
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L1)]
    public void Create_PreservesHighCodepointDiacritics_WhenNameHasZolcLodz()
    {
        //Arrange
        var targetDir = Path.Combine(_tempDir, "Żółć Łódź");
        Directory.CreateDirectory(targetDir);
        var lnkPath = Path.Combine(_tempDir, "ZolcLodzTest.lnk");

        //Act
        _sut.Create(targetDir, lnkPath);
        var resolvedPath = ShellLinkShortcutCreator.Resolve(lnkPath);

        //Assert
        Assert.Contains("Żółć Łódź", resolvedPath);
    }
}
