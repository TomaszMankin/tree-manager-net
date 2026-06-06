using System;
using System.IO;
using Moq;
using Serilog;
using Serilog.Events;
using TreeManager.App.Services;
using TreeManager.Core.Abstractions.Notifications;
using TreeManager.Infrastructure.Logging;

namespace TreeManager.App.L1.Services;

[Collection("CrashReporterIntegration")]
public sealed class CrashReporterIntegrationTests : IDisposable
{
    private readonly string _tempRoot;

    public CrashReporterIntegrationTests()
    {
        _tempRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempRoot);
    }

    public void Dispose()
    {
        Log.CloseAndFlush();

        try
        {
            Directory.Delete(_tempRoot, recursive: true);
        }
        catch
        {
            // best-effort cleanup
        }
    }

    [Fact]
    public void Report_WritesLogEntry_WhenExceptionReported()
    {
        //Arrange
        var date = new DateTime(2026, 1, 15);
        var bootstrapper = new LoggingBootstrapper();
        bootstrapper.Configure(_tempRoot, date, LogEventLevel.Information);

        var dialogMock = new Mock<ICrashDialogService>();
        var escalatorMock = new Mock<IEmailEscalator>();
        var queueMock = new Mock<IOfflineQueue>();
        var sut = new CrashReporter(Log.Logger, dialogMock.Object, escalatorMock.Object, queueMock.Object);
        var exception = new InvalidOperationException("crash-test-error");

        //Act
        sut.Report(exception, "IntegrationTest");
        Log.CloseAndFlush();

        //Assert
        var logPath = LoggingBootstrapper.BuildLogFilePath(_tempRoot, date);
        var content = File.ReadAllText(logPath);
        Assert.Contains("crash-test-error", content);
        Assert.Contains("[ERR]", content);
        dialogMock.Verify(d => d.ShowCrash(), Times.Once);
    }

    [Fact]
    public void Report_WritesLogEntry_WhenDialogServiceThrows()
    {
        //Arrange
        var date = new DateTime(2026, 1, 16);
        var bootstrapper = new LoggingBootstrapper();
        bootstrapper.Configure(_tempRoot, date, LogEventLevel.Information);

        var dialogMock = new Mock<ICrashDialogService>();
        dialogMock.Setup(d => d.ShowCrash()).Throws<Exception>();
        var escalatorMock = new Mock<IEmailEscalator>();
        var queueMock = new Mock<IOfflineQueue>();

        var sut = new CrashReporter(Log.Logger, dialogMock.Object, escalatorMock.Object, queueMock.Object);
        var exception = new InvalidOperationException("crash-dialog-throws");

        //Act
        sut.Report(exception, "IntegrationTest");
        Log.CloseAndFlush();

        //Assert
        var logPath = LoggingBootstrapper.BuildLogFilePath(_tempRoot, date);
        var content = File.ReadAllText(logPath);
        Assert.Contains("crash-dialog-throws", content);
        Assert.Contains("[ERR]", content);
    }
}
