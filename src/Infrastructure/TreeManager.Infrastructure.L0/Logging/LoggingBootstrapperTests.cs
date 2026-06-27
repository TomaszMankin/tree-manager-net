using System;
using System.IO;
using Serilog;
using Serilog.Events;
using TreeManager.Infrastructure.Logging;

namespace TreeManager.Infrastructure.L0.Logging;

public sealed class LoggingBootstrapperTests : IDisposable
{
    private readonly string _tempRoot;

    public LoggingBootstrapperTests()
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
    public void BuildLogFilePath_ReturnsPathUnderTreeManagerLogs_WhenRootGiven()
    {
        //Arrange
        var root = @"C:\FamilyData";
        var date = new DateTime(2026, 6, 6);

        //Act
        var path = LoggingBootstrapper.BuildLogFilePath(root, date);

        //Assert
        Assert.Contains(".TreeManagerNet", path);
        Assert.Contains("logs", path);
    }

    [Fact]
    public void BuildLogFilePath_ProducesIsoDatePrefix_WhenDateGiven()
    {
        //Arrange
        var root = @"C:\FamilyData";
        var date = new DateTime(2026, 6, 6);

        //Act
        var path = LoggingBootstrapper.BuildLogFilePath(root, date);
        var fileName = Path.GetFileName(path);

        //Assert
        Assert.StartsWith("2026-06-06__", fileName);
        Assert.EndsWith(".log", fileName);
    }

    [Fact]
    public void Configure_WritesLogFileAtExpectedPath_WhenInfoLogged()
    {
        //Arrange
        var date = new DateTime(2026, 1, 15);
        var sut = new LoggingBootstrapper();

        //Act
        sut.Configure(_tempRoot, date);
        Log.Information("Test message");
        Log.CloseAndFlush();

        //Assert
        var logsDir = Path.Combine(_tempRoot, ".TreeManagerNet", "logs");
        var files = Directory.GetFiles(logsDir, "*.log");
        Assert.Single(files);
        var content = File.ReadAllText(files[0]);
        Assert.False(string.IsNullOrWhiteSpace(content));
    }

    [Fact]
    public void Configure_WritesAllSeverities_WhenInfoWarningErrorFatalLogged()
    {
        //Arrange
        var date = new DateTime(2026, 1, 15);
        var sut = new LoggingBootstrapper();

        //Act
        sut.Configure(_tempRoot, date, LogEventLevel.Information);
        Log.Information("Info message");
        Log.Warning("Warning message");
        Log.Error("Error message");
        Log.Fatal("Fatal message");
        Log.CloseAndFlush();

        //Assert
        var logPath = LoggingBootstrapper.BuildLogFilePath(_tempRoot, date);
        var content = File.ReadAllText(logPath);
        Assert.Contains("[INF]", content);
        Assert.Contains("[WRN]", content);
        Assert.Contains("[ERR]", content);
        Assert.Contains("[FTL]", content);
    }
}
