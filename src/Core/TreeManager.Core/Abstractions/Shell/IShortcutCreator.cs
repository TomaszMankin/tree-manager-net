namespace TreeManager.Core.Abstractions.Shell;

/// <summary>Creates a Windows shortcut (.lnk) pointing at a target path.</summary>
public interface IShortcutCreator
{
    /// <summary>Creates a shortcut at <paramref name="linkFilePath"/> that points to <paramref name="targetPath"/>.</summary>
    void Create(string targetPath, string linkFilePath);
}
