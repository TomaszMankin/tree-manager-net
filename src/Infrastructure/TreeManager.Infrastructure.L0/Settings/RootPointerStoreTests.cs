using Moq;
using Serilog;
using TreeManager.Common.TestUtilities;
using TreeManager.Core.Abstractions.IO;
using TreeManager.Infrastructure.Settings;

namespace TreeManager.Infrastructure.L0.Settings;

public class RootPointerStoreTests
{
    private const string FakePointerPath = @"C:\fake\appdata\PyTreeManager\last_root.txt";
    private const string FakePointerParent = @"C:\fake\appdata\PyTreeManager";
    private const string FakeLegacyPointerPath = @"C:\fake\appdata\TreeManager\last_root.txt";
    private const string FakeRootPath = @"C:\fake\family\tree";

    private readonly Mock<IFileSystemFacade> _fs;
    private readonly RootPointerStore _sut;

    public RootPointerStoreTests()
    {
        _fs = new Mock<IFileSystemFacade>();
        _sut = new RootPointerStore(_fs.Object, FakePointerPath);
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void Read_ReturnsEmptyString_WhenPointerFileDoesNotExist()
    {
        //Arrange
        _fs.Setup(x => x.FileExists(FakePointerPath)).Returns(false);

        //Act
        var result = _sut.Read();

        //Assert
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void Read_ReturnsTrimmedPath_WhenPointerFileExists()
    {
        //Arrange
        _fs.Setup(x => x.FileExists(FakePointerPath)).Returns(true);
        _fs.Setup(x => x.ReadAllText(FakePointerPath)).Returns($"{FakeRootPath}\r\n");

        //Act
        var result = _sut.Read();

        //Assert
        Assert.Equal(FakeRootPath, result);
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void Write_CreatesParentDirectory_WhenParentMissing()
    {
        //Act
        _sut.Write(FakeRootPath);

        //Assert
        _fs.Verify(x => x.CreateDirectory(FakePointerParent), Times.Once());
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void Write_WritesPathToPointerFile_WhenCalled()
    {
        //Act
        _sut.Write(FakeRootPath);

        //Assert
        _fs.Verify(x => x.WriteAllText(FakePointerPath, FakeRootPath), Times.Once());
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void Write_OverwritesExistingPointer_WhenCalledTwice()
    {
        //Arrange
        var firstPath = @"C:\first\root";
        var secondPath = @"C:\second\root";

        //Act
        _sut.Write(firstPath);
        _sut.Write(secondPath);

        //Assert
        _fs.Verify(x => x.WriteAllText(FakePointerPath, firstPath), Times.Once());
        _fs.Verify(x => x.WriteAllText(FakePointerPath, secondPath), Times.Once());
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void Read_ReturnsMigratedValue_WhenOldPathExistsAndNewPathAbsent()
    {
        //Arrange
        var fs = new Mock<IFileSystemFacade>();
        var log = new Mock<ILogger>();
        var sut = new RootPointerStore(fs.Object, FakePointerPath, FakeLegacyPointerPath, log.Object);

        fs.Setup(x => x.FileExists(FakePointerPath)).Returns(false);
        fs.Setup(x => x.FileExists(FakeLegacyPointerPath)).Returns(true);
        fs.Setup(x => x.ReadAllText(FakeLegacyPointerPath)).Returns(FakeRootPath);

        //Act
        var result = sut.Read();

        //Assert
        Assert.Equal(FakeRootPath, result);
        fs.Verify(x => x.WriteAllText(FakePointerPath, FakeRootPath), Times.Once());
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void Read_ReturnsNewPathValue_WhenBothPathsExist()
    {
        //Arrange
        var fs = new Mock<IFileSystemFacade>();
        var log = new Mock<ILogger>();
        var sut = new RootPointerStore(fs.Object, FakePointerPath, FakeLegacyPointerPath, log.Object);

        fs.Setup(x => x.FileExists(FakePointerPath)).Returns(true);
        fs.Setup(x => x.ReadAllText(FakePointerPath)).Returns(FakeRootPath);

        //Act
        var result = sut.Read();

        //Assert
        Assert.Equal(FakeRootPath, result);
        fs.Verify(x => x.ReadAllText(FakeLegacyPointerPath), Times.Never());
        fs.Verify(x => x.WriteAllText(It.IsAny<string>(), It.IsAny<string>()), Times.Never());
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void Read_ReturnsEmpty_AndLogsError_WhenOldFileUnreadable()
    {
        //Arrange
        var fs = new Mock<IFileSystemFacade>();
        var log = new Mock<ILogger>();
        var sut = new RootPointerStore(fs.Object, FakePointerPath, FakeLegacyPointerPath, log.Object);

        fs.Setup(x => x.FileExists(FakePointerPath)).Returns(false);
        fs.Setup(x => x.FileExists(FakeLegacyPointerPath)).Returns(true);
        fs.Setup(x => x.ReadAllText(FakeLegacyPointerPath)).Throws(new IOException("access denied"));

        //Act
        var result = sut.Read();

        //Assert
        Assert.Equal(string.Empty, result);
        log.Verify(
            x => x.Error(It.IsAny<Exception>(), It.IsAny<string>(), It.IsAny<string>()),
            Times.Once());
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void ResolveDefaultPointerPath_ReturnsPathUnderPyTreeManager()
    {
        //Arrange
        var expectedSubdir = "PyTreeManager";

        //Act
        var path = RootPointerStore.ResolveDefaultPointerPath();

        //Assert
        Assert.Contains(expectedSubdir, path);
        Assert.EndsWith("last_root.txt", path);
    }
}
