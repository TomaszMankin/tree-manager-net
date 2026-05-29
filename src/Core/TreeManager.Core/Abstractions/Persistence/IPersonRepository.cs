using TreeManager.Core.Domain;

namespace TreeManager.Core.Abstractions.Persistence;

public interface IPersonRepository
{
    void Create(MeFile person, string rootPath);
}
