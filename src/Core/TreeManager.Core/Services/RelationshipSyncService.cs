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
                return AddChild(target, sourceId, sourceName);

            case RelationshipRole.IsParentOf:
                return AddParent(target, sourceId, sourceName);

            case RelationshipRole.IsSpouseOf:
                return AddSpouse(target, sourceId, sourceName);

            case RelationshipRole.IsSiblingOf:
                return AddSibling(target, sourceId, sourceName);

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

    private static MeFile AddChild(MeFile target, Guid id, string name)
    {
        if (target.ChildrenId.Contains(id)) { return target; }
        return target with
        {
            ChildrenId = AppendId(target.ChildrenId, id),
            Children = AppendName(target.Children, name),
        };
    }

    private static MeFile AddParent(MeFile target, Guid id, string name)
    {
        if (target.ParentsId.Contains(id)) { return target; }
        return target with
        {
            ParentsId = AppendId(target.ParentsId, id),
            Parents = AppendName(target.Parents, name),
        };
    }

    private static MeFile AddSpouse(MeFile target, Guid id, string name)
    {
        if (target.SpouseId.Contains(id)) { return target; }
        return target with
        {
            SpouseId = AppendId(target.SpouseId, id),
            Spouse = AppendName(target.Spouse, name),
        };
    }

    private static MeFile AddSibling(MeFile target, Guid id, string name)
    {
        if (target.SiblingsId.Contains(id)) { return target; }
        return target with
        {
            SiblingsId = AppendId(target.SiblingsId, id),
            Siblings = AppendName(target.Siblings, name),
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

    /// <summary>
    /// Removes <paramref name="sourceId"/> from <paramref name="target"/> according to
    /// <paramref name="roleSourceHoldsForTarget"/>. Returns the same reference when nothing changed;
    /// returns a new record when an entry was removed.
    /// </summary>
    public static MeFile RemoveBidirectionalSync(
        MeFile target,
        Guid sourceId,
        RelationshipRole roleSourceHoldsForTarget)
    {
        ArgumentNullException.ThrowIfNull(target);

        switch (roleSourceHoldsForTarget)
        {
            case RelationshipRole.IsChildOf:
                return RemoveChild(target, sourceId);

            case RelationshipRole.IsParentOf:
                return RemoveParent(target, sourceId);

            case RelationshipRole.IsSpouseOf:
                return RemoveSpouse(target, sourceId);

            case RelationshipRole.IsSiblingOf:
                return RemoveSibling(target, sourceId);

            default:
                return target;
        }
    }

    private static MeFile RemoveChild(MeFile target, Guid id)
    {
        var idx = target.ChildrenId.IndexOf(id);
        if (idx < 0) { return target; }
        return target with
        {
            ChildrenId = RemoveAt(target.ChildrenId, idx),
            Children = RemoveAt(target.Children, idx),
        };
    }

    private static MeFile RemoveParent(MeFile target, Guid id)
    {
        var idx = target.ParentsId.IndexOf(id);
        if (idx < 0) { return target; }
        return target with
        {
            ParentsId = RemoveAt(target.ParentsId, idx),
            Parents = RemoveAt(target.Parents, idx),
        };
    }

    private static MeFile RemoveSpouse(MeFile target, Guid id)
    {
        var idx = target.SpouseId.IndexOf(id);
        if (idx < 0) { return target; }
        return target with
        {
            SpouseId = RemoveAt(target.SpouseId, idx),
            Spouse = RemoveAt(target.Spouse, idx),
        };
    }

    private static MeFile RemoveSibling(MeFile target, Guid id)
    {
        var idx = target.SiblingsId.IndexOf(id);
        if (idx < 0) { return target; }
        return target with
        {
            SiblingsId = RemoveAt(target.SiblingsId, idx),
            Siblings = RemoveAt(target.Siblings, idx),
        };
    }

    private static List<T> RemoveAt<T>(List<T> source, int index)
    {
        var result = new List<T>(source);
        result.RemoveAt(index);
        return result;
    }

    private enum ListKind { None, Children, Parents, Spouse, Siblings }
}
