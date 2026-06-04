using System;
using System.Collections.Generic;
using System.Linq;
using Serilog;
using TreeManager.Core.Abstractions.IO;
using TreeManager.Core.Abstractions.Persistence;
using TreeManager.Core.Domain;

namespace TreeManager.Infrastructure.Persistence;

public sealed class DraftRepository : IDraftRepository
{
    private const string DraftFolderName = "Poczekalnia";

    private readonly IFileSystemFacade _fs;
    private readonly IMeFileProcessor _processor;
    private readonly ILogger _log;

    public DraftRepository(IFileSystemFacade fs, IMeFileProcessor processor, ILogger log)
    {
        ArgumentNullException.ThrowIfNull(fs);
        ArgumentNullException.ThrowIfNull(processor);
        ArgumentNullException.ThrowIfNull(log);
        _fs = fs;
        _processor = processor;
        _log = log;
    }

    public void SaveDraft(MeFile draft, string rootPath)
    {
        ArgumentNullException.ThrowIfNull(draft);
        ArgumentException.ThrowIfNullOrWhiteSpace(rootPath);

        var draftFolder = Path.Combine(rootPath, DraftFolderName, draft.PersonName);
        var meFilePath = Path.Combine(draftFolder, "me.json");

        _fs.CreateDirectory(draftFolder);
        _processor.WriteMeFile(meFilePath, draft);

        _log.Information("Draft saved to {Path}", meFilePath);
    }

    public IReadOnlyList<PersonSummary> GetAllDrafts(string rootPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rootPath);

        var draftRoot = Path.Combine(rootPath, DraftFolderName);
        if (!_fs.DirectoryExists(draftRoot))
        {
            return [];
        }

        var results = new List<PersonSummary>();
        foreach (var folder in _fs.EnumerateDirectories(draftRoot))
        {
            var meFilePath = Path.Combine(folder, "me.json");
            if (!_fs.FileExists(meFilePath))
            {
                continue;
            }

            try
            {
                var mf = _processor.ReadMeFile(meFilePath);
                results.Add(new PersonSummary(mf.UniqueIdentifier, mf.PersonName ?? string.Empty));
            }
            catch (Exception ex)
            {
                _log.Warning(ex, "Failed to read draft at {Path}", meFilePath);
            }
        }

        return results;
    }

    public MeFile ReadDraft(string rootPath, string folderName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rootPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(folderName);

        var meFilePath = Path.Combine(rootPath, DraftFolderName, folderName, "me.json");
        return _processor.ReadMeFile(meFilePath);
    }

    public void DeleteDraft(string rootPath, string folderName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rootPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(folderName);

        var draftFolder = Path.Combine(rootPath, DraftFolderName, folderName);
        _fs.DeleteDirectory(draftFolder, true);

        _log.Information("Draft deleted: {Folder}", draftFolder);
    }
}
