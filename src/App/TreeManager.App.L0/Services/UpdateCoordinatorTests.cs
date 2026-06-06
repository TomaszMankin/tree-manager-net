using System;
using System.Threading.Tasks;
using Moq;
using Serilog;
using TreeManager.App.Services;

namespace TreeManager.App.L0.Services;

public sealed class UpdateCoordinatorTests
{
    private readonly Mock<IUpdateService> _updateServiceMock = new();
    private readonly Mock<IUpdatePromptService> _promptMock = new();
    private readonly Mock<ILogger> _logMock = new();

    #region RunAsync

    [Fact]
    public async Task RunAsync_ShowsPromptAndApplies_WhenUpdateAvailableAndUserAccepts()
    {
        //Arrange
        _updateServiceMock
            .Setup(s => s.CheckAsync())
            .ReturnsAsync(UpdateCheckResult.Available("1.1.0"));
        _promptMock
            .Setup(p => p.ConfirmUpdate("1.1.0"))
            .Returns(true);
        _updateServiceMock
            .Setup(s => s.DownloadAndApplyAsync())
            .Returns(Task.CompletedTask);
        var sut = BuildSut();

        //Act
        await sut.RunAsync();

        //Assert
        _promptMock.Verify(p => p.ConfirmUpdate("1.1.0"), Times.Once());
        _updateServiceMock.Verify(s => s.DownloadAndApplyAsync(), Times.Once());
    }

    [Fact]
    public async Task RunAsync_DoesNotApply_WhenUserDeclines()
    {
        //Arrange
        _updateServiceMock
            .Setup(s => s.CheckAsync())
            .ReturnsAsync(UpdateCheckResult.Available("1.1.0"));
        _promptMock
            .Setup(p => p.ConfirmUpdate(It.IsAny<string>()))
            .Returns(false);
        var sut = BuildSut();

        //Act
        await sut.RunAsync();

        //Assert
        _updateServiceMock.Verify(s => s.DownloadAndApplyAsync(), Times.Never());
    }

    [Fact]
    public async Task RunAsync_DoesNotPrompt_WhenNoUpdateAvailable()
    {
        //Arrange
        _updateServiceMock
            .Setup(s => s.CheckAsync())
            .ReturnsAsync(UpdateCheckResult.None);
        var sut = BuildSut();

        //Act
        await sut.RunAsync();

        //Assert
        _promptMock.Verify(p => p.ConfirmUpdate(It.IsAny<string>()), Times.Never());
        _updateServiceMock.Verify(s => s.DownloadAndApplyAsync(), Times.Never());
    }

    [Fact]
    public async Task RunAsync_DoesNotThrow_WhenCheckFails()
    {
        //Arrange
        _updateServiceMock
            .Setup(s => s.CheckAsync())
            .ThrowsAsync(new Exception("feed unavailable"));
        var sut = BuildSut();

        //Act
        var recorded = await Record.ExceptionAsync(() => sut.RunAsync());

        //Assert
        Assert.Null(recorded);
        _logMock.Verify(
            l => l.Error(It.IsAny<Exception>(), It.IsAny<string>()),
            Times.AtLeastOnce());
    }

    [Fact]
    public async Task RunAsync_LogsError_WhenApplyThrows()
    {
        //Arrange
        _updateServiceMock
            .Setup(s => s.CheckAsync())
            .ReturnsAsync(UpdateCheckResult.Available("1.1.0"));
        _promptMock
            .Setup(p => p.ConfirmUpdate(It.IsAny<string>()))
            .Returns(true);
        _updateServiceMock
            .Setup(s => s.DownloadAndApplyAsync())
            .ThrowsAsync(new Exception("apply failed"));
        var sut = BuildSut();

        //Act
        var recorded = await Record.ExceptionAsync(() => sut.RunAsync());

        //Assert
        Assert.Null(recorded);
        _logMock.Verify(
            l => l.Error(It.IsAny<Exception>(), It.IsAny<string>()),
            Times.AtLeastOnce());
    }

    #endregion

    private UpdateCoordinator BuildSut() =>
        new(_updateServiceMock.Object, _promptMock.Object, _logMock.Object);
}
