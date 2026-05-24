using System.Text;
using TreeManager.Core.Abstractions.IO;

namespace TreeManager.Infrastructure.IO;

/// <summary>
/// Pass-through implementation of <see cref="IFileSystemFacade"/> over System.IO.
/// Writes UTF-8 WITHOUT BOM (matching py-tree-manager default write style — see ADR-002).
/// Reads are BOM-tolerant via File.ReadAllText default StreamReader behaviour on .NET 6+.
/// </summary>
public sealed class FileSystemFacade : IFileSystemFacade
{
    // UTF-8 without BOM — py-tree-manager writes without BOM by default
    private static readonly Encoding Utf8NoBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

    /// <inheritdoc/>
    public bool FileExists(string path) => File.Exists(path);

    /// <inheritdoc/>
    public bool DirectoryExists(string path) => Directory.Exists(path);

    /// <inheritdoc/>
    public string ReadAllText(string path) => File.ReadAllText(path, Encoding.UTF8);

    /// <inheritdoc/>
    public void WriteAllText(string path, string content) => File.WriteAllText(path, content, Utf8NoBom);

    /// <inheritdoc/>
    public IEnumerable<string> EnumerateFiles(string path, string searchPattern = "*")
        => Directory.EnumerateFiles(path, searchPattern, SearchOption.TopDirectoryOnly);

    /// <inheritdoc/>
    public IEnumerable<string> EnumerateDirectories(string path)
        => Directory.EnumerateDirectories(path);

    /// <inheritdoc/>
    public void CreateDirectory(string path) => Directory.CreateDirectory(path);

    /// <inheritdoc/>
    public void DeleteFile(string path) => File.Delete(path);

    /// <inheritdoc/>
    public void DeleteDirectory(string path, bool recursive = false) => Directory.Delete(path, recursive);
}
