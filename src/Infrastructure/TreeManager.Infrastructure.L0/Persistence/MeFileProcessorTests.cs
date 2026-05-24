using System.Text.Json;
using NSubstitute;
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

    private static readonly IReadOnlySet<string> EmptyForbiddenSet = new HashSet<string>();

    private readonly IFileSystemFacade _fs;
    private readonly MeFileProcessor _sut;

    public MeFileProcessorTests()
    {
        _fs = Substitute.For<IFileSystemFacade>();
        _sut = new MeFileProcessor(_fs, EmptyForbiddenSet);
    }

    #region ScanMeFiles

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void ScanMeFiles_ReturnsMeFilePaths_WhenListaOsobContainsValidPersonFolders()
    {
        //Arrange
        _fs.DirectoryExists(PeopleListPath).Returns(true);
        _fs.EnumerateDirectories(PeopleListPath).Returns(new[] { AnnaFolder, BobFolder });
        _fs.FileExists(AnnaMeJson).Returns(true);
        _fs.FileExists(BobMeJson).Returns(true);

        //Act
        var result = _sut.ScanMeFiles(RootPath).ToList();

        //Assert
        Assert.Equal(2, result.Count);
        Assert.Contains(AnnaMeJson, result);
        Assert.Contains(BobMeJson, result);
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void ScanMeFiles_ReturnsEmpty_WhenListaOsobIsEmpty()
    {
        //Arrange
        _fs.DirectoryExists(PeopleListPath).Returns(true);
        _fs.EnumerateDirectories(PeopleListPath).Returns(Array.Empty<string>());

        //Act
        var result = _sut.ScanMeFiles(RootPath).ToList();

        //Assert
        Assert.Empty(result);
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void ScanMeFiles_SkipsFolder_WhenFolderNameIsForbidden()
    {
        //Arrange
        var forbiddenSet = new HashSet<string>(StringComparer.Ordinal) { "Pozostałe nieuporządkowane" };
        var sut = new MeFileProcessor(_fs, forbiddenSet);
        var forbiddenFolder = @"C:\fake\root\Lista osób\Pozostałe nieuporządkowane";
        var forbiddenMeJson = @"C:\fake\root\Lista osób\Pozostałe nieuporządkowane\me.json";

        _fs.DirectoryExists(PeopleListPath).Returns(true);
        _fs.EnumerateDirectories(PeopleListPath).Returns(new[] { forbiddenFolder, AnnaFolder });
        _fs.FileExists(forbiddenMeJson).Returns(true);
        _fs.FileExists(AnnaMeJson).Returns(true);

        //Act
        var result = sut.ScanMeFiles(RootPath).ToList();

        //Assert
        Assert.Single(result);
        Assert.Contains(AnnaMeJson, result);
        Assert.DoesNotContain(forbiddenMeJson, result);
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void ScanMeFiles_SkipsFolder_WhenMeJsonAbsentInsideFolder()
    {
        //Arrange
        _fs.DirectoryExists(PeopleListPath).Returns(true);
        _fs.EnumerateDirectories(PeopleListPath).Returns(new[] { AnnaFolder });
        _fs.FileExists(AnnaMeJson).Returns(false);

        //Act
        var result = _sut.ScanMeFiles(RootPath).ToList();

        //Assert
        Assert.Empty(result);
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void ScanMeFiles_Throws_WhenListaOsobDirectoryDoesNotExist()
    {
        //Arrange
        _fs.DirectoryExists(PeopleListPath).Returns(false);

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
    public void ScanMeFiles_DoesNotRecurseIntoNestedSubfolders()
    {
        //Arrange
        var nestedFolder = @"C:\fake\root\Lista osób\Anna Iksińska\Children";
        var nestedMeJson = @"C:\fake\root\Lista osób\Anna Iksińska\Children\me.json";

        _fs.DirectoryExists(PeopleListPath).Returns(true);
        _fs.EnumerateDirectories(PeopleListPath).Returns(new[] { AnnaFolder });
        _fs.FileExists(AnnaMeJson).Returns(true);
        // nested: would only be reached if recursive enumeration happened
        _fs.EnumerateDirectories(AnnaFolder).Returns(new[] { nestedFolder });
        _fs.FileExists(nestedMeJson).Returns(true);

        //Act
        var result = _sut.ScanMeFiles(RootPath).ToList();

        //Assert — only Anna's direct me.json returned; no recursion
        Assert.Single(result);
        Assert.Contains(AnnaMeJson, result);
        _fs.Received(1).EnumerateDirectories(PeopleListPath);
        _fs.DidNotReceive().EnumerateDirectories(AnnaFolder);
    }

    #endregion

    #region ReadMeFile

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void ReadMeFile_ReturnsMeFile_WhenContentIsValid()
    {
        //Arrange
        var json = FixtureLoader.LoadMeFixture();
        _fs.FileExists(AnnaMeJson).Returns(true);
        _fs.ReadAllText(AnnaMeJson).Returns(json);

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
        _fs.FileExists(AnnaMeJson).Returns(true);
        _fs.ReadAllText(AnnaMeJson).Returns("not-valid-json{{{");

        //Act & Assert
        Assert.Throws<JsonException>(() => _sut.ReadMeFile(AnnaMeJson));
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void ReadMeFile_Throws_WhenFileDoesNotExist()
    {
        //Arrange
        _fs.FileExists(AnnaMeJson).Returns(false);

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
        _fs.Received(1).WriteAllText(AnnaMeJson, expectedJson);
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void WriteMeFile_Throws_WhenPathIsNull()
    {
        var meFile = new MeFile();
        Assert.Throws<ArgumentNullException>(() => _sut.WriteMeFile(null, meFile));
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void WriteMeFile_Throws_WhenPathIsEmpty()
    {
        var meFile = new MeFile();
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
        var json = FixtureLoader.LoadMeFixture();
        _fs.FileExists(AnnaMeJson).Returns(true);
        _fs.ReadAllText(AnnaMeJson).Returns(json);

        //Act
        var original = _sut.ReadMeFile(AnnaMeJson);
        var modified = original with { PersonName = "Modified" };
        _sut.WriteMeFile(AnnaMeJson, modified);

        //Assert
        var expectedJson = JsonSerializer.Serialize(modified, MeFile.DefaultOptions);
        _fs.Received(1).WriteAllText(AnnaMeJson, expectedJson);
    }

    #endregion
}
