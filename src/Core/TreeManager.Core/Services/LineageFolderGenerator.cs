using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Serilog;
using TreeManager.Core.Abstractions.IO;
using TreeManager.Core.Abstractions.Persistence;
using TreeManager.Core.Abstractions.Services;
using TreeManager.Core.Abstractions.Shell;
using TreeManager.Core.Domain;

namespace TreeManager.Core.Services;

/// <summary>Generates the Rody lineage-folder view using contributor-seeded, full-bloodline ancestor walk.</summary>
public sealed class LineageFolderGenerator : ILineageFolderGenerator
{
    private const string OutputFolderName = "Rody";
    private const string UnknownSentinel = "(nieznane)";

    private readonly IMeFileProcessor _processor;
    private readonly IFolderTreeGenerator _folderTreeGenerator;
    private readonly IShortcutCreator _shortcutCreator;
    private readonly IFileSystemFacade _fs;
    private readonly ILogger _log;

    public LineageFolderGenerator(
        IMeFileProcessor processor,
        IFolderTreeGenerator folderTreeGenerator,
        IShortcutCreator shortcutCreator,
        IFileSystemFacade fs,
        ILogger log)
    {
        _processor = processor;
        _folderTreeGenerator = folderTreeGenerator;
        _shortcutCreator = shortcutCreator;
        _fs = fs;
        _log = log;
    }

    #region ILineageFolderGenerator

    public (int Written, IReadOnlyList<string> Log) Generate(string rootPath, Guid rootPersonId)
    {
        // 1. Scan people
        var (peopleByUid, scanLog) = ScanPeople(rootPath);

        // 2. Compute Drzewo membership for token reuse
        var (drzewoByUid, drzewoLog) = ComputeDrzewoMembership(rootPersonId, peopleByUid);

        // 3. Compute lineages (throws TreeIntegrityException on corruption)
        var (lineages, lineageLog) = ComputeLineages(rootPersonId, peopleByUid);

        var buildLog = new List<string>(scanLog.Count + drzewoLog.Count + lineageLog.Count);
        buildLog.AddRange(scanLog);
        buildLog.AddRange(drzewoLog);
        buildLog.AddRange(lineageLog);

        // 4. Wipe and recreate Rody/
        WipeRodyFolder(rootPath);

        // 5. Write shortcuts
        var written = WriteShortcuts(rootPath, lineages, peopleByUid, drzewoByUid, buildLog);

        return (written, buildLog);
    }

    #endregion

    #region ComputeLineages

    public (IReadOnlyDictionary<string, LineageGroup>, IReadOnlyList<string>) ComputeLineages(
        Guid rootPersonId,
        IReadOnlyDictionary<Guid, MeFile> peopleByUid)
    {
        var log = new List<string>();
        var result = new Dictionary<string, LineageGroup>(StringComparer.Ordinal);

        // 1. Validate root
        if (!peopleByUid.TryGetValue(rootPersonId, out var root))
        {
            log.Add($"ERROR: root person {rootPersonId} not found in map.");
            return (result, log);
        }

        // 2. Check bidirectional integrity across all people (throws TreeIntegrityException on violation)
        CheckBidirectionalIntegrity(peopleByUid);

        // 3. Build universal members — DFS down from root via ChildrenId
        var universalMembers = BuildUniversalMemberList(root, rootPersonId, peopleByUid);

        // 4. Collect contributors — root.ParentsId + per-spouse: spouse.ParentsId
        var contributors = CollectContributors(root, peopleByUid, log);

        // 5. Resolve folder keys with two-pass clash detection
        var folderKeys = ResolveFolderKeys(contributors, peopleByUid, log);

        // 6. Build one lineage group per resolved folder key
        BuildLineageGroups(folderKeys, universalMembers, peopleByUid, log, result);

        return (result, log);
    }

    #endregion

    #region Generate helpers

    private (IReadOnlyDictionary<Guid, MeFile> PeopleByUid, List<string> Log) ScanPeople(string rootPath)
    {
        var peopleByUid = new Dictionary<Guid, MeFile>();
        var scanLog = new List<string>();

        foreach (var meFilePath in _processor.ScanMeFiles(rootPath))
        {
            try
            {
                var meFile = _processor.ReadMeFile(meFilePath);
                if (meFile.UniqueIdentifier != Guid.Empty)
                {
                    peopleByUid[meFile.UniqueIdentifier] = meFile;
                }
            }
            catch (Exception ex)
            {
                _log.Error(ex, "Generate: failed to read {Path}", meFilePath);
                scanLog.Add($"READ_ERROR: {meFilePath} — {ex.Message}");
            }
        }

        return (peopleByUid, scanLog);
    }

    private (IReadOnlyDictionary<Guid, FolderTreeMember> DrzewoByUid, IReadOnlyList<string> Log)
        ComputeDrzewoMembership(Guid rootPersonId, IReadOnlyDictionary<Guid, MeFile> peopleByUid)
    {
        var (drzewoMembers, drzewoLog) = _folderTreeGenerator.ComputeMembership(rootPersonId, peopleByUid);
        var drzewoByUid = drzewoMembers.ToDictionary(m => m.Uid);
        return (drzewoByUid, drzewoLog);
    }

    private void WipeRodyFolder(string rootPath)
    {
        var rodyPath = Path.Combine(rootPath, OutputFolderName);
        _fs.CreateDirectory(rodyPath);

        foreach (var filePath in _fs.EnumerateFiles(rodyPath, "*").ToList())
        {
            _fs.DeleteFile(filePath);
        }

        foreach (var dirPath in _fs.EnumerateDirectories(rodyPath).ToList())
        {
            _fs.DeleteDirectory(dirPath, recursive: true);
        }
    }

    private int WriteShortcuts(
        string rootPath,
        IReadOnlyDictionary<string, LineageGroup> lineages,
        IReadOnlyDictionary<Guid, MeFile> peopleByUid,
        IReadOnlyDictionary<Guid, FolderTreeMember> drzewoByUid,
        List<string> buildLog)
    {
        var rodyPath = Path.Combine(rootPath, OutputFolderName);
        int written = 0;

        foreach (var folderKey in lineages.Keys.OrderBy(s => s, StringComparer.Ordinal))
        {
            var group = lineages[folderKey];
            var subDir = Path.Combine(rodyPath, FolderTreeNaming.Sanitize(folderKey));
            _fs.CreateDirectory(subDir);

            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var memberUid in group.MemberUids)
            {
                if (!peopleByUid.TryGetValue(memberUid, out var memberFile))
                {
                    buildLog.Add($"SKIP_MEMBER: {memberUid} not in peopleByUid map.");
                    continue;
                }

                if (string.IsNullOrEmpty(memberFile.Location))
                {
                    buildLog.Add($"SKIP_MEMBER: {memberUid} ({FolderTreeNaming.FullName(memberFile)}) has empty Location.");
                    continue;
                }

                string baseFilename;
                if (drzewoByUid.TryGetValue(memberUid, out var drzewoMember))
                {
                    baseFilename = FolderTreeNaming.RenderFilename(drzewoMember);
                }
                else
                {
                    buildLog.Add($"DRZEWO_FALLBACK: {memberUid} not in Drzewo membership; using plain name.");
                    baseFilename = FolderTreeNaming.Sanitize(FolderTreeNaming.FullName(memberFile)) + ".lnk";
                }

                var filename = FolderTreeNaming.Deduplicate(baseFilename, seen);
                seen.Add(filename);

                var lnkPath = Path.Combine(subDir, filename);
                try
                {
                    _shortcutCreator.Create(memberFile.Location, lnkPath);
                    written++;
                }
                catch (Exception ex)
                {
                    _log.Error(ex, "Generate: SHORTCUT failed for {Name}", FolderTreeNaming.FullName(memberFile));
                    buildLog.Add($"SHORTCUT_ERROR: failed to create shortcut for {FolderTreeNaming.FullName(memberFile)} — {ex.Message}");
                }
            }
        }

        return written;
    }

    #endregion

    #region ComputeLineages helpers

    private static void CheckBidirectionalIntegrity(IReadOnlyDictionary<Guid, MeFile> peopleByUid)
    {
        foreach (var (personUid, person) in peopleByUid)
        {
            foreach (var parentUid in person.ParentsId)
            {
                if (parentUid == Guid.Empty)
                {
                    continue;
                }

                if (!peopleByUid.TryGetValue(parentUid, out var parent))
                {
                    continue;
                }

                if (!parent.ChildrenId.Contains(personUid))
                {
                    throw new TreeIntegrityException(
                        $"Broken bidirectional reference: {personUid} ({person.FirstName} {person.LastName}) lists {parentUid} as parent, but parent does not list {personUid} as child.");
                }
            }
        }
    }

    private static List<Guid> BuildUniversalMemberList(
        MeFile root,
        Guid rootPersonId,
        IReadOnlyDictionary<Guid, MeFile> peopleByUid)
    {
        var universalMembers = new List<Guid>();
        var universalSeen = new HashSet<Guid>();
        BuildUniversalMembers(root, rootPersonId, peopleByUid, universalMembers, universalSeen);
        return universalMembers;
    }

    private static List<Guid> CollectContributors(
        MeFile root,
        IReadOnlyDictionary<Guid, MeFile> peopleByUid,
        List<string> log)
    {
        var contributors = new List<Guid>();

        foreach (var parentUid in root.ParentsId)
        {
            if (parentUid == Guid.Empty)
            {
                continue;
            }

            contributors.Add(parentUid);
        }

        foreach (var spouseUid in root.SpouseId)
        {
            if (spouseUid == Guid.Empty)
            {
                continue;
            }

            if (!peopleByUid.TryGetValue(spouseUid, out var spouse))
            {
                log.Add($"SKIP: root spouse {spouseUid} not in map.");
                continue;
            }

            foreach (var spParentUid in spouse.ParentsId)
            {
                if (spParentUid == Guid.Empty)
                {
                    continue;
                }

                contributors.Add(spParentUid);
            }
        }

        return contributors;
    }

    private static Dictionary<Guid, string> ResolveFolderKeys(
        List<Guid> contributors,
        IReadOnlyDictionary<Guid, MeFile> peopleByUid,
        List<string> log)
    {
        // Pass 1: compute raw candidate key for each contributor that exists in the map
        var candidateKeys = new Dictionary<Guid, string>();

        foreach (var contributorUid in contributors)
        {
            if (!peopleByUid.TryGetValue(contributorUid, out var contributor))
            {
                log.Add($"SKIP_CONTRIBUTOR: {contributorUid} not in map.");
                continue;
            }

            var key = RawSurnameKey(contributor);
            candidateKeys[contributorUid] = key;
        }

        // Pass 2: detect clashes; escalate both parties to full display name
        var keyFrequency = candidateKeys.Values
            .GroupBy(k => k, StringComparer.Ordinal)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToHashSet(StringComparer.Ordinal);

        var folderKeys = new Dictionary<Guid, string>();

        foreach (var (uid, rawKey) in candidateKeys)
        {
            if (keyFrequency.Contains(rawKey))
            {
                folderKeys[uid] = FolderTreeNaming.FullName(peopleByUid[uid]).Trim();
            }
            else
            {
                folderKeys[uid] = rawKey;
            }
        }

        return folderKeys;
    }

    private static void BuildLineageGroups(
        Dictionary<Guid, string> folderKeys,
        List<Guid> universalMembers,
        IReadOnlyDictionary<Guid, MeFile> peopleByUid,
        List<string> log,
        Dictionary<string, LineageGroup> result)
    {
        foreach (var (contributorUid, folderKey) in folderKeys)
        {
            if (result.ContainsKey(folderKey))
            {
                log.Add($"COLLISION: folder key '{folderKey}' already exists; contributor {contributorUid} dropped.");
                continue;
            }

            if (!peopleByUid.TryGetValue(contributorUid, out var contributor))
            {
                continue;
            }

            var memberList = new List<Guid>();
            var memberSeen = new HashSet<Guid>();

            // a. Universal members
            foreach (var uid in universalMembers)
            {
                if (memberSeen.Add(uid))
                {
                    memberList.Add(uid);
                }
            }

            // b. Contributor
            if (memberSeen.Add(contributorUid))
            {
                memberList.Add(contributorUid);
            }

            // c. Contributor's spouses (each as leaf)
            foreach (var spUid in contributor.SpouseId)
            {
                if (spUid != Guid.Empty && memberSeen.Add(spUid))
                {
                    memberList.Add(spUid);
                }
            }

            // d. Full ancestor walk (no surname gate)
            WalkAncestors(contributor, peopleByUid, memberList, memberSeen, log);

            result[folderKey] = new LineageGroup(folderKey, contributorUid, memberList);
        }
    }

    private static string RawSurnameKey(MeFile person)
    {
        if (person.HasMaidenName)
        {
            var maiden = person.MaidenName.Trim();
            if (!string.IsNullOrEmpty(maiden) && maiden != UnknownSentinel)
            {
                return maiden;
            }
        }

        var last = person.LastName.Trim();
        if (!string.IsNullOrEmpty(last) && last != UnknownSentinel)
        {
            return last;
        }

        return FolderTreeNaming.FullName(person).Trim();
    }

    #endregion

    #region Shared helpers

    private static void BuildUniversalMembers(
        MeFile person,
        Guid personUid,
        IReadOnlyDictionary<Guid, MeFile> peopleByUid,
        List<Guid> members,
        HashSet<Guid> seen)
    {
        if (!seen.Add(personUid))
        {
            return;
        }

        members.Add(personUid);

        foreach (var spUid in person.SpouseId)
        {
            if (spUid != Guid.Empty && seen.Add(spUid))
            {
                members.Add(spUid);
            }
        }

        foreach (var childUid in person.ChildrenId)
        {
            if (childUid == Guid.Empty || seen.Contains(childUid))
            {
                continue;
            }

            if (peopleByUid.TryGetValue(childUid, out var childData))
            {
                BuildUniversalMembers(childData, childUid, peopleByUid, members, seen);
            }
            else
            {
                if (seen.Add(childUid))
                {
                    members.Add(childUid);
                }
            }
        }
    }

    private static void WalkAncestors(
        MeFile startPerson,
        IReadOnlyDictionary<Guid, MeFile> peopleByUid,
        List<Guid> members,
        HashSet<Guid> seen,
        List<string> log)
    {
        var queue = new Queue<Guid>();

        foreach (var parentUid in startPerson.ParentsId)
        {
            if (parentUid != Guid.Empty)
            {
                queue.Enqueue(parentUid);
            }
        }

        var visited = new HashSet<Guid>();

        while (queue.Count > 0)
        {
            var ancUid = queue.Dequeue();

            if (ancUid == Guid.Empty)
            {
                continue;
            }

            if (!visited.Add(ancUid))
            {
                throw new TreeIntegrityException(
                    $"Cycle detected in ancestor walk: {ancUid} was visited more than once starting from {startPerson.UniqueIdentifier}.");
            }

            if (!peopleByUid.TryGetValue(ancUid, out var anc))
            {
                log.Add($"ANCESTOR_MISSING: {ancUid} — folder removed outside application.");
                throw new TreeIntegrityException(
                    $"Person folder missing or unreadable: {ancUid}. The tree was likely modified outside the application.");
            }

            if (seen.Add(ancUid))
            {
                members.Add(ancUid);
            }

            foreach (var spUid in anc.SpouseId)
            {
                if (spUid != Guid.Empty && seen.Add(spUid))
                {
                    members.Add(spUid);
                }
            }

            foreach (var ppUid in anc.ParentsId)
            {
                if (ppUid != Guid.Empty)
                {
                    queue.Enqueue(ppUid);
                }
            }
        }
    }

    #endregion
}
