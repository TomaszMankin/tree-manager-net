using System;
using System.IO;
using Serilog;
using TreeManager.Core.Abstractions.IO;
using TreeManager.Core.Abstractions.Settings;

namespace TreeManager.Infrastructure.Settings;

public sealed class RootPointerStore : IRootPointerStore
{
    private readonly IFileSystemFacade _fs;
    private readonly string _pointerPath;
    private readonly string _legacyPointerPath;
    private readonly ILogger _log;

    public RootPointerStore(IFileSystemFacade fileSystem, ILogger log)
        : this(fileSystem, ResolveDefaultPointerPath(), ResolveLegacyPointerPath(), log) { }

    public RootPointerStore(IFileSystemFacade fileSystem, string pointerPath)
        : this(fileSystem, pointerPath, string.Empty, Log.Logger) { }

    internal RootPointerStore(IFileSystemFacade fileSystem, string pointerPath, string legacyPointerPath, ILogger log)
    {
        _fs = fileSystem;
        _pointerPath = pointerPath;
        _legacyPointerPath = legacyPointerPath;
        _log = log;
    }

    public string Read()
    {
        if (_fs.FileExists(_pointerPath))
        {
            return _fs.ReadAllText(_pointerPath).Trim();
        }

        if (!string.IsNullOrEmpty(_legacyPointerPath) && _fs.FileExists(_legacyPointerPath))
        {
            return MigrateFromLegacy();
        }

        return string.Empty;
    }

    public void Write(string rootPath)
    {
        var parent = Path.GetDirectoryName(_pointerPath);
        if (!string.IsNullOrEmpty(parent))
        {
            _fs.CreateDirectory(parent);
        }
        _fs.WriteAllText(_pointerPath, rootPath);
    }

    public static string ResolveDefaultPointerPath()
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        if (string.IsNullOrEmpty(localAppData))
        {
            return Path.Combine(Path.GetTempPath(), "PyTreeManager", "last_root.txt");
        }
        return Path.Combine(localAppData, "PyTreeManager", "last_root.txt");
    }

    private static string ResolveLegacyPointerPath()
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        if (string.IsNullOrEmpty(localAppData))
        {
            return Path.Combine(Path.GetTempPath(), "TreeManager", "last_root.txt");
        }
        return Path.Combine(localAppData, "TreeManager", "last_root.txt");
    }

    private string MigrateFromLegacy()
    {
        try
        {
            var value = _fs.ReadAllText(_legacyPointerPath).Trim();
            Write(value);
            return value;
        }
        catch (Exception ex)
        {
            _log.Error(ex, "RootPointerStore: failed to read legacy pointer {Path}", _legacyPointerPath);
            return string.Empty;
        }
    }
}
