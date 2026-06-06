using System;
using System.Collections.Generic;
using TreeManager.Core.Domain;
using TreeManager.Core.Validation;

namespace TreeManager.Core.Abstractions.Validation;

/// <summary>
/// Validates the structural consistency of an in-memory person map.
/// Never throws on data problems — all findings are returned as a list.
/// Subjects ordering contract:
///   Cycle — all node GUIDs on the detected cycle, DFS-discovery order. At least one entry.
///   OneSidedRelationship — exactly [claimant, claimed].
///   Orphan — exactly [personId].
///   StaleReference — exactly [referrer, missingId].
/// </summary>
public interface ITreeConsistencyValidator
{
    IReadOnlyList<ValidationIssue> Validate(IReadOnlyDictionary<Guid, MeFile> people);
}
