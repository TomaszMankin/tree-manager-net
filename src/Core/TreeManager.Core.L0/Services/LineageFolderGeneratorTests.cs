using System;
using System.Collections.Generic;
using System.Linq;
using Moq;
using Serilog;
using TreeManager.Common.TestUtilities;
using TreeManager.Core.Abstractions.IO;
using TreeManager.Core.Abstractions.Persistence;
using TreeManager.Core.Abstractions.Services;
using TreeManager.Core.Abstractions.Shell;
using TreeManager.Core.Domain;
using TreeManager.Core.Services;

namespace TreeManager.Core.L0.Services;

public class LineageFolderGeneratorTests
{
    private const string FakeRoot = @"C:\fake\root";

    private readonly Mock<IMeFileProcessor> _mockProcessor;
    private readonly Mock<IFolderTreeGenerator> _mockFolderTreeGenerator;
    private readonly Mock<IShortcutCreator> _mockShortcutCreator;
    private readonly Mock<IFileSystemFacade> _mockFs;
    private readonly Mock<ILogger> _mockLog;
    private readonly LineageFolderGenerator _sut;

    public LineageFolderGeneratorTests()
    {
        _mockProcessor = new Mock<IMeFileProcessor>();
        _mockFolderTreeGenerator = new Mock<IFolderTreeGenerator>();
        _mockShortcutCreator = new Mock<IShortcutCreator>();
        _mockFs = new Mock<IFileSystemFacade>();
        _mockLog = new Mock<ILogger>();

        _mockFolderTreeGenerator
            .Setup(g => g.ComputeMembership(It.IsAny<Guid>(), It.IsAny<IReadOnlyDictionary<Guid, MeFile>>()))
            .Returns((new List<FolderTreeMember>(), new List<string>()));

        _mockFs.Setup(f => f.EnumerateFiles(It.IsAny<string>(), It.IsAny<string>())).Returns([]);
        _mockFs.Setup(f => f.EnumerateDirectories(It.IsAny<string>())).Returns([]);

        _sut = new LineageFolderGenerator(
            _mockProcessor.Object,
            _mockFolderTreeGenerator.Object,
            _mockShortcutCreator.Object,
            _mockFs.Object,
            _mockLog.Object);
    }

    #region ComputeLineages

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void ComputeLineages_EscalatesToFullName_WhenTwoContributorsShareSurname()
    {
        //Arrange
        var rootId = Guid.NewGuid();
        var fatherId = Guid.NewGuid();
        var motherId = Guid.NewGuid();

        var root = PersonFixtureFactory.Build(rootId, "Adam", "X", Sex.Male,
            parentIds: [fatherId, motherId]);
        var father = PersonFixtureFactory.Build(fatherId, "Clark", "Smith", Sex.Male,
            childrenIds: [rootId]);
        var mother = PersonFixtureFactory.Build(motherId, "Janice", "Doe", Sex.Female,
            hasMaidenName: true, maidenName: "Smith",
            childrenIds: [rootId]);

        var map = PersonFixtureFactory.BuildMap(root, father, mother);

        //Act
        var (groups, _) = _sut.ComputeLineages(rootId, map);

        //Assert
        Assert.Equal(2, groups.Count);
        Assert.False(groups.ContainsKey("Smith"));
        Assert.True(groups.ContainsKey("Clark Smith"));
        Assert.True(groups.ContainsKey("Janice Doe zd. Smith"));
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void ComputeLineages_ReturnsFourGroups_WhenFourContributorsAllHaveDistinctSurnames()
    {
        //Arrange
        var rootId = Guid.NewGuid();
        var fatherKowalskiId = Guid.NewGuid();
        var motherNowakId = Guid.NewGuid();
        var husbandId = Guid.NewGuid();
        var husbandFatherId = Guid.NewGuid();
        var husbandMotherId = Guid.NewGuid();

        var root = PersonFixtureFactory.Build(rootId, "Anna", "Kowalska", Sex.Female,
            parentIds: [fatherKowalskiId, motherNowakId],
            spouseIds: [husbandId]);
        var fatherKowalski = PersonFixtureFactory.Build(fatherKowalskiId, "Jan", "Kowalski", Sex.Male,
            childrenIds: [rootId]);
        var motherNowak = PersonFixtureFactory.Build(motherNowakId, "Maria", "Nowak", Sex.Female,
            childrenIds: [rootId]);
        var husband = PersonFixtureFactory.Build(husbandId, "Piotr", "Wisniewski", Sex.Male,
            parentIds: [husbandFatherId, husbandMotherId],
            spouseIds: [rootId]);
        var husbandFather = PersonFixtureFactory.Build(husbandFatherId, "Stanislaw", "Wisniewski", Sex.Male,
            childrenIds: [husbandId]);
        var husbandMother = PersonFixtureFactory.Build(husbandMotherId, "Helena", "Zielinska", Sex.Female,
            childrenIds: [husbandId]);

        var map = PersonFixtureFactory.BuildMap(root, fatherKowalski, motherNowak, husband, husbandFather, husbandMother);

        //Act
        var (groups, _) = _sut.ComputeLineages(rootId, map);

        //Assert
        Assert.Equal(4, groups.Count);
        Assert.True(groups.ContainsKey("Kowalski"));
        Assert.True(groups.ContainsKey("Nowak"));
        Assert.True(groups.ContainsKey("Wisniewski"));
        Assert.True(groups.ContainsKey("Zielinska"));
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void ComputeLineages_PlacesWomanInBirthAndMarriedSurnameGroups_WhenMaidenAndMarriedDiffer()
    {
        //Arrange
        var rootId = Guid.NewGuid();
        var husbandId = Guid.NewGuid();
        var womanId = Guid.NewGuid();

        var root = PersonFixtureFactory.Build(rootId, "Tomek", "Nowak", Sex.Male,
            parentIds: [husbandId, womanId]);
        var husband = PersonFixtureFactory.Build(husbandId, "Krzysztof", "Nowak", Sex.Male,
            spouseIds: [womanId],
            childrenIds: [rootId]);
        var woman = PersonFixtureFactory.Build(womanId, "Zofia", "Nowak", Sex.Female,
            spouseIds: [husbandId],
            hasMaidenName: true, maidenName: "Kowalska",
            childrenIds: [rootId]);

        var map = PersonFixtureFactory.BuildMap(root, husband, woman);

        //Act
        var (groups, _) = _sut.ComputeLineages(rootId, map);

        //Assert
        Assert.True(groups.ContainsKey("Nowak"));
        Assert.True(groups.ContainsKey("Kowalska"));
        Assert.Contains(womanId, groups["Nowak"].MemberUids);
        Assert.Contains(womanId, groups["Kowalska"].MemberUids);
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void ComputeLineages_IncludesAncestorWithDifferentSurname_WhenSurnameBreaksMidChain()
    {
        //Arrange
        var rootId = Guid.NewGuid();
        var fatherId = Guid.NewGuid();
        var gfId = Guid.NewGuid();
        var ggfId = Guid.NewGuid();

        var root = PersonFixtureFactory.Build(rootId, "Adam", "Nowicki", Sex.Male,
            parentIds: [fatherId]);
        var father = PersonFixtureFactory.Build(fatherId, "Piotr", "Nowicki", Sex.Male,
            parentIds: [gfId],
            childrenIds: [rootId]);
        var gf = PersonFixtureFactory.Build(gfId, "Heinrich", "Inny", Sex.Male,
            parentIds: [ggfId],
            childrenIds: [fatherId]);
        var ggf = PersonFixtureFactory.Build(ggfId, "Wilhelm", "Nowicki", Sex.Male,
            childrenIds: [gfId]);

        var map = PersonFixtureFactory.BuildMap(root, father, gf, ggf);

        //Act
        var (groups, _) = _sut.ComputeLineages(rootId, map);

        //Assert — 'Nowicki' folder exists and contains both gf (different surname) and ggf
        Assert.True(groups.ContainsKey("Nowicki"));
        var nowickiMembers = groups["Nowicki"].MemberUids;
        Assert.Contains(gfId, nowickiMembers);
        Assert.Contains(ggfId, nowickiMembers);
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void ComputeLineages_AddsSpouseOfAncestorAsLeaf_ButDoesNotWalkTheirParents()
    {
        //Arrange
        var rootId = Guid.NewGuid();
        var fatherId = Guid.NewGuid();
        var gfId = Guid.NewGuid();
        var gmId = Guid.NewGuid();
        var gmFatherId = Guid.NewGuid();

        var root = PersonFixtureFactory.Build(rootId, "Adam", "Nowicki", Sex.Male,
            parentIds: [fatherId]);
        var father = PersonFixtureFactory.Build(fatherId, "Piotr", "Nowicki", Sex.Male,
            parentIds: [gfId],
            childrenIds: [rootId]);
        var gf = PersonFixtureFactory.Build(gfId, "Dziadek", "Nowicki", Sex.Male,
            spouseIds: [gmId],
            childrenIds: [fatherId]);
        var gm = PersonFixtureFactory.Build(gmId, "Babcia", "Nowicki", Sex.Female,
            parentIds: [gmFatherId],
            spouseIds: [gfId]);
        var gmFather = PersonFixtureFactory.Build(gmFatherId, "PraGrandFather", "Other", Sex.Male,
            childrenIds: [gmId]);

        var map = PersonFixtureFactory.BuildMap(root, father, gf, gm, gmFather);

        //Act
        var (groups, _) = _sut.ComputeLineages(rootId, map);

        //Assert
        Assert.True(groups.ContainsKey("Nowicki"));
        var members = groups["Nowicki"].MemberUids;
        Assert.Contains(gmId, members);
        Assert.DoesNotContain(gmFatherId, members);
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void ComputeLineages_IncludesRootAndFullDescendantSubtree_InEveryGroup()
    {
        //Arrange
        var rootId = Guid.NewGuid();
        var fatherId = Guid.NewGuid();
        var childId = Guid.NewGuid();
        var grandchildId = Guid.NewGuid();
        var grandchildSpouseId = Guid.NewGuid();

        var root = PersonFixtureFactory.Build(rootId, "Adam", "Kowalski", Sex.Male,
            parentIds: [fatherId],
            childrenIds: [childId]);
        var father = PersonFixtureFactory.Build(fatherId, "Piotr", "Kowalski", Sex.Male,
            childrenIds: [rootId]);
        var child = PersonFixtureFactory.Build(childId, "Tomek", "Kowalski", Sex.Male,
            childrenIds: [grandchildId]);
        var grandchild = PersonFixtureFactory.Build(grandchildId, "Jasio", "Kowalski", Sex.Male,
            spouseIds: [grandchildSpouseId]);
        var grandchildSpouse = PersonFixtureFactory.Build(grandchildSpouseId, "Kasia", "Nowak", Sex.Female,
            spouseIds: [grandchildId]);

        var map = PersonFixtureFactory.BuildMap(root, father, child, grandchild, grandchildSpouse);

        //Act
        var (groups, _) = _sut.ComputeLineages(rootId, map);

        //Assert
        Assert.True(groups.ContainsKey("Kowalski"));
        var members = groups["Kowalski"].MemberUids;
        Assert.Contains(rootId, members);
        Assert.Contains(childId, members);
        Assert.Contains(grandchildId, members);
        Assert.Contains(grandchildSpouseId, members);
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void ComputeLineages_UsesLastName_WhenMaidenNameIsUnknownSentinel()
    {
        //Arrange
        var rootId = Guid.NewGuid();
        var motherId = Guid.NewGuid();

        var root = PersonFixtureFactory.Build(rootId, "Adam", "X", Sex.Male,
            parentIds: [motherId]);
        var mother = PersonFixtureFactory.Build(motherId, "Janice", "Priest", Sex.Female,
            hasMaidenName: true, maidenName: "(nieznane)",
            childrenIds: [rootId]);

        var map = PersonFixtureFactory.BuildMap(root, mother);

        //Act
        var (groups, _) = _sut.ComputeLineages(rootId, map);

        //Assert
        Assert.True(groups.ContainsKey("Priest"));
        Assert.False(groups.ContainsKey("(nieznane)"));
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void ComputeLineages_UsesFullNameAsFolderKey_WhenBothLastNameAndMaidenNameAreUnknownSentinel()
    {
        //Arrange
        var rootId = Guid.NewGuid();
        var parentId = Guid.NewGuid();

        var root = PersonFixtureFactory.Build(rootId, "Adam", "X", Sex.Male,
            parentIds: [parentId]);
        var parent = PersonFixtureFactory.Build(parentId, "Jan", "(nieznane)", Sex.Male,
            childrenIds: [rootId]);

        var map = PersonFixtureFactory.BuildMap(root, parent);

        //Act
        var (groups, _) = _sut.ComputeLineages(rootId, map);

        //Assert
        Assert.Single(groups);
        Assert.True(groups.ContainsKey("Jan (nieznane)"));
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void ComputeLineages_UsesMaidenNameForSurname_WhenHasMaidenNameTrue()
    {
        //Arrange
        var rootId = Guid.NewGuid();
        var motherId = Guid.NewGuid();

        var root = PersonFixtureFactory.Build(rootId, "Adam", "X", Sex.Male,
            parentIds: [motherId]);
        var mother = PersonFixtureFactory.Build(motherId, "Maria", "Nowak", Sex.Female,
            hasMaidenName: true, maidenName: "Kowalska",
            childrenIds: [rootId]);

        var mapTrue = PersonFixtureFactory.BuildMap(root, mother);

        //Act
        var (groupsTrue, _) = _sut.ComputeLineages(rootId, mapTrue);

        //Assert
        Assert.True(groupsTrue.ContainsKey("Kowalska"));
        Assert.False(groupsTrue.ContainsKey("Nowak"));

        var mother2Id = Guid.NewGuid();
        var root2Id = Guid.NewGuid();
        var root2 = PersonFixtureFactory.Build(root2Id, "Tomek", "X", Sex.Male,
            parentIds: [mother2Id]);
        var mother2 = PersonFixtureFactory.Build(mother2Id, "Maria", "Nowak", Sex.Female,
            hasMaidenName: false, maidenName: "Kowalska",
            childrenIds: [root2Id]);

        var mapFalse = PersonFixtureFactory.BuildMap(root2, mother2);
        var (groupsFalse, _) = _sut.ComputeLineages(root2Id, mapFalse);

        Assert.True(groupsFalse.ContainsKey("Nowak"));
        Assert.False(groupsFalse.ContainsKey("Kowalska"));
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void ComputeLineages_ThrowsTreeIntegrityException_WhenCycleDetected()
    {
        //Arrange
        var rootId = Guid.NewGuid();
        var fatherId = Guid.NewGuid();
        var gfId = Guid.NewGuid();

        var root = PersonFixtureFactory.Build(rootId, "Adam", "X", Sex.Male,
            parentIds: [fatherId]);
        var father = PersonFixtureFactory.Build(fatherId, "Piotr", "Kowalski", Sex.Male,
            parentIds: [gfId],
            childrenIds: [rootId, gfId]);
        var gf = PersonFixtureFactory.Build(gfId, "Dziadek", "Kowalski", Sex.Male,
            parentIds: [fatherId],
            childrenIds: [fatherId]);

        var map = PersonFixtureFactory.BuildMap(root, father, gf);

        //Act + Assert
        Assert.Throws<TreeIntegrityException>(() => _sut.ComputeLineages(rootId, map));
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void ComputeLineages_ThrowsTreeIntegrityException_WhenBidirectionalReferenceIsBroken()
    {
        //Arrange
        var rootId = Guid.NewGuid();
        var fatherId = Guid.NewGuid();

        var root = PersonFixtureFactory.Build(rootId, "Adam", "X", Sex.Male,
            parentIds: [fatherId]);
        var father = PersonFixtureFactory.Build(fatherId, "Piotr", "Kowalski", Sex.Male);

        var map = PersonFixtureFactory.BuildMap(root, father);

        //Act + Assert
        Assert.Throws<TreeIntegrityException>(() => _sut.ComputeLineages(rootId, map));
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void ComputeLineages_ThrowsTreeIntegrityException_WhenReferencedPersonMissingFromMap()
    {
        //Arrange
        var rootId = Guid.NewGuid();
        var fatherId = Guid.NewGuid();
        var missingGfId = Guid.NewGuid();

        var root = PersonFixtureFactory.Build(rootId, "Adam", "X", Sex.Male,
            parentIds: [fatherId]);
        var father = PersonFixtureFactory.Build(fatherId, "Piotr", "Kowalski", Sex.Male,
            parentIds: [missingGfId],
            childrenIds: [rootId]);

        var map = PersonFixtureFactory.BuildMap(root, father);

        //Act + Assert
        Assert.Throws<TreeIntegrityException>(() => _sut.ComputeLineages(rootId, map));
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void ComputeLineages_ReturnsEmpty_WhenRootNotInMap()
    {
        //Arrange
        var missingRootId = Guid.NewGuid();
        var emptyMap = new Dictionary<Guid, MeFile>();

        //Act
        var (groups, log) = _sut.ComputeLineages(missingRootId, emptyMap);

        //Assert
        Assert.Empty(groups);
        Assert.Contains(log, l => l.Contains("ERROR"));
    }

    #endregion

    #region Generate

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void Generate_CreatesSubfolderPerSurnameAndOneShortcutPerMember_WhenCalled()
    {
        //Arrange
        var rootId = Guid.NewGuid();
        var fatherId = Guid.NewGuid();
        var meFilePath = FakeRoot + @"\Lista osób\Adam Kowalski\me.json";
        var fatherFilePath = FakeRoot + @"\Lista osób\Piotr Kowalski\me.json";

        var root = PersonFixtureFactory.Build(rootId, "Adam", "Kowalski", Sex.Male,
            parentIds: [fatherId]);
        root = root with { Location = FakeRoot + @"\Lista osób\Adam Kowalski" };
        var father = PersonFixtureFactory.Build(fatherId, "Piotr", "Kowalski", Sex.Male,
            childrenIds: [rootId]);
        father = father with { Location = FakeRoot + @"\Lista osób\Piotr Kowalski" };

        _mockProcessor.Setup(p => p.ScanMeFiles(FakeRoot)).Returns([meFilePath, fatherFilePath]);
        _mockProcessor.Setup(p => p.ReadMeFile(meFilePath)).Returns(root);
        _mockProcessor.Setup(p => p.ReadMeFile(fatherFilePath)).Returns(father);

        var drzewoMember = new FolderTreeMember(rootId, 0, 0, 1, "self", "M", "Adam Kowalski", root.Location);
        _mockFolderTreeGenerator
            .Setup(g => g.ComputeMembership(rootId, It.IsAny<IReadOnlyDictionary<Guid, MeFile>>()))
            .Returns((new List<FolderTreeMember> { drzewoMember }, new List<string>()));

        var createdDirs = new List<string>();
        _mockFs.Setup(f => f.CreateDirectory(It.IsAny<string>()))
            .Callback<string>(d => createdDirs.Add(d));

        var createdLinks = new List<string>();
        _mockShortcutCreator.Setup(s => s.Create(It.IsAny<string>(), It.IsAny<string>()))
            .Callback<string, string>((_, lnk) => createdLinks.Add(lnk));

        //Act
        var (written, _) = _sut.Generate(FakeRoot, rootId);

        //Assert
        Assert.True(written > 0);
        Assert.Contains(createdDirs, d => d.Contains("Kowalski"));
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void Generate_WipesRodyFolder_WhenOutputExists()
    {
        //Arrange
        var rootId = Guid.NewGuid();
        var meFilePath = FakeRoot + @"\Lista osób\Adam Kowalski\me.json";
        var existingFile = FakeRoot + @"\Rody\Old\old.lnk";
        var existingSubDir = FakeRoot + @"\Rody\Old";

        var root = PersonFixtureFactory.Build(rootId, "Adam", "Kowalski", Sex.Male);
        root = root with { Location = FakeRoot + @"\Lista osób\Adam Kowalski" };

        _mockProcessor.Setup(p => p.ScanMeFiles(FakeRoot)).Returns([meFilePath]);
        _mockProcessor.Setup(p => p.ReadMeFile(meFilePath)).Returns(root);

        _mockFs.Setup(f => f.EnumerateFiles(It.Is<string>(p => p.Contains("Rody") && !p.Contains("Old")), It.IsAny<string>()))
            .Returns([existingFile]);
        _mockFs.Setup(f => f.EnumerateDirectories(It.Is<string>(p => p.Contains("Rody") && !p.Contains("Old"))))
            .Returns([existingSubDir]);
        _mockFs.Setup(f => f.EnumerateFiles(It.Is<string>(p => p.Contains("Old")), It.IsAny<string>()))
            .Returns([]);
        _mockFs.Setup(f => f.EnumerateDirectories(It.Is<string>(p => p.Contains("Old"))))
            .Returns([]);

        //Act
        _sut.Generate(FakeRoot, rootId);

        //Assert
        _mockFs.Verify(f => f.DeleteFile(existingFile), Times.Once());
        _mockFs.Verify(f => f.DeleteDirectory(existingSubDir, true), Times.Once());
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void Generate_UsesDrzewoTokenForFilename_WhenMemberInDrzewoMembership()
    {
        //Arrange
        var rootId = Guid.NewGuid();
        var fatherId = Guid.NewGuid();
        var meFilePath = FakeRoot + @"\Lista osób\Adam Kowalski\me.json";
        var fatherFilePath = FakeRoot + @"\Lista osób\Piotr Kowalski\me.json";

        var root = PersonFixtureFactory.Build(rootId, "Adam", "Kowalski", Sex.Male,
            parentIds: [fatherId]);
        root = root with { Location = FakeRoot + @"\Lista osób\Adam Kowalski" };
        var father = PersonFixtureFactory.Build(fatherId, "Piotr", "Kowalski", Sex.Male,
            childrenIds: [rootId]);
        father = father with { Location = FakeRoot + @"\Lista osób\Piotr Kowalski" };

        _mockProcessor.Setup(p => p.ScanMeFiles(FakeRoot)).Returns([meFilePath, fatherFilePath]);
        _mockProcessor.Setup(p => p.ReadMeFile(meFilePath)).Returns(root);
        _mockProcessor.Setup(p => p.ReadMeFile(fatherFilePath)).Returns(father);

        var drzewoMemberRoot = new FolderTreeMember(rootId, 0, 0, 1, "self", "M", "Adam Kowalski", root.Location);
        var drzewoMemberFather = new FolderTreeMember(fatherId, 1, 0, 1, "ancestor", "M", "Piotr Kowalski", father.Location);
        _mockFolderTreeGenerator
            .Setup(g => g.ComputeMembership(rootId, It.IsAny<IReadOnlyDictionary<Guid, MeFile>>()))
            .Returns((new List<FolderTreeMember> { drzewoMemberRoot, drzewoMemberFather }, new List<string>()));

        var createdLinks = new List<string>();
        _mockShortcutCreator.Setup(s => s.Create(It.IsAny<string>(), It.IsAny<string>()))
            .Callback<string, string>((_, lnk) => createdLinks.Add(lnk));

        //Act
        _sut.Generate(FakeRoot, rootId);

        //Assert
        var expectedFatherToken = "[51][-1][A][M] Piotr Kowalski.lnk";
        Assert.Contains(createdLinks, lnk => lnk.EndsWith(expectedFatherToken));
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void Generate_UsesPlainNameFallback_WhenMemberNotInDrzewoMembership()
    {
        //Arrange
        var rootId = Guid.NewGuid();
        var fatherId = Guid.NewGuid();
        var meFilePath = FakeRoot + @"\Lista osób\Adam Kowalski\me.json";
        var fatherFilePath = FakeRoot + @"\Lista osób\Piotr Kowalski\me.json";

        var root = PersonFixtureFactory.Build(rootId, "Adam", "Kowalski", Sex.Male,
            parentIds: [fatherId]);
        root = root with { Location = FakeRoot + @"\Lista osób\Adam Kowalski" };
        var father = PersonFixtureFactory.Build(fatherId, "Piotr", "Kowalski", Sex.Male,
            childrenIds: [rootId]);
        father = father with { Location = FakeRoot + @"\Lista osób\Piotr Kowalski" };

        _mockProcessor.Setup(p => p.ScanMeFiles(FakeRoot)).Returns([meFilePath, fatherFilePath]);
        _mockProcessor.Setup(p => p.ReadMeFile(meFilePath)).Returns(root);
        _mockProcessor.Setup(p => p.ReadMeFile(fatherFilePath)).Returns(father);

        var drzewoMemberRoot = new FolderTreeMember(rootId, 0, 0, 1, "self", "M", "Adam Kowalski", root.Location);
        _mockFolderTreeGenerator
            .Setup(g => g.ComputeMembership(rootId, It.IsAny<IReadOnlyDictionary<Guid, MeFile>>()))
            .Returns((new List<FolderTreeMember> { drzewoMemberRoot }, new List<string>()));

        var createdLinks = new List<string>();
        _mockShortcutCreator.Setup(s => s.Create(It.IsAny<string>(), It.IsAny<string>()))
            .Callback<string, string>((_, lnk) => createdLinks.Add(lnk));

        //Act
        var (_, log) = _sut.Generate(FakeRoot, rootId);

        //Assert
        Assert.Contains(createdLinks, lnk => lnk.EndsWith("Piotr Kowalski.lnk"));
        Assert.Contains(log, l => l.Contains("DRZEWO_FALLBACK"));
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void Generate_ContinuesAndLogs_WhenOneShortcutCreateThrows()
    {
        //Arrange
        var rootId = Guid.NewGuid();
        var fatherId = Guid.NewGuid();
        var meFilePath = FakeRoot + @"\Lista osób\Adam Kowalski\me.json";
        var fatherFilePath = FakeRoot + @"\Lista osób\Piotr Kowalski\me.json";

        var root = PersonFixtureFactory.Build(rootId, "Adam", "Kowalski", Sex.Male,
            parentIds: [fatherId]);
        root = root with { Location = FakeRoot + @"\Lista osób\Adam Kowalski" };
        var father = PersonFixtureFactory.Build(fatherId, "Piotr", "Kowalski", Sex.Male,
            childrenIds: [rootId]);
        father = father with { Location = FakeRoot + @"\Lista osób\Piotr Kowalski" };

        _mockProcessor.Setup(p => p.ScanMeFiles(FakeRoot)).Returns([meFilePath, fatherFilePath]);
        _mockProcessor.Setup(p => p.ReadMeFile(meFilePath)).Returns(root);
        _mockProcessor.Setup(p => p.ReadMeFile(fatherFilePath)).Returns(father);

        _mockFolderTreeGenerator
            .Setup(g => g.ComputeMembership(rootId, It.IsAny<IReadOnlyDictionary<Guid, MeFile>>()))
            .Returns((new List<FolderTreeMember>(), new List<string>()));

        int callCount = 0;
        _mockShortcutCreator.Setup(s => s.Create(It.IsAny<string>(), It.IsAny<string>()))
            .Callback<string, string>((_, _) =>
            {
                callCount++;
                if (callCount == 1)
                {
                    throw new InvalidOperationException("Simulated shortcut failure");
                }
            });

        //Act
        var (written, log) = _sut.Generate(FakeRoot, rootId);

        //Assert
        Assert.True(written >= 1);
        Assert.Contains(log, l => l.Contains("SHORTCUT_ERROR") || l.Contains("shortcut") || l.Contains("ERROR"));
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void Generate_SkipsMemberAndLogs_WhenLocationMissing()
    {
        //Arrange
        var rootId = Guid.NewGuid();
        var fatherId = Guid.NewGuid();
        var meFilePath = FakeRoot + @"\Lista osób\Adam Kowalski\me.json";
        var fatherFilePath = FakeRoot + @"\Lista osób\Piotr Kowalski\me.json";

        var root = PersonFixtureFactory.Build(rootId, "Adam", "Kowalski", Sex.Male,
            parentIds: [fatherId]);
        root = root with { Location = FakeRoot + @"\Lista osób\Adam Kowalski" };
        var father = PersonFixtureFactory.Build(fatherId, "Piotr", "Kowalski", Sex.Male,
            childrenIds: [rootId]);
        father = father with { Location = string.Empty };

        _mockProcessor.Setup(p => p.ScanMeFiles(FakeRoot)).Returns([meFilePath, fatherFilePath]);
        _mockProcessor.Setup(p => p.ReadMeFile(meFilePath)).Returns(root);
        _mockProcessor.Setup(p => p.ReadMeFile(fatherFilePath)).Returns(father);

        _mockFolderTreeGenerator
            .Setup(g => g.ComputeMembership(rootId, It.IsAny<IReadOnlyDictionary<Guid, MeFile>>()))
            .Returns((new List<FolderTreeMember>(), new List<string>()));

        //Act
        var (written, log) = _sut.Generate(FakeRoot, rootId);

        //Assert
        _mockShortcutCreator.Verify(s => s.Create(string.Empty, It.IsAny<string>()), Times.Never());
        Assert.Contains(log, l => l.Contains("SKIP_MEMBER"));
    }

    #endregion
}
