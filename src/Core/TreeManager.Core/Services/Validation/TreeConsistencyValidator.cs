using System;
using System.Collections.Generic;
using System.Linq;
using TreeManager.Core.Abstractions.Validation;
using TreeManager.Core.Domain;
using TreeManager.Core.Validation;

namespace TreeManager.Core.Services.Validation;

public sealed class TreeConsistencyValidator : ITreeConsistencyValidator
{
    public IReadOnlyList<ValidationIssue> Validate(IReadOnlyDictionary<Guid, MeFile> people)
    {
        // 1. Detect stale references
        var stale = DetectStaleReferences(people);

        // 2. Detect cycles
        var cycles = DetectCycles(people);

        // 3. Detect one-sided relationships
        var oneSided = DetectOneSidedRelationships(people);

        // 4. Detect orphans
        var orphans = DetectOrphans(people);

        return [.. stale, .. cycles, .. oneSided, .. orphans];
    }

    private static List<ValidationIssue> DetectStaleReferences(IReadOnlyDictionary<Guid, MeFile> people)
    {
        var issues = new List<ValidationIssue>();
        var reported = new HashSet<(Guid, Guid)>();

        foreach (var (personId, person) in people)
        {
            foreach (var referencedId in AllLinks(person))
            {
                if (referencedId == Guid.Empty)
                {
                    continue;
                }

                if (!people.ContainsKey(referencedId))
                {
                    var key = (personId, referencedId);
                    if (reported.Add(key))
                    {
                        issues.Add(new ValidationIssue
                        {
                            Kind = ValidationIssueKind.StaleReference,
                            Subjects = [personId, referencedId],
                        });
                    }
                }
            }
        }

        return issues;
    }

    private static List<ValidationIssue> DetectCycles(IReadOnlyDictionary<Guid, MeFile> people)
    {
        var visited = new HashSet<Guid>();
        var detectedCycles = new List<List<Guid>>();

        foreach (var personId in people.Keys)
        {
            var recursionStack = new HashSet<Guid>();
            var cycleMembers = new List<Guid>();
            HasCycle(personId, people, visited, recursionStack, cycleMembers);

            if (cycleMembers.Count > 0)
            {
                if (!detectedCycles.Any(existing => IsEquivalentCycle(existing, cycleMembers)))
                {
                    detectedCycles.Add(cycleMembers);
                }
            }
        }

        return detectedCycles
            .Select(members => new ValidationIssue
            {
                Kind = ValidationIssueKind.Cycle,
                Subjects = members,
            })
            .ToList();
    }

    private static bool HasCycle(
        Guid personId,
        IReadOnlyDictionary<Guid, MeFile> people,
        HashSet<Guid> visited,
        HashSet<Guid> recursionStack,
        List<Guid> cycleMembers)
    {
        if (!visited.Add(personId))
        {
            // recursionStack separates diamond from true cycle
            return recursionStack.Contains(personId);
        }

        recursionStack.Add(personId);

        if (people.TryGetValue(personId, out var person))
        {
            foreach (var childId in person.ChildrenId)
            {
                if (childId == Guid.Empty)
                {
                    continue;
                }

                if (HasCycle(childId, people, visited, recursionStack, cycleMembers))
                {
                    cycleMembers.Add(personId);
                    return true;
                }
            }
        }

        recursionStack.Remove(personId);
        return false;
    }

    private static bool IsEquivalentCycle(List<Guid> existing, List<Guid> candidate)
    {
        if (existing.Count != candidate.Count)
        {
            return false;
        }

        var existingSet = new HashSet<Guid>(existing);
        return candidate.All(id => existingSet.Contains(id));
    }

    private static List<ValidationIssue> DetectOneSidedRelationships(IReadOnlyDictionary<Guid, MeFile> people)
    {
        var issues = new List<ValidationIssue>();

        foreach (var (personId, person) in people)
        {
            // Check parent direction: person claims a parent who doesn't list person as child
            foreach (var parentId in person.ParentsId)
            {
                if (parentId == Guid.Empty)
                {
                    continue;
                }

                if (!people.TryGetValue(parentId, out var parent))
                {
                    continue;
                }

                if (!parent.ChildrenId.Contains(personId))
                {
                    issues.Add(new ValidationIssue
                    {
                        Kind = ValidationIssueKind.OneSidedRelationship,
                        Subjects = [personId, parentId],
                    });
                }
            }

            // Check spouse direction: person claims a spouse who doesn't reciprocate
            foreach (var spouseId in person.SpouseId)
            {
                if (spouseId == Guid.Empty)
                {
                    continue;
                }

                if (!people.TryGetValue(spouseId, out var spouse))
                {
                    continue;
                }

                if (!spouse.SpouseId.Contains(personId))
                {
                    issues.Add(new ValidationIssue
                    {
                        Kind = ValidationIssueKind.OneSidedRelationship,
                        Subjects = [personId, spouseId],
                    });
                }
            }
        }

        return issues;
    }

    private static List<ValidationIssue> DetectOrphans(IReadOnlyDictionary<Guid, MeFile> people)
    {
        var issues = new List<ValidationIssue>();

        foreach (var (personId, person) in people)
        {
            var hasParent = person.ParentsId.Any(id => id != Guid.Empty);
            var hasChild = person.ChildrenId.Any(id => id != Guid.Empty);
            var hasSpouse = person.SpouseId.Any(id => id != Guid.Empty);

            if (!hasParent && !hasChild && !hasSpouse)
            {
                issues.Add(new ValidationIssue
                {
                    Kind = ValidationIssueKind.Orphan,
                    Subjects = [personId],
                });
            }
        }

        return issues;
    }

    private static IEnumerable<Guid> AllLinks(MeFile person)
    {
        foreach (var id in person.ParentsId) { yield return id; }
        foreach (var id in person.ChildrenId) { yield return id; }
        foreach (var id in person.SpouseId) { yield return id; }
    }
}
