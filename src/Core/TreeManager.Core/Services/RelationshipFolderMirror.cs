using System;
using System.Collections.Generic;
using System.IO;
using Serilog;
using TreeManager.Core.Abstractions.IO;
using TreeManager.Core.Abstractions.Services;
using TreeManager.Core.Abstractions.Shell;
using TreeManager.Core.Domain;

namespace TreeManager.Core.Services;

public sealed class RelationshipFolderMirror : IRelationshipFolderMirror
{
    private const string ParentsSubfolder = "Rodzice";
    private const string ChildrenSubfolder = "Dzieci";
    private const string SpousesSubfolder = "Małżonkowie";
    private const string SiblingsSubfolder = "Rodzeństwo";

    private readonly IFileSystemFacade _fs;
    private readonly IShortcutCreator _shortcutCreator;
    private readonly ILogger _log;

    public RelationshipFolderMirror(IFileSystemFacade fs, IShortcutCreator shortcutCreator, ILogger log)
    {
        ArgumentNullException.ThrowIfNull(fs);
        ArgumentNullException.ThrowIfNull(shortcutCreator);
        ArgumentNullException.ThrowIfNull(log);
        _fs = fs;
        _shortcutCreator = shortcutCreator;
        _log = log;
    }

    public void Mirror(
        MeFile person,
        string personFolderPath,
        IReadOnlyDictionary<Guid, string> relatedFolderPathsByUid)
    {
        EnsureRelationshipSubfolders(personFolderPath);

        // 1. Write shortcuts for parents: person→parent in person/Rodzice, person←parent in parent/Dzieci
        foreach (var parentUid in person.ParentsId)
        {
            WriteShortcutPair(
                personFolderPath, ParentsSubfolder, person.PersonName,
                parentUid, ChildrenSubfolder,
                relatedFolderPathsByUid);
        }

        // 2. Write shortcuts for children: person→child in person/Dzieci, person←child in child/Rodzice
        foreach (var childUid in person.ChildrenId)
        {
            WriteShortcutPair(
                personFolderPath, ChildrenSubfolder, person.PersonName,
                childUid, ParentsSubfolder,
                relatedFolderPathsByUid);
        }

        // 3. Write shortcuts for spouses: both in each other's Małżonkowie
        foreach (var spouseUid in person.SpouseId)
        {
            WriteShortcutPair(
                personFolderPath, SpousesSubfolder, person.PersonName,
                spouseUid, SpousesSubfolder,
                relatedFolderPathsByUid);
        }

        // 4. Write shortcuts for siblings: both in each other's Rodzeństwo
        foreach (var siblingUid in person.SiblingsId)
        {
            WriteShortcutPair(
                personFolderPath, SiblingsSubfolder, person.PersonName,
                siblingUid, SiblingsSubfolder,
                relatedFolderPathsByUid);
        }
    }

    public void ApplyDelta(
        MeFile person,
        string personFolderPath,
        IReadOnlyDictionary<Guid, string> relatedFolderPathsByUid,
        IReadOnlyList<Guid> addedParents,
        IReadOnlyList<Guid> removedParents,
        IReadOnlyList<Guid> addedChildren,
        IReadOnlyList<Guid> removedChildren,
        IReadOnlyList<Guid> addedSpouses,
        IReadOnlyList<Guid> removedSpouses,
        IReadOnlyList<Guid> addedSiblings,
        IReadOnlyList<Guid> removedSiblings)
    {
        EnsureRelationshipSubfolders(personFolderPath);

        AddShortcutPairs(addedParents, personFolderPath, ParentsSubfolder, person.PersonName, ChildrenSubfolder, relatedFolderPathsByUid);
        AddShortcutPairs(addedChildren, personFolderPath, ChildrenSubfolder, person.PersonName, ParentsSubfolder, relatedFolderPathsByUid);
        AddShortcutPairs(addedSpouses, personFolderPath, SpousesSubfolder, person.PersonName, SpousesSubfolder, relatedFolderPathsByUid);
        AddShortcutPairs(addedSiblings, personFolderPath, SiblingsSubfolder, person.PersonName, SiblingsSubfolder, relatedFolderPathsByUid);

        RemoveShortcutPairs(removedParents, personFolderPath, ParentsSubfolder, person.PersonName, ChildrenSubfolder, relatedFolderPathsByUid);
        RemoveShortcutPairs(removedChildren, personFolderPath, ChildrenSubfolder, person.PersonName, ParentsSubfolder, relatedFolderPathsByUid);
        RemoveShortcutPairs(removedSpouses, personFolderPath, SpousesSubfolder, person.PersonName, SpousesSubfolder, relatedFolderPathsByUid);
        RemoveShortcutPairs(removedSiblings, personFolderPath, SiblingsSubfolder, person.PersonName, SiblingsSubfolder, relatedFolderPathsByUid);
    }

    private void EnsureRelationshipSubfolders(string personFolderPath)
    {
        _fs.CreateDirectory(Path.Combine(personFolderPath, ParentsSubfolder));
        _fs.CreateDirectory(Path.Combine(personFolderPath, ChildrenSubfolder));
        _fs.CreateDirectory(Path.Combine(personFolderPath, SpousesSubfolder));
        _fs.CreateDirectory(Path.Combine(personFolderPath, SiblingsSubfolder));
    }

    private void AddShortcutPairs(
        IReadOnlyList<Guid> relatedUids,
        string personFolderPath,
        string personSubfolder,
        string personName,
        string relatedSubfolder,
        IReadOnlyDictionary<Guid, string> relatedFolderPathsByUid)
    {
        foreach (var uid in relatedUids)
        {
            WriteShortcutPair(personFolderPath, personSubfolder, personName, uid, relatedSubfolder, relatedFolderPathsByUid);
        }
    }

    private void RemoveShortcutPairs(
        IReadOnlyList<Guid> relatedUids,
        string personFolderPath,
        string personSubfolder,
        string personName,
        string relatedSubfolder,
        IReadOnlyDictionary<Guid, string> relatedFolderPathsByUid)
    {
        foreach (var uid in relatedUids)
        {
            if (!relatedFolderPathsByUid.TryGetValue(uid, out var relatedFolder))
            {
                continue;
            }

            var relatedFolderName = Path.GetFileName(relatedFolder);
            var sanitizedRelatedName = FolderTreeNaming.Sanitize(relatedFolderName);
            var sanitizedPersonName = FolderTreeNaming.Sanitize(personName);

            TryDeleteShortcut(Path.Combine(personFolderPath, personSubfolder, sanitizedRelatedName + ".lnk"));
            TryDeleteShortcut(Path.Combine(relatedFolder, relatedSubfolder, sanitizedPersonName + ".lnk"));
        }
    }

    private void WriteShortcutPair(
        string personFolderPath,
        string personSubfolder,
        string personName,
        Guid relatedUid,
        string relatedSubfolder,
        IReadOnlyDictionary<Guid, string> relatedFolderPathsByUid)
    {
        if (relatedUid == Guid.Empty)
        {
            return;
        }

        if (!relatedFolderPathsByUid.TryGetValue(relatedUid, out var relatedFolder))
        {
            _log.Error("RelationshipFolderMirror: related folder for {Uid} not found — shortcut skipped", relatedUid);
            return;
        }

        var relatedFolderName = Path.GetFileName(relatedFolder);

        // person → related shortcut in person's subfolder
        var personToRelatedLink = Path.Combine(personFolderPath, personSubfolder,
            FolderTreeNaming.Sanitize(relatedFolderName) + ".lnk");
        TryCreateShortcut(relatedFolder, personToRelatedLink);

        // related → person shortcut in related's subfolder
        EnsureRelatedSubfolder(relatedFolder, relatedSubfolder);
        var relatedToPersonLink = Path.Combine(relatedFolder, relatedSubfolder,
            FolderTreeNaming.Sanitize(personName) + ".lnk");
        TryCreateShortcut(personFolderPath, relatedToPersonLink);
    }

    private void EnsureRelatedSubfolder(string relatedFolder, string subfolder)
    {
        try
        {
            _fs.CreateDirectory(Path.Combine(relatedFolder, subfolder));
        }
        catch (Exception ex)
        {
            _log.Error(ex, "RelationshipFolderMirror: failed to create subfolder {Subfolder} in {Folder}", subfolder, relatedFolder);
        }
    }

    private void TryCreateShortcut(string targetPath, string linkPath)
    {
        try
        {
            _shortcutCreator.Create(targetPath, linkPath);
        }
        catch (Exception ex)
        {
            _log.Error(ex, "RelationshipFolderMirror: failed to create shortcut {Link} → {Target}", linkPath, targetPath);
        }
    }

    private void TryDeleteShortcut(string linkPath)
    {
        try
        {
            if (_fs.FileExists(linkPath))
            {
                _fs.DeleteFile(linkPath);
            }
        }
        catch (Exception ex)
        {
            _log.Error(ex, "RelationshipFolderMirror: failed to delete shortcut {Link}", linkPath);
        }
    }
}
