using System;
using System.Collections.Generic;

namespace TreeManager.Core.Domain;

/// <summary>Domain record for one lineage folder — surname, seeding contributor, and ordered member UIDs.</summary>
public sealed record LineageGroup(
    string Surname,
    Guid ContributorUid,
    IReadOnlyList<Guid> MemberUids);
