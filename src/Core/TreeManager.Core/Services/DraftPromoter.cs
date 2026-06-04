using System;
using System.IO;
using Serilog;
using TreeManager.Core.Abstractions.IO;
using TreeManager.Core.Abstractions.Persistence;
using TreeManager.Core.Domain;

namespace TreeManager.Core.Services;

public sealed class DraftPromoter : IDraftPromoter
{
    private const string PeopleListFolderName = "Lista osób";

    private readonly IPersonRepository _personRepository;
    private readonly IDraftRepository _draftRepository;
    private readonly IFileSystemFacade _fs;
    private readonly ILogger _log;

    public DraftPromoter(
        IPersonRepository personRepository,
        IDraftRepository draftRepository,
        IFileSystemFacade fs,
        ILogger log)
    {
        ArgumentNullException.ThrowIfNull(personRepository);
        ArgumentNullException.ThrowIfNull(draftRepository);
        ArgumentNullException.ThrowIfNull(fs);
        ArgumentNullException.ThrowIfNull(log);
        _personRepository = personRepository;
        _draftRepository = draftRepository;
        _fs = fs;
        _log = log;
    }

    public void Promote(MeFile draft, string rootPath)
    {
        ArgumentNullException.ThrowIfNull(draft);
        ArgumentException.ThrowIfNullOrWhiteSpace(rootPath);

        var folderName = ResolveUniqueFolderName(draft.PersonName, rootPath);
        var destinationFolder = Path.Combine(rootPath, PeopleListFolderName, folderName);

        try
        {
            _personRepository.Create(draft, rootPath, folderName);
        }
        catch (Exception ex)
        {
            _log.Error(ex, "Promote failed for {PersonName}; attempting partial destination cleanup", draft.PersonName);

            if (_fs.DirectoryExists(destinationFolder))
            {
                try
                {
                    _fs.DeleteDirectory(destinationFolder, true);
                }
                catch (Exception cleanupEx)
                {
                    _log.Warning(cleanupEx, "Failed to clean up partial destination {Folder}", destinationFolder);
                }
            }

            throw;
        }

        _draftRepository.DeleteDraft(rootPath, draft.PersonName);
        _log.Information("Draft promoted: {PersonName} -> {FolderName}", draft.PersonName, folderName);
    }

    private string ResolveUniqueFolderName(string originalName, string rootPath)
    {
        var candidateName = originalName;
        var suffix = 2;

        while (_fs.DirectoryExists(Path.Combine(rootPath, PeopleListFolderName, candidateName)))
        {
            candidateName = originalName + " (" + suffix + ")";
            suffix++;
        }

        return candidateName;
    }
}
