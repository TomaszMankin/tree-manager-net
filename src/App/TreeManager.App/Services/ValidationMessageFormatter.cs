using System;
using System.Collections.Generic;
using TreeManager.Core.Domain;
using TreeManager.Core.Services;
using TreeManager.Core.Validation;

namespace TreeManager.App.Services;

public sealed class ValidationMessageFormatter : IValidationMessageFormatter
{
    private const string MissingFolderSentinel = "(brak folderu)";

    public IReadOnlyList<string> Format(
        IReadOnlyList<ValidationIssue> issues,
        IReadOnlyDictionary<Guid, MeFile> people)
    {
        if (issues.Count == 0)
        {
            return [];
        }

        var messages = new List<string>(issues.Count);

        foreach (var issue in issues)
        {
            messages.Add(BuildMessage(issue, people));
        }

        return messages;
    }

    private static string BuildMessage(ValidationIssue issue, IReadOnlyDictionary<Guid, MeFile> people)
    {
        switch (issue.Kind)
        {
            case ValidationIssueKind.Cycle:
                return BuildCycleMessage(issue, people);

            case ValidationIssueKind.OneSidedRelationship:
                return BuildOneSidedMessage(issue, people);

            case ValidationIssueKind.Orphan:
                return BuildOrphanMessage(issue, people);

            case ValidationIssueKind.StaleReference:
                return BuildStaleReferenceMessage(issue, people);

            default:
                throw new ArgumentOutOfRangeException(nameof(issue), issue.Kind, "Unhandled ValidationIssueKind.");
        }
    }

    private static string BuildCycleMessage(ValidationIssue issue, IReadOnlyDictionary<Guid, MeFile> people)
    {
        var names = new List<string>(issue.Subjects.Count);
        foreach (var id in issue.Subjects)
        {
            names.Add(ResolveName(id, people));
        }
        return "Cykl: " + string.Join(" → ", names);
    }

    private static string BuildOneSidedMessage(ValidationIssue issue, IReadOnlyDictionary<Guid, MeFile> people)
    {
        var claimantName = ResolveName(issue.Subjects[0], people);
        var claimedName = ResolveName(issue.Subjects[1], people);
        return $"Jednostronna relacja: {claimantName} → {claimedName}";
    }

    private static string BuildOrphanMessage(ValidationIssue issue, IReadOnlyDictionary<Guid, MeFile> people)
    {
        var name = ResolveName(issue.Subjects[0], people);
        return $"Osoba bez powiązań: {name}";
    }

    private static string BuildStaleReferenceMessage(ValidationIssue issue, IReadOnlyDictionary<Guid, MeFile> people)
    {
        var referrerName = ResolveName(issue.Subjects[0], people);
        var missingId = issue.Subjects[1];
        return $"Brakująca osoba: UUID {missingId} wskazana przez {referrerName} {MissingFolderSentinel}";
    }

    private static string ResolveName(Guid id, IReadOnlyDictionary<Guid, MeFile> people)
    {
        if (!people.TryGetValue(id, out var meFile))
        {
            return MissingFolderSentinel;
        }

        var fullName = FolderTreeNaming.FullName(meFile);

        if (string.IsNullOrWhiteSpace(meFile.FirstName) && string.IsNullOrWhiteSpace(meFile.LastName))
        {
            return $"{fullName} [{id.ToString()[..8]}]";
        }

        return fullName;
    }
}
