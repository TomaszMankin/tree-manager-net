using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using Serilog;
using TreeManager.Core.Abstractions.IO;
using TreeManager.Core.Abstractions.Settings;

namespace TreeManager.Infrastructure.Settings;

/// <summary>
/// Persists Drzewo-specific settings per tree root at &lt;rootPath&gt;/.TreeManagerNet/settings.json.
/// Uses JSON read-modify-write via JsonNode to preserve unknown keys (forward-compat for future settings).
/// </summary>
public sealed class FolderTreeSettingsStore : IFolderTreeSettingsStore
{
    private const string RuntimeFolderName = ".TreeManagerNet";
    private const string SettingsFileName = "settings.json";
    private const string RootPersonIdKey = "drzewoRootPersonId";

    private readonly IFileSystemFacade _fs;
    private readonly ILogger _log;

    public FolderTreeSettingsStore(IFileSystemFacade fs, ILogger log)
    {
        _fs = fs;
        _log = log;
    }

    public Guid GetRootPersonId(string rootPath)
    {
        var path = SettingsPath(rootPath);

        if (!_fs.FileExists(path))
        {
            return Guid.Empty;
        }

        try
        {
            var json = _fs.ReadAllText(path);
            var node = JsonNode.Parse(json);
            if (node is not JsonObject obj)
            {
                return Guid.Empty;
            }

            if (obj[RootPersonIdKey] is JsonValue val && val.TryGetValue<string>(out var raw))
            {
                if (Guid.TryParse(raw, out var id))
                {
                    return id;
                }
            }

            return Guid.Empty;
        }
        catch (Exception ex)
        {
            _log.Error(ex, "FolderTreeSettingsStore: failed to read {Path}; returning Guid.Empty", path);
            return Guid.Empty;
        }
    }

    public void SetRootPersonId(string rootPath, Guid rootPersonId)
    {
        var dirPath = Path.Combine(rootPath, RuntimeFolderName);
        _fs.CreateDirectory(dirPath);

        var path = SettingsPath(rootPath);

        // Read-modify-write: load existing JSON to preserve unknown keys
        JsonObject obj;
        if (_fs.FileExists(path))
        {
            try
            {
                var existing = _fs.ReadAllText(path);
                var parsed = JsonNode.Parse(existing);
                obj = parsed as JsonObject ?? new JsonObject();
            }
            catch
            {
                obj = new JsonObject();
            }
        }
        else
        {
            obj = new JsonObject();
        }

        obj[RootPersonIdKey] = rootPersonId.ToString();

        var options = new JsonSerializerOptions { WriteIndented = true };
        var written = obj.ToJsonString(options);
        _fs.WriteAllText(path, written);
    }

    private static string SettingsPath(string rootPath)
    {
        return Path.Combine(rootPath, RuntimeFolderName, SettingsFileName);
    }
}
