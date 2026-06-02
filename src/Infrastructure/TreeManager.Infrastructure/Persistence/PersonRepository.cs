using System;
using System.Collections.Generic;
using System.Linq;
using TreeManager.Core.Abstractions.IO;
using TreeManager.Core.Abstractions.Persistence;
using TreeManager.Core.Domain;
using TreeManager.Core.Domain.Relationships;
using TreeManager.Core.Services;
using Serilog;

namespace TreeManager.Infrastructure.Persistence;

public sealed class PersonRepository : IPersonRepository
{
    private const string PeopleListFolderName = "Lista osób";

    private readonly IFileSystemFacade _fs;
    private readonly IMeFileProcessor _processor;
    private readonly ILogger _log;

    public PersonRepository(IFileSystemFacade fs, IMeFileProcessor processor, ILogger log)
    {
        ArgumentNullException.ThrowIfNull(fs);
        ArgumentNullException.ThrowIfNull(processor);
        ArgumentNullException.ThrowIfNull(log);
        _fs = fs;
        _processor = processor;
        _log = log;
    }

    public void Update(MeFile person, MeFile originalSnapshot, string rootPath)
    {
        ArgumentNullException.ThrowIfNull(person);
        ArgumentNullException.ThrowIfNull(originalSnapshot);
        ArgumentException.ThrowIfNullOrWhiteSpace(rootPath);

        if (person.PersonName != originalSnapshot.PersonName)
        {
            _log.Warning("PersonName changed from {Original} to {New}; folder rename deferred",
                originalSnapshot.PersonName, person.PersonName);
        }

        var personFolderPath = Path.Combine(rootPath, PeopleListFolderName, originalSnapshot.PersonName);
        var meFilePath = Path.Combine(personFolderPath, "me.json");

        _processor.WriteMeFile(meFilePath, person);

        var index = BuildIndex(rootPath);

        ApplyDeltaSync(person, originalSnapshot, index);

        if (person.PersonName != originalSnapshot.PersonName)
        {
            PropagateNameChange(person, index);
        }
    }

    private void PropagateNameChange(MeFile person, IReadOnlyDictionary<Guid, string> uuidToPath)
    {
        foreach (var kvp in uuidToPath)
        {
            if (kvp.Key == person.UniqueIdentifier)
            {
                continue;
            }

            try
            {
                var meFile = _processor.ReadMeFile(kvp.Value);
                var changed = false;

                var parentIdx = meFile.ParentsId.IndexOf(person.UniqueIdentifier);
                if (parentIdx >= 0)
                {
                    var updatedNames = meFile.Parents.ToList();
                    updatedNames[parentIdx] = person.PersonName;
                    meFile = meFile with { Parents = updatedNames };
                    changed = true;
                }

                var childIdx = meFile.ChildrenId.IndexOf(person.UniqueIdentifier);
                if (childIdx >= 0)
                {
                    var updatedNames = meFile.Children.ToList();
                    updatedNames[childIdx] = person.PersonName;
                    meFile = meFile with { Children = updatedNames };
                    changed = true;
                }

                var spouseIdx = meFile.SpouseId.IndexOf(person.UniqueIdentifier);
                if (spouseIdx >= 0)
                {
                    var updatedNames = meFile.Spouse.ToList();
                    updatedNames[spouseIdx] = person.PersonName;
                    meFile = meFile with { Spouse = updatedNames };
                    changed = true;
                }

                var siblingIdx = meFile.SiblingsId.IndexOf(person.UniqueIdentifier);
                if (siblingIdx >= 0)
                {
                    var updatedNames = meFile.Siblings.ToList();
                    updatedNames[siblingIdx] = person.PersonName;
                    meFile = meFile with { Siblings = updatedNames };
                    changed = true;
                }

                if (changed)
                {
                    _processor.WriteMeFile(kvp.Value, meFile);
                }
            }
            catch (Exception ex)
            {
                _log.Warning(ex, "Failed to propagate name change to {Path}", kvp.Value);
            }
        }
    }

    private void ApplyDeltaSync(MeFile person, MeFile snapshot, Dictionary<Guid, string> index)
    {
        var addedParents = person.ParentsId.Except(snapshot.ParentsId).ToList();
        var removedParents = snapshot.ParentsId.Except(person.ParentsId).ToList();
        var addedChildren = person.ChildrenId.Except(snapshot.ChildrenId).ToList();
        var removedChildren = snapshot.ChildrenId.Except(person.ChildrenId).ToList();
        var addedSpouses = person.SpouseId.Except(snapshot.SpouseId).ToList();
        var removedSpouses = snapshot.SpouseId.Except(person.SpouseId).ToList();
        var addedSiblings = person.SiblingsId.Except(snapshot.SiblingsId).ToList();
        var removedSiblings = snapshot.SiblingsId.Except(person.SiblingsId).ToList();

        SyncList(addedParents, person.UniqueIdentifier, person.PersonName, RelationshipRole.IsChildOf, index);
        SyncList(addedChildren, person.UniqueIdentifier, person.PersonName, RelationshipRole.IsParentOf, index);
        SyncList(addedSpouses, person.UniqueIdentifier, person.PersonName, RelationshipRole.IsSpouseOf, index);
        SyncList(addedSiblings, person.UniqueIdentifier, person.PersonName, RelationshipRole.IsSiblingOf, index);

        RemoveSyncList(removedParents, person.UniqueIdentifier, RelationshipRole.IsChildOf, index);
        RemoveSyncList(removedChildren, person.UniqueIdentifier, RelationshipRole.IsParentOf, index);
        RemoveSyncList(removedSpouses, person.UniqueIdentifier, RelationshipRole.IsSpouseOf, index);
        RemoveSyncList(removedSiblings, person.UniqueIdentifier, RelationshipRole.IsSiblingOf, index);
    }

    private void RemoveSyncList(
        List<Guid> relatedIds,
        Guid sourceId,
        RelationshipRole role,
        Dictionary<Guid, string> index)
    {
        foreach (var relatedId in relatedIds)
        {
            if (!index.TryGetValue(relatedId, out var relatedPath))
            {
                continue;
            }

            try
            {
                var original = _processor.ReadMeFile(relatedPath);
                var updated = RelationshipSyncService.RemoveBidirectionalSync(original, sourceId, role);
                if (!ReferenceEquals(original, updated))
                {
                    _processor.WriteMeFile(relatedPath, updated);
                }
            }
            catch (Exception ex)
            {
                _log.Warning(ex, "Failed to sync relationship to {Path}", relatedPath);
            }
        }
    }

    public void Create(MeFile person, string rootPath)
    {
        ArgumentNullException.ThrowIfNull(person);
        ArgumentException.ThrowIfNullOrWhiteSpace(rootPath);

        var personFolderPath = Path.Combine(rootPath, PeopleListFolderName, person.PersonName);
        var meFilePath = Path.Combine(personFolderPath, "me.json");

        _fs.CreateDirectory(personFolderPath);
        _processor.WriteMeFile(meFilePath, person);

        ApplyBidirSync(person, rootPath);
    }

    private void ApplyBidirSync(MeFile person, string rootPath)
    {
        var index = BuildIndex(rootPath);

        SyncList(person.ParentsId, person.UniqueIdentifier, person.PersonName, RelationshipRole.IsChildOf, index);
        SyncList(person.ChildrenId, person.UniqueIdentifier, person.PersonName, RelationshipRole.IsParentOf, index);
        SyncList(person.SpouseId, person.UniqueIdentifier, person.PersonName, RelationshipRole.IsSpouseOf, index);
        SyncList(person.SiblingsId, person.UniqueIdentifier, person.PersonName, RelationshipRole.IsSiblingOf, index);
    }

    private Dictionary<Guid, string> BuildIndex(string rootPath)
    {
        var index = new Dictionary<Guid, string>();
        foreach (var path in _processor.ScanMeFiles(rootPath))
        {
            try
            {
                var mf = _processor.ReadMeFile(path);
                index[mf.UniqueIdentifier] = path;
            }
            catch (Exception ex)
            {
                _log.Warning(ex, "Failed to index me.json at {Path}", path);
            }
        }
        return index;
    }

    private void SyncList(
        List<Guid> relatedIds,
        Guid sourceId,
        string sourceName,
        RelationshipRole role,
        Dictionary<Guid, string> index)
    {
        foreach (var relatedId in relatedIds)
        {
            if (!index.TryGetValue(relatedId, out var relatedPath))
            {
                continue;
            }

            try
            {
                var original = _processor.ReadMeFile(relatedPath);
                var updated = RelationshipSyncService.ApplyBidirectionalSync(original, sourceId, sourceName, role);
                if (!ReferenceEquals(original, updated))
                {
                    _processor.WriteMeFile(relatedPath, updated);
                }
            }
            catch (Exception ex)
            {
                _log.Warning(ex, "Failed to sync relationship to {Path}", relatedPath);
            }
        }
    }
}
