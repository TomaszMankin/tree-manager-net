using System.Text.Json;
using Moq;
using TreeManager.Common.TestUtilities;
using TreeManager.Core.Abstractions.IO;
using TreeManager.Core.Domain;
using TreeManager.Infrastructure.Persistence;

namespace TreeManager.Infrastructure.L0.Persistence;

public class MeFileProcessorTests
{
    private const string RootPath = @"C:\fake\root";
    private const string PeopleListPath = @"C:\fake\root\Lista osób";
    private const string AnnaFolder = @"C:\fake\root\Lista osób\Anna Iksińska";
    private const string AnnaMeJson = @"C:\fake\root\Lista osób\Anna Iksińska\me.json";
    private const string BobFolder = @"C:\fake\root\Lista osób\Bob Testowy";
    private const string BobMeJson = @"C:\fake\root\Lista osób\Bob Testowy\me.json";

    private readonly Mock<IFileSystemFacade> _fs;
    private readonly MeFileProcessor _sut;

    public MeFileProcessorTests()
    {
        _fs = new Mock<IFileSystemFacade>();
        _sut = new MeFileProcessor(_fs.Object);
    }

    #region ScanMeFiles

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void ScanMeFiles_ReturnsMeFilePaths_WhenPersonFoldersExist()
    {
        //Arrange
        _fs.Setup(x => x.DirectoryExists(PeopleListPath)).Returns(true);
        _fs.Setup(x => x.EnumerateDirectories(PeopleListPath)).Returns(new[] { AnnaFolder, BobFolder });
        _fs.Setup(x => x.FileExists(AnnaMeJson)).Returns(true);
        _fs.Setup(x => x.FileExists(BobMeJson)).Returns(true);

        //Act
        var result = _sut.ScanMeFiles(RootPath).ToList();

        //Assert
        Assert.Equal(2, result.Count);
        Assert.Contains(AnnaMeJson, result);
        Assert.Contains(BobMeJson, result);
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void ScanMeFiles_ReturnsEmpty_WhenPersonFolderIsEmpty()
    {
        //Arrange
        _fs.Setup(x => x.DirectoryExists(PeopleListPath)).Returns(true);
        _fs.Setup(x => x.EnumerateDirectories(PeopleListPath)).Returns(Array.Empty<string>());

        //Act
        var result = _sut.ScanMeFiles(RootPath).ToList();

        //Assert
        Assert.Empty(result);
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void ScanMeFiles_SkipsFolder_WhenMeJsonAbsentInsideFolder()
    {
        //Arrange
        _fs.Setup(x => x.DirectoryExists(PeopleListPath)).Returns(true);
        _fs.Setup(x => x.EnumerateDirectories(PeopleListPath)).Returns(new[] { AnnaFolder });
        _fs.Setup(x => x.FileExists(AnnaMeJson)).Returns(false);

        //Act
        var result = _sut.ScanMeFiles(RootPath).ToList();

        //Assert
        Assert.Empty(result);
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void ScanMeFiles_Throws_WhenPersonFolderDoesNotExist()
    {
        //Arrange
        _fs.Setup(x => x.DirectoryExists(PeopleListPath)).Returns(false);

        //Act & Assert
        Assert.Throws<DirectoryNotFoundException>(() => _sut.ScanMeFiles(RootPath).ToList());
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void ScanMeFiles_Throws_WhenRootPathIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => _sut.ScanMeFiles(null).ToList());
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void ScanMeFiles_Throws_WhenRootPathIsEmpty()
    {
        Assert.Throws<ArgumentException>(() => _sut.ScanMeFiles(string.Empty).ToList());
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void ScanMeFiles_Throws_WhenRootPathIsWhitespace()
    {
        Assert.Throws<ArgumentException>(() => _sut.ScanMeFiles("   ").ToList());
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void ScanMeFiles_DoesNotRecurseIntoNestedSubfolders_WhenNestedFoldersExists()
    {
        //Arrange
        var nestedFolder = @"C:\fake\root\Lista osób\Anna Iksińska\Children";
        var nestedMeJson = @"C:\fake\root\Lista osób\Anna Iksińska\Children\me.json";

        _fs.Setup(x => x.DirectoryExists(PeopleListPath)).Returns(true);
        _fs.Setup(x => x.EnumerateDirectories(PeopleListPath)).Returns(new[] { AnnaFolder });
        _fs.Setup(x => x.FileExists(AnnaMeJson)).Returns(true);
        _fs.Setup(x => x.EnumerateDirectories(AnnaFolder)).Returns(new[] { nestedFolder });
        _fs.Setup(x => x.FileExists(nestedMeJson)).Returns(true);

        //Act
        var result = _sut.ScanMeFiles(RootPath).ToList();

        //Assert
        Assert.Single(result);
        Assert.Contains(AnnaMeJson, result);
        _fs.Verify(x => x.EnumerateDirectories(PeopleListPath), Times.Once());
        _fs.Verify(x => x.EnumerateDirectories(AnnaFolder), Times.Never());
    }

    #endregion

    #region ReadMeFile

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void ReadMeFile_ReturnsMeFile_WhenContentIsValid()
    {
        //Arrange
        var json = FixtureLoader.Load("me-fixture.json");
        _fs.Setup(x => x.FileExists(AnnaMeJson)).Returns(true);
        _fs.Setup(x => x.ReadAllText(AnnaMeJson)).Returns(json);

        //Act
        var result = _sut.ReadMeFile(AnnaMeJson);

        //Assert
        Assert.NotNull(result);
        Assert.Equal(Guid.Parse("aaaaaaaa-1111-2222-3333-bbbbbbbbbbbb"), result.UniqueIdentifier);
        Assert.Equal("Jan Testowy", result.PersonName);
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void ReadMeFile_Throws_WhenContentIsMalformedJson()
    {
        //Arrange
        _fs.Setup(x => x.FileExists(AnnaMeJson)).Returns(true);
        _fs.Setup(x => x.ReadAllText(AnnaMeJson)).Returns("not-valid-json{{{");

        //Act & Assert
        Assert.Throws<JsonException>(() => _sut.ReadMeFile(AnnaMeJson));
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void ReadMeFile_Throws_WhenFileDoesNotExist()
    {
        //Arrange
        _fs.Setup(x => x.FileExists(AnnaMeJson)).Returns(false);

        //Act & Assert
        Assert.Throws<FileNotFoundException>(() => _sut.ReadMeFile(AnnaMeJson));
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void ReadMeFile_Throws_WhenPathIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => _sut.ReadMeFile(null));
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void ReadMeFile_Throws_WhenPathIsEmpty()
    {
        Assert.Throws<ArgumentException>(() => _sut.ReadMeFile(string.Empty));
    }

    #endregion

    #region WriteMeFile

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void WriteMeFile_WritesSerializedJson_WhenInputsValid()
    {
        //Arrange
        var meFile = new MeFile { PersonName = "Anna Iksińska" };
        var expectedJson = JsonSerializer.Serialize(meFile, MeFile.DefaultOptions);

        //Act
        _sut.WriteMeFile(AnnaMeJson, meFile);

        //Assert
        _fs.Verify(x => x.WriteAllText(AnnaMeJson, expectedJson), Times.Once());
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void WriteMeFile_Throws_WhenPathIsNull()
    {
        //Arrange
        var meFile = new MeFile();

        //Act & Assert
        Assert.Throws<ArgumentNullException>(() => _sut.WriteMeFile(null, meFile));
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void WriteMeFile_Throws_WhenPathIsEmpty()
    {
        //Arrange
        var meFile = new MeFile();

        //Act & Assert
        Assert.Throws<ArgumentException>(() => _sut.WriteMeFile(string.Empty, meFile));
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void WriteMeFile_Throws_WhenMeFileIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => _sut.WriteMeFile(AnnaMeJson, null));
    }

    #endregion

    #region Roundtrip

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void Roundtrip_PreservesAllFields_WhenReadModifyWriteCycleApplied()
    {
        //Arrange
        var json = FixtureLoader.Load("me-fixture.json");
        _fs.Setup(x => x.FileExists(AnnaMeJson)).Returns(true);
        _fs.Setup(x => x.ReadAllText(AnnaMeJson)).Returns(json);

        //Act
        var original = _sut.ReadMeFile(AnnaMeJson);
        var modified = original with { PersonName = "Modified" };
        _sut.WriteMeFile(AnnaMeJson, modified);

        //Assert
        var expectedJson = JsonSerializer.Serialize(modified, MeFile.DefaultOptions);
        _fs.Verify(x => x.WriteAllText(AnnaMeJson, expectedJson), Times.Once());
    }

    #endregion
}
