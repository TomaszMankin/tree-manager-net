using System;

namespace TreeManager.Core.Domain;

/// <summary>Resolved membership entry — one person in one rebuilt Drzewo folder.</summary>
public sealed record FolderTreeMember(
    Guid Uid,
    int Generation,
    int CoupleIndex,
    int TotalCouplesInGeneration,
    string Role,
    string Gender,
    string FullName,
    string TargetLocation);
