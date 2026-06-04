using System.Collections.Generic;
using TreeManager.Core.Domain;

namespace TreeManager.Core.Abstractions.Persistence;

public interface IDraftRepository
{
    void SaveDraft(MeFile draft, string rootPath);
    IReadOnlyList<PersonSummary> GetAllDrafts(string rootPath);
    MeFile ReadDraft(string rootPath, string folderName);
    void DeleteDraft(string rootPath, string folderName);
}
