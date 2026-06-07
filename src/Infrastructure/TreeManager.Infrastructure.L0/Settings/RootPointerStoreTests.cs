using Moq;
using TreeManager.Common.TestUtilities;
using TreeManager.Core.Abstractions.IO;
using TreeManager.Infrastructure.Settings;

namespace TreeManager.Infrastructure.L0.Settings;

public class RootPointerStoreTests
{
    private const string FakePointerPath = @"C:\fake\appdata\TreeManagerNet\last_root.txt";
    private const string FakePointerParent = @"C:\fake\appdata\TreeManagerNet";
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
    public void Write_WritesPathToPointerFile_Always()
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
    public void ResolveDefaultPointerPath_ReturnsPathUnderTreeManagerNet_Always()
    {
        //Arrange
        var expectedSubdir = "TreeManagerNet";

        //Act
        var path = RootPointerStore.ResolveDefaultPointerPath();

        //Assert
        Assert.Contains(expectedSubdir, path);
        Assert.EndsWith("last_root.txt", path);
    }
}
