using TreeManager.Core.Abstractions.Services;
using TreeManager.Core.Abstractions.Settings;

namespace TreeManager.App.Services;

/// <summary>Groups Drzewo- and Rody-command collaborators — same pattern as PersonEditDependencies.</summary>
public sealed record FolderTreeCommandDependencies(
    IFolderTreeGenerator Generator,
    IFolderTreeSettingsStore SettingsStore,
    ILineageFolderGenerator LineageGenerator);
