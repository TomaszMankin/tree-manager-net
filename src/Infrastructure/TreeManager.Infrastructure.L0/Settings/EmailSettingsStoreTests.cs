using Moq;
using Serilog;
using TreeManager.Common.TestUtilities;
using TreeManager.Core.Abstractions.IO;
using TreeManager.Infrastructure.Settings;

namespace TreeManager.Infrastructure.L0.Settings;

public sealed class EmailSettingsStoreTests
{
    private const string FakeSettingsPath = @"C:\fake\appsettings.user.json";

    private readonly Mock<IFileSystemFacade> _fsMock = new();
    private readonly Mock<ILogger> _logMock = new();

    private EmailSettingsStore BuildSut() =>
        new(FakeSettingsPath, _fsMock.Object, _logMock.Object);

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void Get_ReturnsConfiguredSettings_WhenJsonValid()
    {
        //Arrange
        var json = @"{
            ""email"": {
                ""host"": ""smtp.gmail.com"",
                ""port"": 587,
                ""useSsl"": true,
                ""fromAddress"": ""sender@gmail.com"",
                ""appPassword"": ""abcd efgh ijkl mnop"",
                ""toAddress"": ""receiver@gmail.com""
            }
        }";
        _fsMock.Setup(f => f.FileExists(FakeSettingsPath)).Returns(true);
        _fsMock.Setup(f => f.ReadAllText(FakeSettingsPath)).Returns(json);

        //Act
        var result = BuildSut().Get();

        //Assert
        Assert.True(result.IsConfigured);
        Assert.Equal("smtp.gmail.com", result.Host);
        Assert.Equal(587, result.Port);
        Assert.True(result.UseSsl);
        Assert.Equal("sender@gmail.com", result.FromAddress);
        Assert.Equal("abcd efgh ijkl mnop", result.AppPassword);
        Assert.Equal("receiver@gmail.com", result.ToAddress);
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void Get_ReturnsNotConfigured_WhenFileMissing()
    {
        //Arrange
        _fsMock.Setup(f => f.FileExists(FakeSettingsPath)).Returns(false);

        //Act
        var result = BuildSut().Get();

        //Assert
        Assert.False(result.IsConfigured);
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void Get_ReturnsNotConfigured_WhenJsonMalformed()
    {
        //Arrange
        _fsMock.Setup(f => f.FileExists(FakeSettingsPath)).Returns(true);
        _fsMock.Setup(f => f.ReadAllText(FakeSettingsPath)).Returns("NOT VALID JSON {{{");

        //Act
        var result = BuildSut().Get();

        //Assert
        Assert.False(result.IsConfigured);
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void Get_ReturnsNotConfigured_WhenRequiredFieldsBlank()
    {
        //Arrange
        var json = @"{
            ""email"": {
                ""host"": """",
                ""port"": 0,
                ""useSsl"": false,
                ""fromAddress"": """",
                ""appPassword"": """",
                ""toAddress"": """"
            }
        }";
        _fsMock.Setup(f => f.FileExists(FakeSettingsPath)).Returns(true);
        _fsMock.Setup(f => f.ReadAllText(FakeSettingsPath)).Returns(json);

        //Act
        var result = BuildSut().Get();

        //Assert
        Assert.False(result.IsConfigured);
    }
}
