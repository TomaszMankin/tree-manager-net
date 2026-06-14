using System;
using System.Collections.Generic;
using Moq;
using Serilog;
using TreeManager.Common.TestUtilities;
using TreeManager.Core.Abstractions.IO;
using TreeManager.Core.Abstractions.Shell;
using TreeManager.Core.Domain;
using TreeManager.Core.Services;

namespace TreeManager.Core.L0.Services;

public class RelationshipFolderMirrorTests
{
    private const string PersonFolder = @"C:\root\Lista osób\Jan Kowalski";
    private const string RelatedFolder = @"C:\root\Lista osób\Anna Nowak";

    private static readonly Guid PersonId = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001");
    private static readonly Guid RelatedId = Guid.Parse("bbbbbbbb-0000-0000-0000-000000000002");

    private readonly Mock<IFileSystemFacade> _mockFs;
    private readonly Mock<IShortcutCreator> _mockShortcut;
    private readonly Mock<ILogger> _mockLog;
    private readonly RelationshipFolderMirror _sut;

    public RelationshipFolderMirrorTests()
    {
        _mockFs = new Mock<IFileSystemFacade>();
        _mockShortcut = new Mock<IShortcutCreator>();
        _mockLog = new Mock<ILogger>();
        _sut = new RelationshipFolderMirror(_mockFs.Object, _mockShortcut.Object, _mockLog.Object);
    }

    #region Mirror — subfolders

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void Mirror_CreatesAllFourSubfolders_Always()
    {
        //Arrange
        var person = BuildPerson();
        var folderIndex = new Dictionary<Guid, string>();

        //Act
        _sut.Mirror(person, PersonFolder, folderIndex);

        //Assert
        _mockFs.Verify(x => x.CreateDirectory(PersonFolder + @"\Rodzice"), Times.Once());
        _mockFs.Verify(x => x.CreateDirectory(PersonFolder + @"\Dzieci"), Times.Once());
        _mockFs.Verify(x => x.CreateDirectory(PersonFolder + @"\Małżonkowie"), Times.Once());
        _mockFs.Verify(x => x.CreateDirectory(PersonFolder + @"\Rodzeństwo"), Times.Once());
    }

    #endregion

    #region Mirror — parent shortcut pair

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void Mirror_CreatesPersonToParentShortcutInRodzice_WhenParentExists()
    {
        //Arrange
        var person = BuildPersonWithParent(RelatedId);
        var folderIndex = BuildFolderIndex(RelatedId, RelatedFolder);

        //Act
        _sut.Mirror(person, PersonFolder, folderIndex);

        //Assert — person/Rodzice/Anna Nowak.lnk → RelatedFolder
        _mockShortcut.Verify(
            x => x.Create(RelatedFolder, PersonFolder + @"\Rodzice\Anna Nowak.lnk"),
            Times.Once());
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void Mirror_CreatesParentToDzieciShortcut_WhenParentExists()
    {
        //Arrange
        var person = BuildPersonWithParent(RelatedId);
        var folderIndex = BuildFolderIndex(RelatedId, RelatedFolder);

        //Act
        _sut.Mirror(person, PersonFolder, folderIndex);

        //Assert — parent/Dzieci/Jan Kowalski.lnk → PersonFolder
        _mockShortcut.Verify(
            x => x.Create(PersonFolder, RelatedFolder + @"\Dzieci\Jan Kowalski.lnk"),
            Times.Once());
    }

    #endregion

    #region Mirror — spouse shortcut pair

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void Mirror_CreatesSpouseShortcutsInBothMalzonkowie_WhenSpouseExists()
    {
        //Arrange
        var person = BuildPersonWithSpouse(RelatedId);
        var folderIndex = BuildFolderIndex(RelatedId, RelatedFolder);

        //Act
        _sut.Mirror(person, PersonFolder, folderIndex);

        //Assert — person/Małżonkowie/Anna Nowak.lnk → RelatedFolder
        _mockShortcut.Verify(
            x => x.Create(RelatedFolder, PersonFolder + @"\Małżonkowie\Anna Nowak.lnk"),
            Times.Once());

        //Assert — related/Małżonkowie/Jan Kowalski.lnk → PersonFolder
        _mockShortcut.Verify(
            x => x.Create(PersonFolder, RelatedFolder + @"\Małżonkowie\Jan Kowalski.lnk"),
            Times.Once());
    }

    #endregion

    #region Mirror — sibling shortcut pair

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void Mirror_CreatesSiblingShortcutsInBothRodzenstwo_WhenSiblingExists()
    {
        //Arrange
        var person = BuildPersonWithSibling(RelatedId);
        var folderIndex = BuildFolderIndex(RelatedId, RelatedFolder);

        //Act
        _sut.Mirror(person, PersonFolder, folderIndex);

        //Assert — person/Rodzeństwo/Anna Nowak.lnk → RelatedFolder
        _mockShortcut.Verify(
            x => x.Create(RelatedFolder, PersonFolder + @"\Rodzeństwo\Anna Nowak.lnk"),
            Times.Once());

        //Assert — related/Rodzeństwo/Jan Kowalski.lnk → PersonFolder
        _mockShortcut.Verify(
            x => x.Create(PersonFolder, RelatedFolder + @"\Rodzeństwo\Jan Kowalski.lnk"),
            Times.Once());
    }

    #endregion

    #region Mirror — missing related folder

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void Mirror_SkipsAndLogsError_WhenRelatedFolderPathAbsent()
    {
        //Arrange
        var person = BuildPersonWithParent(RelatedId);
        var folderIndex = new Dictionary<Guid, string>(); // related not in index

        //Act
        _sut.Mirror(person, PersonFolder, folderIndex);

        //Assert — no shortcuts created; error logged
        _mockShortcut.Verify(x => x.Create(It.IsAny<string>(), It.IsAny<string>()), Times.Never());
        _mockLog.Verify(
            x => x.Error(It.IsAny<string>(), It.IsAny<Guid>()),
            Times.Once());
    }

    #endregion

    #region Helpers

    private static MeFile BuildPerson()
    {
        return new MeFile { UniqueIdentifier = PersonId, PersonName = "Jan Kowalski" };
    }

    private static MeFile BuildPersonWithParent(Guid parentId)
    {
        return new MeFile
        {
            UniqueIdentifier = PersonId,
            PersonName = "Jan Kowalski",
            ParentsId = new List<Guid> { parentId },
        };
    }

    private static MeFile BuildPersonWithSpouse(Guid spouseId)
    {
        return new MeFile
        {
            UniqueIdentifier = PersonId,
            PersonName = "Jan Kowalski",
            SpouseId = new List<Guid> { spouseId },
        };
    }

    private static MeFile BuildPersonWithSibling(Guid siblingId)
    {
        return new MeFile
        {
            UniqueIdentifier = PersonId,
            PersonName = "Jan Kowalski",
            SiblingsId = new List<Guid> { siblingId },
        };
    }

    private static IReadOnlyDictionary<Guid, string> BuildFolderIndex(Guid uid, string folderPath)
    {
        return new Dictionary<Guid, string> { [uid] = folderPath };
    }

    #endregion
}
