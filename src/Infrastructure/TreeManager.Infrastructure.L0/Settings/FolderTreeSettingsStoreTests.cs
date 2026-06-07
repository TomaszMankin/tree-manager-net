using System;
using System.IO;
using Moq;
using Serilog;
using TreeManager.Common.TestUtilities;
using TreeManager.Core.Abstractions.IO;
using TreeManager.Infrastructure.Settings;

namespace TreeManager.Infrastructure.L0.Settings;

public class FolderTreeSettingsStoreTests
{
    private const string FakeRoot = @"C:\fake\root";
    private const string SettingsPath = @"C:\fake\root\.TreeManagerNet\settings.json";

    private readonly Mock<IFileSystemFacade> _mockFs;
    private readonly Mock<ILogger> _mockLog;
    private readonly FolderTreeSettingsStore _sut;

    public FolderTreeSettingsStoreTests()
    {
        _mockFs = new Mock<IFileSystemFacade>();
        _mockLog = new Mock<ILogger>();

        _sut = new FolderTreeSettingsStore(_mockFs.Object, _mockLog.Object);
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void GetRootPersonId_ReturnsEmpty_WhenSettingsFileMissing()
    {
        //Arrange
        _mockFs.Setup(f => f.FileExists(SettingsPath)).Returns(false);

        //Act
        var result = _sut.GetRootPersonId(FakeRoot);

        //Assert
        Assert.Equal(Guid.Empty, result);
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void GetRootPersonId_ReturnsStoredGuid_WhenSettingsFileHasId()
    {
        //Arrange
        var expected = Guid.NewGuid();
        var json = $@"{{""drzewoRootPersonId"":""{expected}""}}";

        _mockFs.Setup(f => f.FileExists(SettingsPath)).Returns(true);
        _mockFs.Setup(f => f.ReadAllText(SettingsPath)).Returns(json);

        //Act
        var result = _sut.GetRootPersonId(FakeRoot);

        //Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void GetRootPersonId_ReturnsEmpty_WhenJsonMalformed()
    {
        //Arrange
        _mockFs.Setup(f => f.FileExists(SettingsPath)).Returns(true);
        _mockFs.Setup(f => f.ReadAllText(SettingsPath)).Returns("NOT VALID JSON {{{");

        //Act
        var result = _sut.GetRootPersonId(FakeRoot);

        //Assert — benign fallback, no throw
        Assert.Equal(Guid.Empty, result);
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void SetRootPersonId_WritesJsonUnderTreeManagerNet_WhenCalled()
    {
        //Arrange
        var id = Guid.NewGuid();
        string capturedContent = null;
        _mockFs.Setup(f => f.FileExists(SettingsPath)).Returns(false);
        _mockFs.Setup(f => f.WriteAllText(SettingsPath, It.IsAny<string>()))
            .Callback<string, string>((_, content) => capturedContent = content);

        //Act
        _sut.SetRootPersonId(FakeRoot, id);

        //Assert
        _mockFs.Verify(f => f.CreateDirectory(Path.Combine(FakeRoot, ".TreeManagerNet")), Times.Once());
        _mockFs.Verify(f => f.WriteAllText(SettingsPath, It.IsAny<string>()), Times.Once());
        Assert.NotNull(capturedContent);
        Assert.Contains(id.ToString(), capturedContent);
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void SetRootPersonId_PreservesUnknownJsonKeys_WhenSettingsAlreadyExist()
    {
        //Arrange
        var id = Guid.NewGuid();
        var existingJson = @"{""drzewoRootPersonId"":""00000000-0000-0000-0000-000000000000"",""futureKey"":""futureValue""}";
        string capturedContent = null;

        _mockFs.Setup(f => f.FileExists(SettingsPath)).Returns(true);
        _mockFs.Setup(f => f.ReadAllText(SettingsPath)).Returns(existingJson);
        _mockFs.Setup(f => f.WriteAllText(SettingsPath, It.IsAny<string>()))
            .Callback<string, string>((_, content) => capturedContent = content);

        //Act
        _sut.SetRootPersonId(FakeRoot, id);

        //Assert — unknown key preserved; new Guid written
        Assert.NotNull(capturedContent);
        Assert.Contains("futureKey", capturedContent);
        Assert.Contains("futureValue", capturedContent);
        Assert.Contains(id.ToString(), capturedContent);
    }
}
