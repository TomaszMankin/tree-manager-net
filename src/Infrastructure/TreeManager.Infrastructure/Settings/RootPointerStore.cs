using TreeManager.Core.Abstractions.IO;
using TreeManager.Core.Abstractions.Settings;

namespace TreeManager.Infrastructure.Settings;

public sealed class RootPointerStore : IRootPointerStore
{
    private readonly IFileSystemFacade _fs;
    private readonly string _pointerPath;

    public RootPointerStore(IFileSystemFacade fileSystem)
        : this(fileSystem, ResolveDefaultPointerPath()) { }

    public RootPointerStore(IFileSystemFacade fileSystem, string pointerPath)
    {
        _fs = fileSystem;
        _pointerPath = pointerPath;
    }

    public string Read()
    {
        if (!_fs.FileExists(_pointerPath)) return string.Empty;
        return _fs.ReadAllText(_pointerPath).Trim();
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

    private static string ResolveDefaultPointerPath()
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        if (string.IsNullOrEmpty(localAppData))
        {
            return Path.Combine(Path.GetTempPath(), "TreeManager", "last_root.txt");
        }
        return Path.Combine(localAppData, "TreeManager", "last_root.txt");
    }
}
