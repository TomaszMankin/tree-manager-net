namespace TreeManager.Core.Abstractions.IO;

/// <summary>
/// Seam over System.IO for filesystem operations.
/// Placed in Core per ADR-001 Onion rule — Core owns all interfaces.
/// Implementation lives in Infrastructure/IO/FileSystemFacade.cs.
/// </summary>
public interface IFileSystemFacade
{
    /// <summary>Returns true if the file at <paramref name="path"/> exists.</summary>
    bool FileExists(string path);

    /// <summary>Returns true if the directory at <paramref name="path"/> exists.</summary>
    bool DirectoryExists(string path);

    /// <summary>Reads all text from the file at <paramref name="path"/> (UTF-8, BOM-tolerant).</summary>
    string ReadAllText(string path);

    /// <summary>Writes <paramref name="content"/> to the file at <paramref name="path"/> (UTF-8 without BOM).</summary>
    void WriteAllText(string path, string content);

    /// <summary>Enumerates file paths under <paramref name="path"/> matching <paramref name="searchPattern"/> (top-level only).</summary>
    IEnumerable<string> EnumerateFiles(string path, string searchPattern = "*");

    /// <summary>Enumerates immediate sub-directory paths under <paramref name="path"/>.</summary>
    IEnumerable<string> EnumerateDirectories(string path);

    /// <summary>Creates the directory at <paramref name="path"/> (idempotent).</summary>
    void CreateDirectory(string path);

    /// <summary>Deletes the file at <paramref name="path"/>.</summary>
    void DeleteFile(string path);

    /// <summary>Deletes the directory at <paramref name="path"/>.</summary>
    void DeleteDirectory(string path, bool recursive = false);
}
