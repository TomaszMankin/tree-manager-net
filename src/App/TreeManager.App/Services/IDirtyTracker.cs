using TreeManager.Core.Domain;

namespace TreeManager.App.Services;

public interface IDirtyTracker
{
    bool IsDirty(MeFile snapshot, MeFile current);
}
