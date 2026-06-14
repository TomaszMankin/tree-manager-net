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

public class DraftFlowIntegrationTests : IDisposable
{
    private readonly string _rootPath;
    private readonly FileSystemFacade _fs;
    private readonly MeFileProcessor _processor;
    private readonly PersonRepository _personRepo;
    private readonly DraftRepository _draftRepo;
    private readonly DraftPromoter _sut;

    public DraftFlowIntegrationTests()
    {
        _rootPath = Path.Combine(Path.GetTempPath(), "TreeManagerDraftL1_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(_rootPath, "Lista osób"));

        _fs = new FileSystemFacade();
        _processor = new MeFileProcessor(_fs);
        var shortcutCreator = new ShellLinkShortcutCreator(Serilog.Log.Logger);
        var folderMirror = new RelationshipFolderMirror(_fs, shortcutCreator, Serilog.Log.Logger);
        _personRepo = new PersonRepository(_fs, _processor, folderMirror, Serilog.Log.Logger);
        _draftRepo = new DraftRepository(_fs, _processor, Serilog.Log.Logger);
        _sut = new DraftPromoter(_personRepo, _draftRepo, _fs, Serilog.Log.Logger);
    }

    #region DraftFlow

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L1)]
    public void DraftFlow_PromotesToMainTree_WhenSaveLoadPromote()
    {
        //Arrange
        var relatedId = Guid.NewGuid();
        var relatedMeFile = new MeFile
        {
            UniqueIdentifier = relatedId,
            PersonName = "Anna Nowak",
        };
        var relatedFolder = Path.Combine(_rootPath, "Lista osób", "Anna Nowak");
        Directory.CreateDirectory(relatedFolder);
        File.WriteAllText(
            Path.Combine(relatedFolder, "me.json"),
            JsonSerializer.Serialize(relatedMeFile, MeFile.DefaultOptions));

        var draftId = Guid.NewGuid();
        var draft = new MeFile
        {
            UniqueIdentifier = draftId,
            PersonName = "Jan Kowalski",
            ParentsId = new List<Guid> { relatedId },
            Parents = new List<string> { "Anna Nowak" },
        };

        //Act — save draft
        _draftRepo.SaveDraft(draft, _rootPath);

        //Assert — draft me.json exists under Poczekalnia
        var draftMeJson = Path.Combine(_rootPath, "Poczekalnia", "Jan Kowalski", "me.json");
        Assert.True(File.Exists(draftMeJson), "Draft me.json should exist under Poczekalnia");

        //Act — read draft round-trip
        var loadedDraft = _draftRepo.ReadDraft(_rootPath, "Jan Kowalski");

        //Assert — Guid preserved in round-trip
        Assert.Equal(draftId, loadedDraft.UniqueIdentifier);
        Assert.Equal("Jan Kowalski", loadedDraft.PersonName);

        //Act — promote
        _sut.Promote(loadedDraft, _rootPath);

        //Assert — person now in Lista osób
        var promotedMeJson = Path.Combine(_rootPath, "Lista osób", "Jan Kowalski", "me.json");
        Assert.True(File.Exists(promotedMeJson), "Promoted me.json should exist under Lista osób");

        //Assert — Guid retained after promote
        var promotedMeFile = JsonSerializer.Deserialize<MeFile>(
            File.ReadAllText(promotedMeJson), MeFile.DefaultOptions);
        Assert.Equal(draftId, promotedMeFile.UniqueIdentifier);

        //Assert — draft folder removed
        var draftFolder = Path.Combine(_rootPath, "Poczekalnia", "Jan Kowalski");
        Assert.False(Directory.Exists(draftFolder), "Draft folder should be removed after promote");

        //Assert — bidirectional sync: related person has back-reference
        var relatedMeJson = Path.Combine(_rootPath, "Lista osób", "Anna Nowak", "me.json");
        var updatedRelated = JsonSerializer.Deserialize<MeFile>(
            File.ReadAllText(relatedMeJson), MeFile.DefaultOptions);
        Assert.Contains(draftId, updatedRelated.ChildrenId);
        Assert.Contains("Jan Kowalski", updatedRelated.Children);
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
