using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Moq;
using TreeManager.Common.TestUtilities;
using TreeManager.Core.Abstractions.IO;
using TreeManager.Core.Abstractions.Shell;
using TreeManager.Core.Domain;
using TreeManager.UiTesting.Graph;
using TreeManager.UiTesting.Reading;

namespace TreeManager.UiTesting.L0.Reading;

public sealed class DiskTreeGraphReaderTests
{
    private const string Root = @"C:\TmRoot";
    private const string PeopleListFolder = "Lista osób";
    private const string PersonFolder = "Jan Kowalski";
    private const string ParentFolder = "Anna Nowak";

    private readonly Mock<IFileSystemFacade> _fs = new Mock<IFileSystemFacade>(MockBehavior.Strict);
    private readonly Mock<IShortcutCreator> _shortcutCreator = new Mock<IShortcutCreator>(MockBehavior.Strict);
    private readonly DiskTreeGraphReader _sut;

    public DiskTreeGraphReaderTests()
    {
        _sut = new DiskTreeGraphReader(_fs.Object, _shortcutCreator.Object);
    }

    #region Read

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void Read_ReturnsEmptyGraph_WhenPeopleListFolderMissing()
    {
        //Arrange
        var peopleList = Path.Combine(Root, PeopleListFolder);
        _fs.Setup(x => x.DirectoryExists(peopleList)).Returns(false);

        //Act
        var graph = _sut.Read(Root);

        //Assert
        Assert.Empty(graph.Nodes);
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void Read_PopulatesRelationshipReferences_WhenMeFileHasParentId()
    {
        //Arrange
        var personUid = Guid.NewGuid();
        var parentUid = Guid.NewGuid();
        var personFolderPath = Path.Combine(Root, PeopleListFolder, PersonFolder);
        SetupSinglePersonFolder(personFolderPath, BuildMeFile(personUid, parents: new[] { parentUid }));
        SetupNoLinks(personFolderPath);

        //Act
        var graph = _sut.Read(Root);

        //Assert
        var node = graph.Nodes.Single();
        Assert.Equal(personUid, node.UniqueIdentifier);
        Assert.Contains(parentUid, node.ParentIds);
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void Read_ResolvesLinkTargetToFolderName_WhenSubfolderHasLnk()
    {
        //Arrange
        var personUid = Guid.NewGuid();
        var personFolderPath = Path.Combine(Root, PeopleListFolder, PersonFolder);
        SetupSinglePersonFolder(personFolderPath, BuildMeFile(personUid));

        var parentsSubfolder = Path.Combine(personFolderPath, RelationshipSubfolders.Parents);
        var lnkPath = Path.Combine(parentsSubfolder, ParentFolder + ".lnk");
        var resolvedTarget = Path.Combine(Root, PeopleListFolder, ParentFolder);

        SetupSubfolderWithLink(personFolderPath, RelationshipSubfolders.Parents, lnkPath);
        SetupEmptySubfolder(personFolderPath, RelationshipSubfolders.Children);
        SetupEmptySubfolder(personFolderPath, RelationshipSubfolders.Spouses);
        SetupEmptySubfolder(personFolderPath, RelationshipSubfolders.Siblings);
        _shortcutCreator.Setup(x => x.Resolve(lnkPath)).Returns(resolvedTarget);

        //Act
        var graph = _sut.Read(Root);

        //Assert
        var node = graph.Nodes.Single();
        Assert.Contains(new LinkEdge(RelationshipSubfolders.Parents, ParentFolder), node.LinkEdges);
    }

    #endregion

    private void SetupSinglePersonFolder(string personFolderPath, MeFile meFile)
    {
        var peopleList = Path.Combine(Root, PeopleListFolder);
        _fs.Setup(x => x.DirectoryExists(peopleList)).Returns(true);
        _fs.Setup(x => x.EnumerateDirectories(peopleList)).Returns(new[] { personFolderPath });

        var meFilePath = Path.Combine(personFolderPath, "me.json");
        _fs.Setup(x => x.FileExists(meFilePath)).Returns(true);
        _fs.Setup(x => x.ReadAllText(meFilePath)).Returns(JsonSerializer.Serialize(meFile, MeFile.DefaultOptions));
    }

    private void SetupNoLinks(string personFolderPath)
    {
        foreach (var subfolder in RelationshipSubfolders.All)
        {
            SetupEmptySubfolder(personFolderPath, subfolder);
        }
    }

    private void SetupEmptySubfolder(string personFolderPath, string subfolder)
    {
        var subfolderPath = Path.Combine(personFolderPath, subfolder);
        _fs.Setup(x => x.DirectoryExists(subfolderPath)).Returns(true);
        _fs.Setup(x => x.EnumerateFiles(subfolderPath, "*.lnk")).Returns(Array.Empty<string>());
    }

    private void SetupSubfolderWithLink(string personFolderPath, string subfolder, string lnkPath)
    {
        var subfolderPath = Path.Combine(personFolderPath, subfolder);
        _fs.Setup(x => x.DirectoryExists(subfolderPath)).Returns(true);
        _fs.Setup(x => x.EnumerateFiles(subfolderPath, "*.lnk")).Returns(new[] { lnkPath });
    }

    private static MeFile BuildMeFile(Guid uid, IReadOnlyList<Guid> parents = null)
    {
        return new MeFile
        {
            UniqueIdentifier = uid,
            ParentsId = parents == null ? new List<Guid>() : parents.ToList(),
        };
    }
}
