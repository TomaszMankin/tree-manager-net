namespace TreeManager.Core.Abstractions.Settings;

public interface IRootPointerStore
{
    /// <summary>Returns the stored root path, or empty string when no pointer exists.</summary>
    string Read();

    /// <summary>Writes the root path, creating the parent directory if needed.</summary>
    void Write(string rootPath);
}
