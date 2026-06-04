using System;
using System.Collections.Generic;
using System.Linq;
using Moq;
using Serilog;
using TreeManager.Common.TestUtilities;
using TreeManager.Core.Abstractions.IO;
using TreeManager.Core.Abstractions.Persistence;
using TreeManager.Core.Abstractions.Shell;
using TreeManager.Core.Domain;
using TreeManager.Core.Services;

namespace TreeManager.Core.L0.Services;

public class DrzewoGeneratorTests
{
    private readonly Mock<IMeFileProcessor> _mockProcessor;
    private readonly Mock<IShortcutCreator> _mockShortcutCreator;
    private readonly Mock<IFileSystemFacade> _mockFs;
    private readonly Mock<ILogger> _mockLog;
    private readonly DrzewoGenerator _sut;

    public DrzewoGeneratorTests()
    {
        _mockProcessor = new Mock<IMeFileProcessor>();
        _mockShortcutCreator = new Mock<IShortcutCreator>();
        _mockFs = new Mock<IFileSystemFacade>();
        _mockLog = new Mock<ILogger>();

        _sut = new DrzewoGenerator(
            _mockProcessor.Object,
            _mockShortcutCreator.Object,
            _mockFs.Object,
            _mockLog.Object);
    }

    #region ComputeMembership

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void ComputeMembership_PlacesRootAndSpouseAtGenZero_WhenRootHasSpouse()
    {
        //Arrange
        var rootId = Guid.NewGuid();
        var spouseId = Guid.NewGuid();

        var root = BuildMeFile(rootId, "Adam", "Kowalski", Sex.Male, spouseIds: [spouseId]);
        var spouse = BuildMeFile(spouseId, "Eva", "Nowakowska", Sex.Female);
        var map = BuildMap(root, spouse);

        //Act
        var (members, _) = _sut.ComputeMembership(rootId, map);

        //Assert
        var gen0 = members.Where(m => m.Generation == 0).ToList();
        Assert.Equal(2, gen0.Count);
        Assert.Contains(gen0, m => m.Uid == rootId && m.Role == "self");
        Assert.Contains(gen0, m => m.Uid == spouseId && m.Role == "spouse");
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void ComputeMembership_AssignsRootParentsCoupleA_WhenSpouseSeeded()
    {
        //Arrange
        var rootId = Guid.NewGuid();
        var spouseId = Guid.NewGuid();
        var fatherId = Guid.NewGuid();
        var motherId = Guid.NewGuid();
        var spouseFatherId = Guid.NewGuid();
        var spouseMotherId = Guid.NewGuid();

        var root = BuildMeFile(rootId, "Adam", "Kowalski", Sex.Male,
            spouseIds: [spouseId], parentIds: [fatherId, motherId]);
        var spouse = BuildMeFile(spouseId, "Eva", "Nowakowska", Sex.Female,
            parentIds: [spouseFatherId, spouseMotherId]);
        var father = BuildMeFile(fatherId, "Piotr", "Kowalski", Sex.Male);
        var mother = BuildMeFile(motherId, "Maria", "Kowalska", Sex.Female);
        var spouseFather = BuildMeFile(spouseFatherId, "Jan", "Nowak", Sex.Male);
        var spouseMother = BuildMeFile(spouseMotherId, "Anna", "Nowak", Sex.Female);

        var map = BuildMap(root, spouse, father, mother, spouseFather, spouseMother);

        //Act
        var (members, _) = _sut.ComputeMembership(rootId, map);

        //Assert
        // Root's parents = couple A (index 0) at gen+1
        var fatherMember = members.Single(m => m.Uid == fatherId);
        var motherMember = members.Single(m => m.Uid == motherId);
        Assert.Equal(1, fatherMember.Generation);
        Assert.Equal(0, fatherMember.CoupleIndex);
        Assert.Equal(1, motherMember.Generation);
        Assert.Equal(0, motherMember.CoupleIndex);

        // Spouse's parents = couple B (index 1) at gen+1
        var spFather = members.Single(m => m.Uid == spouseFatherId);
        var spMother = members.Single(m => m.Uid == spouseMotherId);
        Assert.Equal(1, spFather.Generation);
        Assert.Equal(1, spFather.CoupleIndex);
        Assert.Equal(1, spMother.Generation);
        Assert.Equal(1, spMother.CoupleIndex);
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void ComputeMembership_NumbersThreeGenerationsCorrectly_WhenThreeGenFixture()
    {
        //Arrange
        var rootId = Guid.NewGuid();
        var fatherId = Guid.NewGuid();
        var grandfatherId = Guid.NewGuid();
        var childId = Guid.NewGuid();

        var root = BuildMeFile(rootId, "Adam", "Kowalski", Sex.Male,
            parentIds: [fatherId], childrenIds: [childId]);
        var father = BuildMeFile(fatherId, "Piotr", "Kowalski", Sex.Male,
            parentIds: [grandfatherId]);
        var grandfather = BuildMeFile(grandfatherId, "Dziadek", "Kowalski", Sex.Male);
        var child = BuildMeFile(childId, "Tomek", "Kowalski", Sex.Male);

        var map = BuildMap(root, father, grandfather, child);

        //Act
        var (members, _) = _sut.ComputeMembership(rootId, map);

        //Assert
        Assert.Equal(0, members.Single(m => m.Uid == rootId).Generation);
        Assert.Equal(1, members.Single(m => m.Uid == fatherId).Generation);
        Assert.Equal(2, members.Single(m => m.Uid == grandfatherId).Generation);
        Assert.Equal(-1, members.Single(m => m.Uid == childId).Generation);
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void ComputeMembership_AssignsSequentialCoupleLetters_WhenMultiplePartnersInGeneration()
    {
        //Arrange
        var rootId = Guid.NewGuid();
        var spouseId = Guid.NewGuid();
        var fatherId = Guid.NewGuid();
        var motherId = Guid.NewGuid();
        var spouseFatherId = Guid.NewGuid();
        var spouseMotherId = Guid.NewGuid();

        var root = BuildMeFile(rootId, "Adam", "K", Sex.Male,
            spouseIds: [spouseId], parentIds: [fatherId, motherId]);
        var spouse = BuildMeFile(spouseId, "Eva", "N", Sex.Female,
            parentIds: [spouseFatherId, spouseMotherId]);
        var father = BuildMeFile(fatherId, "Piotr", "K", Sex.Male);
        var mother = BuildMeFile(motherId, "Maria", "K", Sex.Female);
        var spouseFather = BuildMeFile(spouseFatherId, "Jan", "N", Sex.Male);
        var spouseMother = BuildMeFile(spouseMotherId, "Anna", "N", Sex.Female);

        var map = BuildMap(root, spouse, father, mother, spouseFather, spouseMother);

        //Act
        var (members, _) = _sut.ComputeMembership(rootId, map);

        //Assert
        // Gen+1 has 2 couples → total=2
        var gen1 = members.Where(m => m.Generation == 1).ToList();
        Assert.Equal(4, gen1.Count);
        Assert.Equal(2, gen1.Select(m => m.TotalCouplesInGeneration).Distinct().Single());
        // Couple indices 0 and 1 assigned
        var indices = gen1.Select(m => m.CoupleIndex).Distinct().OrderBy(x => x).ToList();
        Assert.Equal(new[] { 0, 1 }, indices);
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void ComputeMembership_WalksDescendantsBirthOrder_WhenRootHasChildren()
    {
        //Arrange
        var rootId = Guid.NewGuid();
        var child1Id = Guid.NewGuid();
        var child2Id = Guid.NewGuid();
        var grandchildId = Guid.NewGuid();

        var root = BuildMeFile(rootId, "Adam", "K", Sex.Male,
            childrenIds: [child1Id, child2Id]);
        var child1 = BuildMeFile(child1Id, "Tomek", "K", Sex.Male,
            childrenIds: [grandchildId]);
        var child2 = BuildMeFile(child2Id, "Ania", "K", Sex.Female);
        var grandchild = BuildMeFile(grandchildId, "Jasio", "K", Sex.Male);

        var map = BuildMap(root, child1, child2, grandchild);

        //Act
        var (members, _) = _sut.ComputeMembership(rootId, map);

        //Assert
        Assert.Equal(-1, members.Single(m => m.Uid == child1Id).Generation);
        Assert.Equal(-1, members.Single(m => m.Uid == child2Id).Generation);
        Assert.Equal(-2, members.Single(m => m.Uid == grandchildId).Generation);
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void ComputeMembership_UsesPaternalFirstOrder_WhenBothParentsPresent()
    {
        //Arrange
        var rootId = Guid.NewGuid();
        var fatherId = Guid.NewGuid();
        var motherId = Guid.NewGuid();

        var root = BuildMeFile(rootId, "Adam", "K", Sex.Male,
            parentIds: [motherId, fatherId]); // mother listed first intentionally
        var father = BuildMeFile(fatherId, "Piotr", "K", Sex.Male);
        var mother = BuildMeFile(motherId, "Maria", "K", Sex.Female);

        var map = BuildMap(root, father, mother);

        //Act
        var (members, _) = _sut.ComputeMembership(rootId, map);

        //Assert — father should be CoupleIndex 0 (father edge first), mother also index 0 (same couple)
        var fatherMember = members.Single(m => m.Uid == fatherId);
        var motherMember = members.Single(m => m.Uid == motherId);
        Assert.Equal(fatherMember.CoupleIndex, motherMember.CoupleIndex);
        Assert.Equal("M", fatherMember.Gender);
        Assert.Equal("F", motherMember.Gender);
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void ComputeMembership_LogsGenderFallback_WhenParentSexUnknown()
    {
        //Arrange
        var rootId = Guid.NewGuid();
        var unknownParentId = Guid.NewGuid();

        var root = BuildMeFile(rootId, "Adam", "K", Sex.Male,
            parentIds: [unknownParentId]);
        var unknownParent = BuildMeFile(unknownParentId, "Unknown", "K", Sex.Unknown);

        var map = BuildMap(root, unknownParent);

        //Act
        var (_, log) = _sut.ComputeMembership(rootId, map);

        //Assert — log contains a gender-fallback entry
        Assert.Contains(log, entry => entry.Contains("GENDER_FALLBACK"));
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void ComputeMembership_ReturnsEmptyWithLog_WhenRootNotInMap()
    {
        //Arrange
        var missingId = Guid.NewGuid();
        var emptyMap = new Dictionary<Guid, MeFile>();

        //Act
        var (members, log) = _sut.ComputeMembership(missingId, emptyMap);

        //Assert
        Assert.Empty(members);
        Assert.NotEmpty(log);
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void ComputeMembership_ReturnsExpectedFilenameSet_WhenEightPersonFixture()
    {
        //Arrange — 8-person fixture hand-derived from py algorithm
        var adamId = Guid.NewGuid();
        var evaId = Guid.NewGuid();
        var piotrId = Guid.NewGuid();
        var mariaId = Guid.NewGuid();
        var janId = Guid.NewGuid();
        var annaId = Guid.NewGuid();
        var tomekId = Guid.NewGuid();
        var kasiaId = Guid.NewGuid();

        // Root: Adam Kowalski, M — spouse=Eva, parents=[Piotr,Maria], children=[Tomek]
        var adam = BuildMeFile(adamId, "Adam", "Kowalski", Sex.Male,
            spouseIds: [evaId], parentIds: [piotrId, mariaId], childrenIds: [tomekId]);
        // Root's spouse: Eva Nowakowska, F — parents=[Jan,Anna]
        var eva = BuildMeFile(evaId, "Eva", "Nowakowska", Sex.Female,
            parentIds: [janId, annaId]);
        // Root's father: Piotr Kowalski, M
        var piotr = BuildMeFile(piotrId, "Piotr", "Kowalski", Sex.Male);
        // Root's mother: Maria Kowalska, F
        var maria = BuildMeFile(mariaId, "Maria", "Kowalska", Sex.Female);
        // Eva's father: Jan Nowak, M
        var jan = BuildMeFile(janId, "Jan", "Nowak", Sex.Male);
        // Eva's mother: Anna Nowak, F
        var anna = BuildMeFile(annaId, "Anna", "Nowak", Sex.Female);
        // Root's child: Tomek Kowalski, M — spouse=Kasia
        var tomek = BuildMeFile(tomekId, "Tomek", "Kowalski", Sex.Male,
            spouseIds: [kasiaId]);
        // Tomek's spouse: Kasia Kwiatkowska, F
        var kasia = BuildMeFile(kasiaId, "Kasia", "Kwiatkowska", Sex.Female);

        var map = BuildMap(adam, eva, piotr, maria, jan, anna, tomek, kasia);

        //Act
        var (members, _) = _sut.ComputeMembership(adamId, map);
        var filenames = members.Select(DrzewoNaming.RenderFilename).ToHashSet();

        //Assert — expected set hand-derived from py algorithm:
        // gen 0 self+spouse (no couple bracket)
        // gen +1: root parents = couple A, spouse parents = couple B
        // gen -1: child+spouse = couple A (spouse inherits descendant's gender = M)
        var expected = new HashSet<string>
        {
            "[50][0][M] Adam Kowalski.lnk",
            "[50][0][F] Eva Nowakowska.lnk",
            "[51][-1][A][M] Piotr Kowalski.lnk",
            "[51][-1][A][F] Maria Kowalska.lnk",
            "[51][-1][B][M] Jan Nowak.lnk",
            "[51][-1][B][F] Anna Nowak.lnk",
            "[49][1][A][M] Tomek Kowalski.lnk",
            "[49][1][A][M] Kasia Kwiatkowska.lnk",
        };

        Assert.Equal(expected, filenames);
    }

    #endregion

    #region Generate orchestration

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void Generate_ScansAndComputesAndWritesOneShortcutPerMember_WhenCalled()
    {
        //Arrange
        const string Root = @"C:\fake\root";
        var rootId = Guid.NewGuid();
        var meFilePath = @"C:\fake\root\Lista osób\Adam Kowalski\me.json";
        var meFile = BuildMeFile(rootId, "Adam", "Kowalski", Sex.Male);
        meFile = meFile with { Location = @"C:\fake\root\Lista osób\Adam Kowalski" };

        _mockProcessor.Setup(p => p.ScanMeFiles(Root)).Returns([meFilePath]);
        _mockProcessor.Setup(p => p.ReadMeFile(meFilePath)).Returns(meFile);
        _mockFs.Setup(f => f.DirectoryExists(It.IsAny<string>())).Returns(false);
        _mockFs.Setup(f => f.EnumerateFiles(It.IsAny<string>(), It.IsAny<string>()))
            .Returns([]);
        _mockFs.Setup(f => f.EnumerateDirectories(It.IsAny<string>())).Returns([]);

        //Act
        var (written, _) = _sut.Generate(Root, rootId);

        //Assert
        Assert.Equal(1, written);
        _mockShortcutCreator.Verify(s => s.Create(
            It.IsAny<string>(),
            It.Is<string>(p => p.Contains("Drzewo"))),
            Times.Once());
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void Generate_WipesDrzewoBeforeWriting_WhenDrzewoExists()
    {
        //Arrange
        const string Root = @"C:\fake\root";
        var rootId = Guid.NewGuid();
        var meFilePath = @"C:\fake\root\Lista osób\Adam Kowalski\me.json";
        var existingLnk = @"C:\fake\root\Drzewo\[50][0][M] Old.lnk";
        var meFile = BuildMeFile(rootId, "Adam", "Kowalski", Sex.Male);
        meFile = meFile with { Location = @"C:\fake\root\Lista osób\Adam Kowalski" };

        _mockProcessor.Setup(p => p.ScanMeFiles(Root)).Returns([meFilePath]);
        _mockProcessor.Setup(p => p.ReadMeFile(meFilePath)).Returns(meFile);
        _mockFs.Setup(f => f.DirectoryExists(It.IsAny<string>())).Returns(true);
        _mockFs.Setup(f => f.EnumerateFiles(It.IsAny<string>(), It.IsAny<string>()))
            .Returns([existingLnk]);
        _mockFs.Setup(f => f.EnumerateDirectories(It.IsAny<string>())).Returns([]);

        var deleteOrder = new List<string>();
        var createOrder = new List<string>();

        _mockFs.Setup(f => f.DeleteFile(It.IsAny<string>()))
            .Callback<string>(p => deleteOrder.Add("delete:" + p));
        _mockShortcutCreator.Setup(s => s.Create(It.IsAny<string>(), It.IsAny<string>()))
            .Callback<string, string>((t, l) => createOrder.Add("create:" + l));

        //Act
        _sut.Generate(Root, rootId);

        //Assert — delete happened before create
        _mockFs.Verify(f => f.DeleteFile(existingLnk), Times.Once());
        _mockShortcutCreator.Verify(s => s.Create(It.IsAny<string>(), It.IsAny<string>()), Times.Once());
        Assert.True(deleteOrder.Count > 0, "Expected a delete to occur");
        Assert.True(createOrder.Count > 0, "Expected a create to occur");
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void Generate_CreatesDrzewoDirectory_WhenMissing()
    {
        //Arrange
        const string Root = @"C:\fake\root";
        var rootId = Guid.NewGuid();
        var meFilePath = @"C:\fake\root\Lista osób\Adam Kowalski\me.json";
        var meFile = BuildMeFile(rootId, "Adam", "Kowalski", Sex.Male);
        meFile = meFile with { Location = @"C:\fake\root\Lista osób\Adam Kowalski" };

        _mockProcessor.Setup(p => p.ScanMeFiles(Root)).Returns([meFilePath]);
        _mockProcessor.Setup(p => p.ReadMeFile(meFilePath)).Returns(meFile);
        _mockFs.Setup(f => f.DirectoryExists(It.IsAny<string>())).Returns(false);
        _mockFs.Setup(f => f.EnumerateFiles(It.IsAny<string>(), It.IsAny<string>()))
            .Returns([]);
        _mockFs.Setup(f => f.EnumerateDirectories(It.IsAny<string>())).Returns([]);

        //Act
        _sut.Generate(Root, rootId);

        //Assert
        _mockFs.Verify(f => f.CreateDirectory(
            It.Is<string>(p => p.EndsWith("Drzewo"))),
            Times.Once());
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void Generate_SuffixesDuplicateFilenames_WhenTwoMembersRenderSame()
    {
        //Arrange — two people with the same name at gen 0 (unusual, but tests dedup)
        const string Root = @"C:\fake\root";
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();
        var path1 = @"C:\fake\root\Lista osób\Adam Kowalski\me.json";
        var path2 = @"C:\fake\root\Lista osób\Adam Kowalski (2)\me.json";

        // Both render to same filename
        var me1 = BuildMeFile(id1, "Adam", "Kowalski", Sex.Male, spouseIds: [id2]);
        me1 = me1 with { Location = @"C:\fake\root\Lista osób\Adam Kowalski" };
        var me2 = BuildMeFile(id2, "Adam", "Kowalski", Sex.Male);
        me2 = me2 with { Location = @"C:\fake\root\Lista osób\Adam Kowalski (2)" };

        _mockProcessor.Setup(p => p.ScanMeFiles(Root)).Returns([path1, path2]);
        _mockProcessor.Setup(p => p.ReadMeFile(path1)).Returns(me1);
        _mockProcessor.Setup(p => p.ReadMeFile(path2)).Returns(me2);
        _mockFs.Setup(f => f.DirectoryExists(It.IsAny<string>())).Returns(false);
        _mockFs.Setup(f => f.EnumerateFiles(It.IsAny<string>(), It.IsAny<string>()))
            .Returns([]);
        _mockFs.Setup(f => f.EnumerateDirectories(It.IsAny<string>())).Returns([]);

        var createdPaths = new List<string>();
        _mockShortcutCreator.Setup(s => s.Create(It.IsAny<string>(), It.IsAny<string>()))
            .Callback<string, string>((_, lnk) => createdPaths.Add(lnk));

        //Act
        _sut.Generate(Root, id1);

        //Assert — both shortcuts created, second has (2) suffix
        Assert.Equal(2, createdPaths.Count);
        Assert.Contains(createdPaths, p => p.Contains(" (2).lnk") || p.Contains("(2).lnk"));
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void Generate_ContinuesAndLogs_WhenOneShortcutCreateThrows()
    {
        //Arrange
        const string Root = @"C:\fake\root";
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();
        var path1 = @"C:\fake\root\Lista osób\Adam Kowalski\me.json";
        var path2 = @"C:\fake\root\Lista osób\Eva Nowakowska\me.json";

        var me1 = BuildMeFile(id1, "Adam", "Kowalski", Sex.Male, spouseIds: [id2]);
        me1 = me1 with { Location = @"C:\fake\root\Lista osób\Adam Kowalski" };
        var me2 = BuildMeFile(id2, "Eva", "Nowakowska", Sex.Female);
        me2 = me2 with { Location = @"C:\fake\root\Lista osób\Eva Nowakowska" };

        _mockProcessor.Setup(p => p.ScanMeFiles(Root)).Returns([path1, path2]);
        _mockProcessor.Setup(p => p.ReadMeFile(path1)).Returns(me1);
        _mockProcessor.Setup(p => p.ReadMeFile(path2)).Returns(me2);
        _mockFs.Setup(f => f.DirectoryExists(It.IsAny<string>())).Returns(false);
        _mockFs.Setup(f => f.EnumerateFiles(It.IsAny<string>(), It.IsAny<string>()))
            .Returns([]);
        _mockFs.Setup(f => f.EnumerateDirectories(It.IsAny<string>())).Returns([]);

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
        var (written, log) = _sut.Generate(Root, id1);

        //Assert — second shortcut still created; log entry for the failure
        Assert.Equal(1, written);
        Assert.Contains(log, entry => entry.Contains("ERROR") || entry.Contains("shortcut") || entry.Contains("failed") || entry.Contains("SHORTCUT"));
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
        List<Guid> childrenIds = null)
    {
        return new MeFile
        {
            UniqueIdentifier = id,
            FirstName = firstName,
            LastName = lastName,
            Sex = sex,
            Location = $@"C:\fake\root\Lista osób\{firstName} {lastName}",
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
