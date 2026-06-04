using TreeManager.Core.Domain;

namespace TreeManager.Core.Abstractions.Persistence;

public interface IPersonRepository
{
    void Create(MeFile person, string rootPath);
    void Create(MeFile person, string rootPath, string folderName);
    void Update(MeFile person, MeFile originalSnapshot, string rootPath);
}
