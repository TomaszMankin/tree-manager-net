using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Serilog;
using TreeManager.Common.TestUtilities;
using TreeManager.Core.Domain;
using TreeManager.Core.Services;
using TreeManager.Infrastructure.IO;
using TreeManager.Infrastructure.Persistence;
using TreeManager.Infrastructure.Shell;

namespace TreeManager.Infrastructure.L1.Lineage;

/// <summary>End-to-end integration test: real filesystem + real ShellLinkShortcutCreator + real LineageFolderGenerator.</summary>
public class LineageFolderGeneratorIntegrationTests : IDisposable
{
    private const string ListaOsobFolder = "Lista osób";
    private const string RodyFolder = "Rody";

    private readonly string _tempRoot;
    private readonly LineageFolderGenerator _sut;

    public LineageFolderGeneratorIntegrationTests()
    {
        _tempRoot = Path.Combine(Path.GetTempPath(), "TreeManagerL1Lineage_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempRoot);

        var fs = new FileSystemFacade();
        var logger = Log.Logger;
        var processor = new MeFileProcessor(fs);
        var shortcutCreator = new ShellLinkShortcutCreator(logger);
        var folderTreeGenerator = new FolderTreeGenerator(processor, shortcutCreator, fs, logger);

        _sut = new LineageFolderGenerator(processor, folderTreeGenerator, shortcutCreator, fs, logger);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempRoot))
        {
            Directory.Delete(_tempRoot, recursive: true);
        }
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L1)]
    public void Generate_BuildsExpectedRodyStructure_WhenEndToEndFixture()
    {
        //Arrange
        var rootId = Guid.NewGuid();
        var fatherId = Guid.NewGuid();
        var motherId = Guid.NewGuid();
        var childId = Guid.NewGuid();

        WriteMeFile(rootId, "Adam", "Nowicki", Sex.Male,
            parentIds: [fatherId, motherId],
            childrenIds: [childId]);
        WriteMeFile(fatherId, "Władysław", "Łęczyński", Sex.Male,
            childrenIds: [rootId]);
        WriteMeFile(motherId, "Maria", "Nowicki", Sex.Female,
            childrenIds: [rootId]);
        WriteMeFile(childId, "Tomek", "Nowicki", Sex.Male);

        //Act
        var (written, log) = _sut.Generate(_tempRoot, rootId);

        //Assert — 'Rody/' exists with two subfolders
        var rodyPath = Path.Combine(_tempRoot, RodyFolder);
        Assert.True(Directory.Exists(rodyPath), "Rody folder should exist");

        var subFolders = Directory.GetDirectories(rodyPath)
            .Select(Path.GetFileName)
            .OrderBy(n => n)
            .ToList();

        Assert.Contains("Łęczyński", subFolders);
        Assert.Contains("Nowicki", subFolders);

        // Each folder must contain at least one .lnk file
        var leczynFolder = Path.Combine(rodyPath, "Łęczyński");
        var nowickiFolder = Path.Combine(rodyPath, "Nowicki");

        var leczynLinks = Directory.GetFiles(leczynFolder, "*.lnk");
        var nowickiLinks = Directory.GetFiles(nowickiFolder, "*.lnk");

        Assert.NotEmpty(leczynLinks);
        Assert.NotEmpty(nowickiLinks);

        // Root (Adam Nowicki) must appear in BOTH folders as a universal member
        Assert.Contains(leczynLinks, lnk => Path.GetFileName(lnk).Contains("Adam Nowicki"));
        Assert.Contains(nowickiLinks, lnk => Path.GetFileName(lnk).Contains("Adam Nowicki"));

        // Father (Władysław Łęczyński) must be in 'Łęczyński' folder (contributor)
        Assert.Contains(leczynLinks, lnk => Path.GetFileName(lnk).Contains("Łęczyński"));

        // Total written > 0
        Assert.True(written > 0, $"Expected written > 0, log: {string.Join("; ", log)}");
    }

    #region Helpers

    private void WriteMeFile(
        Guid id,
        string firstName,
        string lastName,
        Sex sex,
        List<Guid> parentIds = null,
        List<Guid> childrenIds = null,
        List<Guid> spouseIds = null)
    {
        var folderName = $"{firstName} {lastName}";
        var personFolder = Path.Combine(_tempRoot, ListaOsobFolder, folderName);
        Directory.CreateDirectory(personFolder);

        var meFile = new MeFile
        {
            UniqueIdentifier = id,
            PersonName = folderName,
            Location = personFolder,
            FirstName = firstName,
            LastName = lastName,
            Sex = sex,
            ParentsId = parentIds ?? [],
            ChildrenId = childrenIds ?? [],
            SpouseId = spouseIds ?? [],
        };

        var json = JsonSerializer.Serialize(meFile, MeFile.DefaultOptions);
        File.WriteAllText(Path.Combine(personFolder, "me.json"), json, System.Text.Encoding.UTF8);
    }

    #endregion
}
