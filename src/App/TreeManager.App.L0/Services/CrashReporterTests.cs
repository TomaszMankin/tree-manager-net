using System;
using System.Threading.Tasks;
using Moq;
using Serilog;
using TreeManager.App.Services;
using TreeManager.Common.TestUtilities;
using TreeManager.Core.Abstractions.Notifications;
using TreeManager.Core.Abstractions.Settings;
using TreeManager.Core.Domain.Notifications;

namespace TreeManager.App.L0.Services;

public sealed class CrashReporterTests
{
    private readonly Mock<ILogger> _logMock = new();
    private readonly Mock<ICrashDialogService> _dialogMock = new();
    private readonly Mock<IEmailEscalator> _escalatorMock = new();
    private readonly Mock<IOfflineQueue> _queueMock = new();
    private readonly Mock<IEmailSettingsStore> _settingsMock = new();
    private readonly Mock<IInfoDialogService> _infoMock = new();

    #region Report — crash path

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void Report_LogsErrorAndShowsDialogOnce_WhenExceptionGiven()
    {
        //Arrange
        var sut = BuildSut();
        var exception = new InvalidOperationException("test error");

        //Act
        sut.Report(exception, "test");

        //Assert
        _logMock.Verify(
            l => l.Error<string>(
                exception,
                "Unhandled exception from {Source}",
                "test"),
            Times.Once);
        _dialogMock.Verify(d => d.ShowCrash(), Times.Once);
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void Report_DoesNotThrow_WhenDialogServiceThrows()
    {
        //Arrange
        _dialogMock.Setup(d => d.ShowCrash()).Throws<InvalidOperationException>();
        var sut = BuildSut();

        //Act & Assert
        var exception = Record.Exception(() => sut.Report(new Exception("boom"), "test"));
        Assert.Null(exception);
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public async Task EscalateAsync_FiresEscalation_WhenExceptionReported()
    {
        //Arrange
        _escalatorMock.Setup(e => e.SendAsync(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);
        var sut = BuildSut();
        var ex = new InvalidOperationException("crash-in-test");

        //Act
        await sut.EscalateAsync(ex, "TestSource");

        //Assert
        _escalatorMock.Verify(
            e => e.SendAsync(
                It.Is<string>(s => s.Contains("TestSource")),
                It.Is<string>(b => b.Contains("crash-in-test"))),
            Times.Once);
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public async Task EscalateAsync_EnqueuesMessage_WhenEscalationThrows()
    {
        //Arrange
        _escalatorMock.Setup(e => e.SendAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new Exception("smtp down"));
        var sut = BuildSut();
        var ex = new InvalidOperationException("crash-for-queue");

        //Act
        await sut.EscalateAsync(ex, "TestSource");

        //Assert
        _queueMock.Verify(
            q => q.Enqueue(It.Is<QueuedMessage>(m =>
                m.SubjectText.Contains("TestSource") &&
                m.BodyText.Contains("crash-for-queue"))),
            Times.Once);
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public async Task EscalateAsync_DoesNotThrow_WhenEscalationAndQueueBothThrow()
    {
        //Arrange
        _escalatorMock.Setup(e => e.SendAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new Exception("smtp down"));
        _queueMock.Setup(q => q.Enqueue(It.IsAny<QueuedMessage>()))
            .Throws(new Exception("disk full"));
        var sut = BuildSut();
        var ex = new InvalidOperationException("double-fault");

        //Act & Assert
        var recorded = await Record.ExceptionAsync(() => sut.EscalateAsync(ex, "TestSource"));
        Assert.Null(recorded);
    }

    #endregion

    #region ReportManual — non-crash path

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void ReportManual_DoesNotLogError_Always()
    {
        //Arrange
        SetupConfigured();
        var sut = BuildSut();

        //Act
        sut.ReportManual("test note");

        //Assert
        _logMock.Verify(
            l => l.Error(It.IsAny<Exception>(), It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void ReportManual_DoesNotShowCrashDialog_Always()
    {
        //Arrange
        SetupConfigured();
        var sut = BuildSut();

        //Act
        sut.ReportManual("test note");

        //Assert
        _dialogMock.Verify(d => d.ShowCrash(), Times.Never);
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void ReportManual_ShowsInfoDialog_WhenSmtpConfigured()
    {
        //Arrange
        SetupConfigured();
        var sut = BuildSut();

        //Act
        sut.ReportManual("test note");

        //Assert
        _infoMock.Verify(i => i.Show(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void ReportManual_ShowsNotConfiguredDialog_WhenSmtpHostEmpty()
    {
        //Arrange
        _settingsMock.Setup(s => s.Get()).Returns(new EmailSettings());
        var sut = BuildSut();

        //Act
        sut.ReportManual("test note");

        //Assert — info dialog shown with "nie skonfigurowana" text
        _infoMock.Verify(
            i => i.Show(
                It.IsAny<string>(),
                It.Is<string>(m => m.Contains("nie jest skonfigurowana"))),
            Times.Once);
    }

    #endregion

    private void SetupConfigured()
    {
        _settingsMock.Setup(s => s.Get()).Returns(new EmailSettings
        {
            Host = "smtp.example.com",
            Port = 587,
            UseSsl = true,
            FromAddress = "from@example.com",
            ToAddress = "to@example.com",
            AppPassword = "secret"
        });
    }

    private CrashReporter BuildSut() =>
        new(_logMock.Object, _dialogMock.Object, _escalatorMock.Object, _queueMock.Object,
            _settingsMock.Object, _infoMock.Object);
}
