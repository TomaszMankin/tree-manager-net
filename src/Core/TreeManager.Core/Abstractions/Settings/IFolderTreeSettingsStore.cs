using System;

namespace TreeManager.Core.Abstractions.Settings;

/// <summary>
/// Persists Drzewo-specific settings per tree root.
/// Reads/writes <c>&lt;rootPath&gt;/.TreeManagerNet/settings.json</c>.
/// </summary>
public interface IFolderTreeSettingsStore
{
    /// <summary>
    /// Returns the persisted root-person Guid for <paramref name="rootPath"/>.
    /// Returns <see cref="Guid.Empty"/> when no value is stored or the file is unreadable.
    /// </summary>
    Guid GetRootPersonId(string rootPath);

    /// <summary>Persists <paramref name="rootPersonId"/> as the Drzewo root for <paramref name="rootPath"/>.</summary>
    void SetRootPersonId(string rootPath, Guid rootPersonId);
}
