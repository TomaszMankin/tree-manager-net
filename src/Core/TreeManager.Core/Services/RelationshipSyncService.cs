using System;
using System.Collections.Generic;
using TreeManager.Core.Domain;
using TreeManager.Core.Domain.Relationships;

namespace TreeManager.Core.Services;

public static class RelationshipSyncService
{
    /// <summary>
    /// Applies one bidirectional sync step to <paramref name="target"/>.
    /// <paramref name="roleSourceHoldsForTarget"/> is the role the person-being-saved
    /// holds relative to <paramref name="target"/> (e.g. IsChildOf = saved person is a child of target).
    /// Returns the same <paramref name="target"/> reference when no change is needed (idempotent / type mismatch).
    /// Returns a new record when sync was applied.
    /// </summary>
    public static MeFile ApplyBidirectionalSync(
        MeFile target,
        Guid sourceId,
        string sourceName,
        RelationshipRole roleSourceHoldsForTarget)
    {
        ArgumentNullException.ThrowIfNull(target);

        if (IsInAnyOtherList(target, sourceId, roleSourceHoldsForTarget))
        {
            return target;
        }

        switch (roleSourceHoldsForTarget)
        {
            case RelationshipRole.IsChildOf:
                // Saved person is a child of target → target should list saved person in its Children
                if (target.ChildrenId.Contains(sourceId))
                {
                    return target;
                }
                return target with
                {
                    ChildrenId = AppendId(target.ChildrenId, sourceId),
                    Children = AppendName(target.Children, sourceName),
                };

            case RelationshipRole.IsParentOf:
                // Saved person is a parent of target → target should list saved person in its Parents
                if (target.ParentsId.Contains(sourceId))
                {
                    return target;
                }
                return target with
                {
                    ParentsId = AppendId(target.ParentsId, sourceId),
                    Parents = AppendName(target.Parents, sourceName),
                };

            case RelationshipRole.IsSpouseOf:
                if (target.SpouseId.Contains(sourceId))
                {
                    return target;
                }
                return target with
                {
                    SpouseId = AppendId(target.SpouseId, sourceId),
                    Spouse = AppendName(target.Spouse, sourceName),
                };

            case RelationshipRole.IsSiblingOf:
                if (target.SiblingsId.Contains(sourceId))
                {
                    return target;
                }
                return target with
                {
                    SiblingsId = AppendId(target.SiblingsId, sourceId),
                    Siblings = AppendName(target.Siblings, sourceName),
                };

            default:
                return target;
        }
    }

    private static bool IsInAnyOtherList(MeFile target, Guid sourceId, RelationshipRole role)
    {
        var correctList = GetCorrectList(role);

        if (correctList != ListKind.Children && target.ChildrenId.Contains(sourceId)) { return true; }
        if (correctList != ListKind.Parents && target.ParentsId.Contains(sourceId)) { return true; }
        if (correctList != ListKind.Spouse && target.SpouseId.Contains(sourceId)) { return true; }
        if (correctList != ListKind.Siblings && target.SiblingsId.Contains(sourceId)) { return true; }

        return false;
    }

    private static ListKind GetCorrectList(RelationshipRole role)
    {
        return role switch
        {
            RelationshipRole.IsChildOf => ListKind.Children,
            RelationshipRole.IsParentOf => ListKind.Parents,
            RelationshipRole.IsSpouseOf => ListKind.Spouse,
            RelationshipRole.IsSiblingOf => ListKind.Siblings,
            _ => ListKind.None,
        };
    }

    private static List<Guid> AppendId(List<Guid> existing, Guid id)
    {
        var result = new List<Guid>(existing) { id };
        return result;
    }

    private static List<string> AppendName(List<string> existing, string name)
    {
        var result = new List<string>(existing) { name };
        return result;
    }

    private enum ListKind { None, Children, Parents, Spouse, Siblings }
}
