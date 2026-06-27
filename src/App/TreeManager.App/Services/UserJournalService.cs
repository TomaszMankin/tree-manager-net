using System;
using System.IO;
using Serilog;
using TreeManager.Core.Abstractions.IO;
using TreeManager.Core.Abstractions.Settings;

namespace TreeManager.App.Services;

public sealed class UserJournalService : IUserJournalService
{
    private const string RuntimeFolderName = ".TreeManagerNet";
    private const string LogsFolderName = "logs";

    private readonly IFileSystemFacade _fs;
    private readonly IRootPointerStore _rootPointerStore;
    private readonly ILogger _log;

    public UserJournalService(IFileSystemFacade fs, IRootPointerStore rootPointerStore, ILogger log)
    {
        _fs = fs;
        _rootPointerStore = rootPointerStore;
        _log = log;
    }

    public void LogAction(string action, string personLabel = null)
    {
        try
        {
            var label = string.IsNullOrWhiteSpace(personLabel) ? "-" : personLabel;
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            var line = $"[{timestamp}] [{label}] User clicked \"{action}\"{Environment.NewLine}";

            var journalPath = ResolveJournalPath();
            _fs.AppendAllText(journalPath, line);
        }
        catch (Exception ex)
        {
            _log.Error(ex, "UserJournalService: failed to write journal entry for action {Action}", action);
        }
    }

    private string ResolveJournalPath()
    {
        var root = _rootPointerStore.Read();

        string logsDir;
        if (string.IsNullOrWhiteSpace(root))
        {
            logsDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "TreeManagerNet",
                LogsFolderName);
        }
        else
        {
            logsDir = Path.Combine(root, RuntimeFolderName, LogsFolderName);
        }

        _fs.CreateDirectory(logsDir);

        var fileName = $"{DateTime.Now:yyyy-MM-dd}__journey.log";
        return Path.Combine(logsDir, fileName);
    }
}
