using System;
using System.Collections.Generic;
using TreeManager.Core.Domain;

namespace TreeManager.Core.Abstractions.Services;

public interface IRelationshipFolderMirror
{
    void Mirror(
        MeFile person,
        string personFolderPath,
        IReadOnlyDictionary<Guid, string> relatedFolderPathsByUid);

    void ApplyDelta(
        MeFile person,
        string personFolderPath,
        IReadOnlyDictionary<Guid, string> relatedFolderPathsByUid,
        IReadOnlyList<Guid> addedParents,
        IReadOnlyList<Guid> removedParents,
        IReadOnlyList<Guid> addedChildren,
        IReadOnlyList<Guid> removedChildren,
        IReadOnlyList<Guid> addedSpouses,
        IReadOnlyList<Guid> removedSpouses,
        IReadOnlyList<Guid> addedSiblings,
        IReadOnlyList<Guid> removedSiblings);
}
