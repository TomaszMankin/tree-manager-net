namespace TreeManager.Core.Abstractions.Persistence;

using TreeManager.Core.Domain;

public interface IMeFileProcessor
{
    /// <summary>Enumerates me.json paths under &lt;rootPath&gt;/Lista osób/, depth-1 only, forbidden folders skipped.</summary>
    IEnumerable<string> ScanMeFiles(string rootPath);

    /// <summary>Reads and deserializes the me.json at <paramref name="meFilePath"/>.</summary>
    MeFile ReadMeFile(string meFilePath);

    /// <summary>Serializes <paramref name="content"/> and writes it to <paramref name="meFilePath"/>. Overwrites if exists.</summary>
    void WriteMeFile(string meFilePath, MeFile content);
}
