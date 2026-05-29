using TreeManager.Core.Domain;

namespace TreeManager.Core.Abstractions.Persistence;

public interface IPersonDirectoryService
{
    IReadOnlyList<PersonSummary> GetAll(string rootPath);
}
