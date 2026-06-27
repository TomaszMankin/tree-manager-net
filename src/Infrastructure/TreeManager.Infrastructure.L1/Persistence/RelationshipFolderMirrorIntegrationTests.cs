using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using TreeManager.Common.TestUtilities;
using TreeManager.Core.Domain;
using TreeManager.Core.Services;
using TreeManager.Infrastructure.IO;
using TreeManager.Infrastructure.Persistence;
using TreeManager.Infrastructure.Shell;

namespace TreeManager.Infrastructure.L1.Persistence;

public sealed class RelationshipFolderMirrorIntegrationTests : IDisposable
{
    private const string PeopleListFolder = "Lista osób";

    private readonly string _rootPath;
    private readonly PersonRepository _sut;

    public RelationshipFolderMirrorIntegrationTests()
    {
        _rootPath = Path.Combine(Path.GetTempPath(), "TM_MirrorL1_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(_rootPath, PeopleListFolder));

        var fs = new FileSystemFacade();
        var processor = new MeFileProcessor(fs);
        var shortcutCreator = new ShellLinkShortcutCreator(Serilog.Log.Logger);
        var folderMirror = new RelationshipFolderMirror(fs, shortcutCreator, Serilog.Log.Logger);
        _sut = new PersonRepository(fs, processor, folderMirror, Serilog.Log.Logger);
    }

    #region M-001 parent↔child disk proof

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L1)]
    public void Create_WritesParentChildShortcutsOnDisk_WhenPersonHasParent()
    {
        //Arrange — create person A (parent) first with no relations
        var aId = Guid.NewGuid();
        var aFolder = Path.Combine(_rootPath, PeopleListFolder, "Alicja Testowa");
        Directory.CreateDirectory(aFolder);
        var personA = new MeFile
        {
            UniqueIdentifier = aId,
            PersonName = "Alicja Testowa",
            Location = aFolder,
        };
        File.WriteAllText(
            Path.Combine(aFolder, "me.json"),
            JsonSerializer.Serialize(personA, MeFile.DefaultOptions));

        // person B with A as parent
        var bId = Guid.NewGuid();
        var personB = new MeFile
        {
            UniqueIdentifier = bId,
            PersonName = "Bartosz Testowy",
            Location = Path.Combine(_rootPath, PeopleListFolder, "Bartosz Testowy"),
            ParentsId = new List<Guid> { aId },
            Parents = new List<string> { "Alicja Testowa" },
        };

        //Act
        _sut.Create(personB, _rootPath);

        //Assert — B/Rodzice/Alicja Testowa.lnk exists
        var bFolder = Path.Combine(_rootPath, PeopleListFolder, "Bartosz Testowy");
        var bToAShortcut = Path.Combine(bFolder, "Rodzice", "Alicja Testowa.lnk");
        Assert.True(File.Exists(bToAShortcut), $"Expected shortcut at: {bToAShortcut}");

        //Assert — A/Dzieci/Bartosz Testowy.lnk exists
        var aToBShortcut = Path.Combine(aFolder, "Dzieci", "Bartosz Testowy.lnk");
        Assert.True(File.Exists(aToBShortcut), $"Expected shortcut at: {aToBShortcut}");

        //Assert — me.json arrays updated bidirectionally
        var updatedA = JsonSerializer.Deserialize<MeFile>(
            File.ReadAllText(Path.Combine(aFolder, "me.json")), MeFile.DefaultOptions);
        Assert.Contains(bId, updatedA.ChildrenId);
    }

    #endregion

    #region M-001 spouse↔spouse disk proof

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L1)]
    public void Create_WritesSpouseShortcutsOnDisk_WhenPersonHasSpouse()
    {
        //Arrange — create person A (spouse) first
        var aId = Guid.NewGuid();
        var aFolder = Path.Combine(_rootPath, PeopleListFolder, "Anna Małżeńska");
        Directory.CreateDirectory(aFolder);
        var personA = new MeFile
        {
            UniqueIdentifier = aId,
            PersonName = "Anna Małżeńska",
            Location = aFolder,
        };
        File.WriteAllText(
            Path.Combine(aFolder, "me.json"),
            JsonSerializer.Serialize(personA, MeFile.DefaultOptions));

        var bId = Guid.NewGuid();
        var personB = new MeFile
        {
            UniqueIdentifier = bId,
            PersonName = "Bogdan Małżeński",
            Location = Path.Combine(_rootPath, PeopleListFolder, "Bogdan Małżeński"),
            SpouseId = new List<Guid> { aId },
            Spouse = new List<string> { "Anna Małżeńska" },
        };

        //Act
        _sut.Create(personB, _rootPath);

        //Assert — B/Małżonkowie/Anna Małżeńska.lnk exists
        var bFolder = Path.Combine(_rootPath, PeopleListFolder, "Bogdan Małżeński");
        Assert.True(File.Exists(Path.Combine(bFolder, "Małżonkowie", "Anna Małżeńska.lnk")));

        //Assert — A/Małżonkowie/Bogdan Małżeński.lnk exists
        Assert.True(File.Exists(Path.Combine(aFolder, "Małżonkowie", "Bogdan Małżeński.lnk")));
    }

    #endregion

    public void Dispose()
    {
        if (Directory.Exists(_rootPath))
        {
            Directory.Delete(_rootPath, recursive: true);
        }
    }
}
