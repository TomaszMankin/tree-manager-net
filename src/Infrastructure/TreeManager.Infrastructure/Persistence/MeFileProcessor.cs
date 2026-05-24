using System.Text.Json;
using TreeManager.Core.Abstractions.IO;
using TreeManager.Core.Abstractions.Persistence;
using TreeManager.Core.Domain;
using TreeManager.Core.Domain.Constants;

namespace TreeManager.Infrastructure.Persistence;

public sealed class MeFileProcessor : IMeFileProcessor
{
    private readonly IFileSystemFacade _fs;
    private readonly IReadOnlySet<string> _forbiddenFolders;

    public MeFileProcessor(IFileSystemFacade fileSystem)
        : this(fileSystem, ForbiddenFolders.Default) { }

    public MeFileProcessor(IFileSystemFacade fileSystem, IReadOnlySet<string> forbiddenFolders)
    {
        _fs = fileSystem;
        _forbiddenFolders = forbiddenFolders;
    }

    public IEnumerable<string> ScanMeFiles(string rootPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rootPath, nameof(rootPath));

        var peopleListPath = Path.Combine(rootPath, ForbiddenFolders.PeopleListFolderName);
        if (!_fs.DirectoryExists(peopleListPath))
        {
            throw new DirectoryNotFoundException(peopleListPath);
        }

        foreach (var folder in _fs.EnumerateDirectories(peopleListPath))
        {
            var folderName = Path.GetFileName(folder);
            if (_forbiddenFolders.Contains(folderName)) continue;

            var meFilePath = Path.Combine(folder, "me.json");
            if (_fs.FileExists(meFilePath))
            {
                yield return meFilePath;
            }
        }
    }

    public MeFile ReadMeFile(string meFilePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(meFilePath, nameof(meFilePath));

        if (!_fs.FileExists(meFilePath))
        {
            throw new FileNotFoundException(meFilePath);
        }

        var content = _fs.ReadAllText(meFilePath);
        return JsonSerializer.Deserialize<MeFile>(content, MeFile.DefaultOptions)
            ?? throw new JsonException($"me.json deserialized to null: {meFilePath}");
    }

    public void WriteMeFile(string meFilePath, MeFile content)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(meFilePath, nameof(meFilePath));
        ArgumentNullException.ThrowIfNull(content, nameof(content));

        var serialized = JsonSerializer.Serialize(content, MeFile.DefaultOptions);
        _fs.WriteAllText(meFilePath, serialized);
    }
}
