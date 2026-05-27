using System.Text.Json;
using TreeManager.Core.Abstractions.IO;
using TreeManager.Core.Abstractions.Persistence;
using TreeManager.Core.Domain;

namespace TreeManager.Infrastructure.Persistence;

public sealed class MeFileProcessor : IMeFileProcessor
{
    private const string PeopleListFolderName = "Lista osób";

    private readonly IFileSystemFacade _fs;

    public MeFileProcessor(IFileSystemFacade fileSystem)
    {
        _fs = fileSystem;
    }

    public IEnumerable<string> ScanMeFiles(string rootPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rootPath);

        var peopleListPath = Path.Combine(rootPath, PeopleListFolderName);
        if (!_fs.DirectoryExists(peopleListPath))
        {
            throw new DirectoryNotFoundException(peopleListPath);
        }

        foreach (var folder in _fs.EnumerateDirectories(peopleListPath))
        {
            var meFilePath = Path.Combine(folder, "me.json");
            if (_fs.FileExists(meFilePath))
            {
                yield return meFilePath;
            }
        }
    }

    public MeFile ReadMeFile(string meFilePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(meFilePath);

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
        ArgumentException.ThrowIfNullOrWhiteSpace(meFilePath);
        ArgumentNullException.ThrowIfNull(content);

        var serialized = JsonSerializer.Serialize(content, MeFile.DefaultOptions);
        _fs.WriteAllText(meFilePath, serialized);
    }
}
