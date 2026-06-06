using System;
using System.IO;
using Serilog;
using Serilog.Events;

namespace TreeManager.Infrastructure.Logging;

public sealed class LoggingBootstrapper
{
    public static string BuildLogFilePath(string rootPath, DateTime date)
    {
        var fileName = $"{date:yyyy-MM-dd}__tree-manager.log";
        return Path.Combine(rootPath, ".TreeManager", "logs", fileName);
    }

    public void Configure(string rootPath, LogEventLevel minLevel = LogEventLevel.Information)
    {
        Configure(rootPath, DateTime.Now, minLevel);
    }

    public void Configure(string rootPath, DateTime date, LogEventLevel minLevel = LogEventLevel.Information)
    {
        var logFilePath = BuildLogFilePath(rootPath, date);
        var logDir = Path.GetDirectoryName(logFilePath);

        if (!string.IsNullOrEmpty(logDir))
        {
            Directory.CreateDirectory(logDir);
        }

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Is(minLevel)
            .WriteTo.File(logFilePath)
            .CreateLogger();
    }
}
