using NSubstitute;
using TreeManager.Common.TestUtilities;
using TreeManager.Core.Abstractions.IO;
using TreeManager.Infrastructure.Settings;

namespace TreeManager.Infrastructure.L0.Settings;

public class RootPointerStoreTests
{
    private const string FakePointerPath = @"C:\fake\appdata\TreeManager\last_root.txt";
    private const string FakePointerParent = @"C:\fake\appdata\TreeManager";
    private const string FakeRootPath = @"C:\fake\family\tree";

    private readonly IFileSystemFacade _fs;
    private readonly RootPointerStore _sut;

    public RootPointerStoreTests()
    {
        _fs = Substitute.For<IFileSystemFacade>();
        _sut = new RootPointerStore(_fs, FakePointerPath);
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void Read_ReturnsEmptyString_WhenPointerFileDoesNotExist()
    {
        //Arrange
        _fs.FileExists(FakePointerPath).Returns(false);

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
        _fs.FileExists(FakePointerPath).Returns(true);
        _fs.ReadAllText(FakePointerPath).Returns($"{FakeRootPath}\r\n");

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
        _fs.Received(1).CreateDirectory(FakePointerParent);
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void Write_WritesPathToPointerFile_WhenCalled()
    {
        //Act
        _sut.Write(FakeRootPath);

        //Assert
        _fs.Received(1).WriteAllText(FakePointerPath, FakeRootPath);
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

        //Assert — second call overwrites
        _fs.Received(1).WriteAllText(FakePointerPath, firstPath);
        _fs.Received(1).WriteAllText(FakePointerPath, secondPath);
    }
}
