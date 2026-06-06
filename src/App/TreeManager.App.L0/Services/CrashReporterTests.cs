using System;
using Moq;
using Serilog;
using Serilog.Events;
using TreeManager.App.Services;

namespace TreeManager.App.L0.Services;

public sealed class CrashReporterTests
{
    [Fact]
    public void Report_LogsErrorAndShowsDialogOnce_WhenExceptionGiven()
    {
        //Arrange
        var loggerMock = new Mock<ILogger>();
        var dialogMock = new Mock<ICrashDialogService>();
        var sut = new CrashReporter(loggerMock.Object, dialogMock.Object);
        var exception = new InvalidOperationException("test error");

        //Act
        sut.Report(exception, "test");

        //Assert
        loggerMock.Verify(
            l => l.Error<string>(
                exception,
                "Unhandled exception from {Source}",
                "test"),
            Times.Once);
        dialogMock.Verify(d => d.ShowCrash(), Times.Once);
    }

    [Fact]
    public void Report_DoesNotThrow_WhenDialogServiceThrows()
    {
        //Arrange
        var loggerMock = new Mock<ILogger>();
        var dialogMock = new Mock<ICrashDialogService>();
        dialogMock.Setup(d => d.ShowCrash()).Throws<InvalidOperationException>();

        var sut = new CrashReporter(loggerMock.Object, dialogMock.Object);

        //Act & Assert
        var exception = Record.Exception(() => sut.Report(new Exception("boom"), "test"));
        Assert.Null(exception);
    }
}
