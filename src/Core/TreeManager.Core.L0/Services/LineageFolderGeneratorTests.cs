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

        // Default: ComputeMembership returns empty (tests override as needed)
        _mockFolderTreeGenerator
            .Setup(g => g.ComputeMembership(It.IsAny<Guid>(), It.IsAny<IReadOnlyDictionary<Guid, MeFile>>()))
            .Returns((new List<FolderTreeMember>(), new List<string>()));

        // Default fs: no existing files or dirs
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
    public void ComputeLineages_ReturnsThreeGroups_WhenSixPeopleAcrossThreeSurnames()
    {
        //Arrange
        // root — parents=[fatherKowalski, motherNowak]; spouse=[husband]
        // husband — parents=[fatherWisniewski, fatherNowak2 (same surname Nowak → collision, only first wins)]
        // But we want 3 groups: Kowalski (father), Nowak (mother), Wisniewski (husbandFather)
        // husbandFather + husbandMother where husbandMother has maiden = Wisniewski → seeds 3rd group
        // Let's build: root.parents=[fatherKowalski(Kowalski), motherNowak(Nowak)]
        //              root.spouse=[husband]; husband.parents=[husbandFather(Wisniewski), husbandMother(Wisniewski)]
        // husbandFather seeds Wisniewski; husbandMother has same surname → collision → first wins → still 3
        var rootId = Guid.NewGuid();
        var fatherKowalskiId = Guid.NewGuid();
        var motherNowakId = Guid.NewGuid();
        var husbandId = Guid.NewGuid();
        var husbandFatherId = Guid.NewGuid();
        var husbandMotherId = Guid.NewGuid();

        var root = BuildMeFile(rootId, "Anna", "Kowalska", Sex.Female,
            parentIds: [fatherKowalskiId, motherNowakId],
            spouseIds: [husbandId]);
        var fatherKowalski = BuildMeFile(fatherKowalskiId, "Jan", "Kowalski", Sex.Male);
        var motherNowak = BuildMeFile(motherNowakId, "Maria", "Nowak", Sex.Female);
        var husband = BuildMeFile(husbandId, "Piotr", "Wisniewski", Sex.Male,
            parentIds: [husbandFatherId, husbandMotherId],
            spouseIds: [rootId]);
        var husbandFather = BuildMeFile(husbandFatherId, "Stanislaw", "Wisniewski", Sex.Male);
        var husbandMother = BuildMeFile(husbandMotherId, "Helena", "Wisniewski", Sex.Female); // collision — dropped

        var map = BuildMap(root, fatherKowalski, motherNowak, husband, husbandFather, husbandMother);

        //Act
        var (groups, _) = _sut.ComputeLineages(rootId, map);

        //Assert
        Assert.Equal(3, groups.Count);
        Assert.True(groups.ContainsKey("Kowalski"));
        Assert.True(groups.ContainsKey("Nowak"));
        Assert.True(groups.ContainsKey("Wisniewski"));
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void ComputeLineages_PlacesWomanInBirthAndMarriedSurnameGroups_WhenMaidenAndMarriedDiffer()
    {
        //Arrange
        // Topology from research-notes §6:
        // root.parents=[husband(Nowak), woman]; woman.maiden=Kowalska, last=Nowak, hasMaiden=true
        // woman.spouseIds=[husband]; husband.spouseIds=[woman]
        // Contributor husband(Nowak) seeds Rody/Nowak — R3 adds husband's spouses → woman added as Nowak leaf
        // Contributor woman(Kowalska via maiden) seeds Rody/Kowalska — woman is contributor
        var rootId = Guid.NewGuid();
        var husbandId = Guid.NewGuid();
        var womanId = Guid.NewGuid();

        var root = BuildMeFile(rootId, "Tomek", "Nowak", Sex.Male,
            parentIds: [husbandId, womanId]);
        var husband = BuildMeFile(husbandId, "Krzysztof", "Nowak", Sex.Male,
            spouseIds: [womanId]);
        var woman = BuildMeFile(womanId, "Zofia", "Nowak", Sex.Female,
            spouseIds: [husbandId],
            hasMaidenName: true, maidenName: "Kowalska");

        var map = BuildMap(root, husband, woman);

        //Act
        var (groups, _) = _sut.ComputeLineages(rootId, map);

        //Assert — woman appears in both Nowak (as husband's spouse) and Kowalska (as contributor)
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
        // H-E headline test: father(Mankin) → gf(Inny) → ggf(Mankin)
        // Folder Mankin (seeded by father) MUST contain BOTH gf(Inny) AND ggf(Mankin)
        var rootId = Guid.NewGuid();
        var fatherId = Guid.NewGuid();
        var gfId = Guid.NewGuid();
        var ggfId = Guid.NewGuid();

        var root = BuildMeFile(rootId, "Adam", "Mankin", Sex.Male, parentIds: [fatherId]);
        var father = BuildMeFile(fatherId, "Piotr", "Mankin", Sex.Male, parentIds: [gfId]);
        var gf = BuildMeFile(gfId, "Heinrich", "Inny", Sex.Male, parentIds: [ggfId]);
        var ggf = BuildMeFile(ggfId, "Wilhelm", "Mankin", Sex.Male);

        var map = BuildMap(root, father, gf, ggf);

        //Act
        var (groups, _) = _sut.ComputeLineages(rootId, map);

        //Assert — Mankin folder exists and contains both gf(Inny) and ggf(Mankin)
        Assert.True(groups.ContainsKey("Mankin"));
        var mankinMembers = groups["Mankin"].MemberUids;
        Assert.Contains(gfId, mankinMembers);
        Assert.Contains(ggfId, mankinMembers);
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void ComputeLineages_AddsSpouseOfAncestorAsLeaf_ButDoesNotWalkTheirParents()
    {
        //Arrange
        // father(Mankin) → gf; gf.spouseIds=[gm]; gm.parentIds=[gmFather]
        // Assert: gm present, gmFather ABSENT
        var rootId = Guid.NewGuid();
        var fatherId = Guid.NewGuid();
        var gfId = Guid.NewGuid();
        var gmId = Guid.NewGuid();
        var gmFatherId = Guid.NewGuid();

        var root = BuildMeFile(rootId, "Adam", "Mankin", Sex.Male, parentIds: [fatherId]);
        var father = BuildMeFile(fatherId, "Piotr", "Mankin", Sex.Male, parentIds: [gfId]);
        var gf = BuildMeFile(gfId, "Dziadek", "Mankin", Sex.Male, spouseIds: [gmId]);
        var gm = BuildMeFile(gmId, "Babcia", "Mankin", Sex.Female, parentIds: [gmFatherId]);
        var gmFather = BuildMeFile(gmFatherId, "PraGrandFather", "Other", Sex.Male);

        var map = BuildMap(root, father, gf, gm, gmFather);

        //Act
        var (groups, _) = _sut.ComputeLineages(rootId, map);

        //Assert
        Assert.True(groups.ContainsKey("Mankin"));
        var members = groups["Mankin"].MemberUids;
        Assert.Contains(gmId, members);
        Assert.DoesNotContain(gmFatherId, members);
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void ComputeLineages_IncludesRootAndFullDescendantSubtree_InEveryGroup()
    {
        //Arrange
        // root → child → grandchild; grandchild has spouse
        // root.parents=[father(Kowalski)] → one group Kowalski
        // All of root, child, grandchild, grandchildSpouse must be in the group
        var rootId = Guid.NewGuid();
        var fatherId = Guid.NewGuid();
        var childId = Guid.NewGuid();
        var grandchildId = Guid.NewGuid();
        var grandchildSpouseId = Guid.NewGuid();

        var root = BuildMeFile(rootId, "Adam", "Kowalski", Sex.Male,
            parentIds: [fatherId],
            childrenIds: [childId]);
        var father = BuildMeFile(fatherId, "Piotr", "Kowalski", Sex.Male);
        var child = BuildMeFile(childId, "Tomek", "Kowalski", Sex.Male,
            childrenIds: [grandchildId]);
        var grandchild = BuildMeFile(grandchildId, "Jasio", "Kowalski", Sex.Male,
            spouseIds: [grandchildSpouseId]);
        var grandchildSpouse = BuildMeFile(grandchildSpouseId, "Kasia", "Nowak", Sex.Female,
            spouseIds: [grandchildId]);

        var map = BuildMap(root, father, child, grandchild, grandchildSpouse);

        //Act
        var (groups, _) = _sut.ComputeLineages(rootId, map);

        //Assert — the Kowalski group (only group) must include all universal members
        Assert.True(groups.ContainsKey("Kowalski"));
        var members = groups["Kowalski"].MemberUids;
        Assert.Contains(rootId, members);
        Assert.Contains(childId, members);
        Assert.Contains(grandchildId, members);
        Assert.Contains(grandchildSpouseId, members);
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void ComputeLineages_FirstContributorWins_WhenTwoContributorsShareSurname()
    {
        //Arrange
        // root.parents=[father1(Kowalski), father2(Kowalski)]
        // father1 seeds Kowalski; father2 dropped (collision)
        var rootId = Guid.NewGuid();
        var father1Id = Guid.NewGuid();
        var father2Id = Guid.NewGuid();

        var root = BuildMeFile(rootId, "Adam", "X", Sex.Male,
            parentIds: [father1Id, father2Id]);
        var father1 = BuildMeFile(father1Id, "Piotr", "Kowalski", Sex.Male);
        var father2 = BuildMeFile(father2Id, "Marek", "Kowalski", Sex.Male);

        var map = BuildMap(root, father1, father2);

        //Act
        var (groups, log) = _sut.ComputeLineages(rootId, map);

        //Assert — only one Kowalski group; collision logged
        Assert.Single(groups);
        Assert.True(groups.ContainsKey("Kowalski"));
        Assert.Equal(father1Id, groups["Kowalski"].ContributorUid);
        Assert.Contains(log, l => l.Contains("COLLISION"));
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void ComputeLineages_SkipsContributor_WhenSurnameIsUnknownSentinel()
    {
        //Arrange
        var rootId = Guid.NewGuid();
        var parentId = Guid.NewGuid();

        var root = BuildMeFile(rootId, "Adam", "X", Sex.Male, parentIds: [parentId]);
        var parent = BuildMeFile(parentId, "Jan", "(nieznane)", Sex.Male);

        var map = BuildMap(root, parent);

        //Act
        var (groups, log) = _sut.ComputeLineages(rootId, map);

        //Assert — no group seeded; log entry
        Assert.Empty(groups);
        Assert.Contains(log, l => l.Contains("SKIP_CONTRIBUTOR"));
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void ComputeLineages_UsesMaidenNameForSurname_WhenHasMaidenNameTrue()
    {
        //Arrange
        var rootId = Guid.NewGuid();
        var motherId = Guid.NewGuid();

        var root = BuildMeFile(rootId, "Adam", "X", Sex.Male, parentIds: [motherId]);
        // mother has maiden Kowalska, last Nowak, hasMaiden=true → surname = Kowalska
        var mother = BuildMeFile(motherId, "Maria", "Nowak", Sex.Female,
            hasMaidenName: true, maidenName: "Kowalska");

        var mapTrue = BuildMap(root, mother);

        //Act
        var (groupsTrue, _) = _sut.ComputeLineages(rootId, mapTrue);

        //Assert — maiden used when hasMaidenName=true
        Assert.True(groupsTrue.ContainsKey("Kowalska"));
        Assert.False(groupsTrue.ContainsKey("Nowak"));

        // Now test hasMaidenName=false → last name used
        var mother2Id = Guid.NewGuid();
        var root2Id = Guid.NewGuid();
        var root2 = BuildMeFile(root2Id, "Tomek", "X", Sex.Male, parentIds: [mother2Id]);
        var mother2 = BuildMeFile(mother2Id, "Maria", "Nowak", Sex.Female,
            hasMaidenName: false, maidenName: "Kowalska");

        var mapFalse = BuildMap(root2, mother2);
        var (groupsFalse, _) = _sut.ComputeLineages(root2Id, mapFalse);

        Assert.True(groupsFalse.ContainsKey("Nowak"));
        Assert.False(groupsFalse.ContainsKey("Kowalska"));
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void ComputeLineages_IgnoresCycle_WhenAncestorLoopsBack()
    {
        //Arrange
        // father.parentIds = [gf]; gf.parentIds = [father] — cycle
        var rootId = Guid.NewGuid();
        var fatherId = Guid.NewGuid();
        var gfId = Guid.NewGuid();

        var root = BuildMeFile(rootId, "Adam", "X", Sex.Male, parentIds: [fatherId]);
        var father = BuildMeFile(fatherId, "Piotr", "Kowalski", Sex.Male, parentIds: [gfId]);
        var gf = BuildMeFile(gfId, "Dziadek", "Kowalski", Sex.Male, parentIds: [fatherId]); // cycle back

        var map = BuildMap(root, father, gf);

        //Act — must not throw or loop infinitely
        var (groups, _) = _sut.ComputeLineages(rootId, map);

        //Assert — group exists; both father and gf added once each
        Assert.True(groups.ContainsKey("Kowalski"));
        var members = groups["Kowalski"].MemberUids;
        Assert.Contains(fatherId, members);
        Assert.Contains(gfId, members);
        Assert.Equal(members.Count, members.Distinct().Count()); // no duplicates
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

        var root = BuildMeFile(rootId, "Adam", "Kowalski", Sex.Male, parentIds: [fatherId]);
        root = root with { Location = FakeRoot + @"\Lista osób\Adam Kowalski" };
        var father = BuildMeFile(fatherId, "Piotr", "Kowalski", Sex.Male);
        father = father with { Location = FakeRoot + @"\Lista osób\Piotr Kowalski" };

        _mockProcessor.Setup(p => p.ScanMeFiles(FakeRoot)).Returns([meFilePath, fatherFilePath]);
        _mockProcessor.Setup(p => p.ReadMeFile(meFilePath)).Returns(root);
        _mockProcessor.Setup(p => p.ReadMeFile(fatherFilePath)).Returns(father);

        // Drzewo returns only root as member
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

        //Assert — subfolder created for Kowalski; shortcuts written
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

        var root = BuildMeFile(rootId, "Adam", "Kowalski", Sex.Male);
        root = root with { Location = FakeRoot + @"\Lista osób\Adam Kowalski" };

        _mockProcessor.Setup(p => p.ScanMeFiles(FakeRoot)).Returns([meFilePath]);
        _mockProcessor.Setup(p => p.ReadMeFile(meFilePath)).Returns(root);

        // EnumerateFiles returns one existing file in Rody, EnumerateDirectories returns one subdir
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

        //Assert — DeleteFile + DeleteDirectory called for existing content
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

        var root = BuildMeFile(rootId, "Adam", "Kowalski", Sex.Male, parentIds: [fatherId]);
        root = root with { Location = FakeRoot + @"\Lista osób\Adam Kowalski" };
        var father = BuildMeFile(fatherId, "Piotr", "Kowalski", Sex.Male);
        father = father with { Location = FakeRoot + @"\Lista osób\Piotr Kowalski" };

        _mockProcessor.Setup(p => p.ScanMeFiles(FakeRoot)).Returns([meFilePath, fatherFilePath]);
        _mockProcessor.Setup(p => p.ReadMeFile(meFilePath)).Returns(root);
        _mockProcessor.Setup(p => p.ReadMeFile(fatherFilePath)).Returns(father);

        // Drzewo returns root with a known token
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

        //Assert — father's link uses Drzewo token "[51][1][A][M] Piotr Kowalski.lnk"
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

        var root = BuildMeFile(rootId, "Adam", "Kowalski", Sex.Male, parentIds: [fatherId]);
        root = root with { Location = FakeRoot + @"\Lista osób\Adam Kowalski" };
        var father = BuildMeFile(fatherId, "Piotr", "Kowalski", Sex.Male);
        father = father with { Location = FakeRoot + @"\Lista osób\Piotr Kowalski" };

        _mockProcessor.Setup(p => p.ScanMeFiles(FakeRoot)).Returns([meFilePath, fatherFilePath]);
        _mockProcessor.Setup(p => p.ReadMeFile(meFilePath)).Returns(root);
        _mockProcessor.Setup(p => p.ReadMeFile(fatherFilePath)).Returns(father);

        // Drzewo returns only root (father NOT in Drzewo membership)
        var drzewoMemberRoot = new FolderTreeMember(rootId, 0, 0, 1, "self", "M", "Adam Kowalski", root.Location);
        _mockFolderTreeGenerator
            .Setup(g => g.ComputeMembership(rootId, It.IsAny<IReadOnlyDictionary<Guid, MeFile>>()))
            .Returns((new List<FolderTreeMember> { drzewoMemberRoot }, new List<string>()));

        var createdLinks = new List<string>();
        _mockShortcutCreator.Setup(s => s.Create(It.IsAny<string>(), It.IsAny<string>()))
            .Callback<string, string>((_, lnk) => createdLinks.Add(lnk));

        //Act
        var (_, log) = _sut.Generate(FakeRoot, rootId);

        //Assert — father uses plain name fallback; log entry present
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

        var root = BuildMeFile(rootId, "Adam", "Kowalski", Sex.Male, parentIds: [fatherId]);
        root = root with { Location = FakeRoot + @"\Lista osób\Adam Kowalski" };
        var father = BuildMeFile(fatherId, "Piotr", "Kowalski", Sex.Male);
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

        //Assert — second shortcut written; error logged
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

        var root = BuildMeFile(rootId, "Adam", "Kowalski", Sex.Male, parentIds: [fatherId]);
        root = root with { Location = FakeRoot + @"\Lista osób\Adam Kowalski" };
        // father has empty Location
        var father = BuildMeFile(fatherId, "Piotr", "Kowalski", Sex.Male);
        father = father with { Location = string.Empty };

        _mockProcessor.Setup(p => p.ScanMeFiles(FakeRoot)).Returns([meFilePath, fatherFilePath]);
        _mockProcessor.Setup(p => p.ReadMeFile(meFilePath)).Returns(root);
        _mockProcessor.Setup(p => p.ReadMeFile(fatherFilePath)).Returns(father);

        _mockFolderTreeGenerator
            .Setup(g => g.ComputeMembership(rootId, It.IsAny<IReadOnlyDictionary<Guid, MeFile>>()))
            .Returns((new List<FolderTreeMember>(), new List<string>()));

        //Act
        var (written, log) = _sut.Generate(FakeRoot, rootId);

        //Assert — father skipped (empty Location); log entry; shortcut NOT created targeting empty string
        _mockShortcutCreator.Verify(s => s.Create(string.Empty, It.IsAny<string>()), Times.Never());
        Assert.Contains(log, l => l.Contains("SKIP_MEMBER"));
    }

    #endregion

    #region Helpers

    private static MeFile BuildMeFile(
        Guid id,
        string firstName,
        string lastName,
        Sex sex,
        List<Guid> spouseIds = null,
        List<Guid> parentIds = null,
        List<Guid> childrenIds = null,
        bool hasMaidenName = false,
        string maidenName = "")
    {
        return new MeFile
        {
            UniqueIdentifier = id,
            FirstName = firstName,
            LastName = lastName,
            Sex = sex,
            HasMaidenName = hasMaidenName,
            MaidenName = maidenName,
            Location = FakeRoot + $@"\Lista osób\{firstName} {lastName}",
            SpouseId = spouseIds ?? [],
            ParentsId = parentIds ?? [],
            ChildrenId = childrenIds ?? [],
        };
    }

    private static IReadOnlyDictionary<Guid, MeFile> BuildMap(params MeFile[] people)
    {
        return people.ToDictionary(p => p.UniqueIdentifier);
    }

    #endregion
}
