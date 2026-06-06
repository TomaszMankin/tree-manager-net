using System;
using System.Threading.Tasks;
using Moq;
using Serilog;
using TreeManager.App.Services;
using TreeManager.Core.Abstractions.Notifications;
using TreeManager.Core.Domain.Notifications;

namespace TreeManager.App.L0.Services;

public sealed class CrashReporterTests
{
    private readonly Mock<ILogger> _logMock = new();
    private readonly Mock<ICrashDialogService> _dialogMock = new();
    private readonly Mock<IEmailEscalator> _escalatorMock = new();
    private readonly Mock<IOfflineQueue> _queueMock = new();

    private CrashReporter BuildSut() =>
        new(_logMock.Object, _dialogMock.Object, _escalatorMock.Object, _queueMock.Object);

    [Fact]
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
}
