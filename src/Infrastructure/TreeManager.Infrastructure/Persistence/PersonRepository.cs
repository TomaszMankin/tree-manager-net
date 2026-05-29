using System;
using System.Collections.Generic;
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
