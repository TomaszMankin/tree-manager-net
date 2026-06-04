using TreeManager.Core.Domain;

namespace TreeManager.Core.Abstractions.Persistence;

public interface IDraftPromoter
{
    void Promote(MeFile draft, string rootPath);
}
