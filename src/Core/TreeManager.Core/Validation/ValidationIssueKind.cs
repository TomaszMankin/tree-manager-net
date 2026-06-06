namespace TreeManager.Core.Validation;

public enum ValidationIssueKind
{
    Cycle,
    OneSidedRelationship,
    Orphan,
    StaleReference,
}
