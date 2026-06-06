using System;
using System.Collections.Generic;

namespace TreeManager.Core.Validation;

public sealed record ValidationIssue
{
    public ValidationIssueKind Kind { get; init; }
    public IReadOnlyList<Guid> Subjects { get; init; } = [];
}
