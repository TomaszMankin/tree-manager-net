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
        // Build UUID→MeFile map from full scan
        var peopleByUid = new Dictionary<Guid, MeFile>();
        var scanErrors = new List<string>();
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
                scanErrors.Add($"READ_ERROR: {meFilePath} — {ex.Message}");
            }
        }

        // Compute Drzewo membership for token reuse (F-003)
        var (drzewoMembers, drzewoLog) = _folderTreeGenerator.ComputeMembership(rootPersonId, peopleByUid);
        var drzewoByUid = drzewoMembers.ToDictionary(m => m.Uid);

        // Compute lineages
        var (lineages, lineageLog) = ComputeLineages(rootPersonId, peopleByUid);

        var buildLog = new List<string>(scanErrors.Count + drzewoLog.Count + lineageLog.Count);
        buildLog.AddRange(scanErrors);
        buildLog.AddRange(drzewoLog);
        buildLog.AddRange(lineageLog);

        // Wipe + recreate Rody/
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

        // Write one subfolder per surname, one shortcut per member
        int written = 0;
        foreach (var surname in lineages.Keys.OrderBy(s => s, StringComparer.Ordinal))
        {
            var group = lineages[surname];
            var subDir = Path.Combine(rodyPath, FolderTreeNaming.Sanitize(surname));
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

        if (!peopleByUid.TryGetValue(rootPersonId, out var root))
        {
            log.Add($"ERROR: root person {rootPersonId} not found in map.");
            return (result, log);
        }

        // Step 2: universal members — DFS down from root via ChildrenId
        var universalMembers = new List<Guid>();
        var universalSeen = new HashSet<Guid>();
        BuildUniversalMembers(root, rootPersonId, peopleByUid, universalMembers, universalSeen);

        // Step 3: enumerate contributors — root.ParentsId (ALL), then per spouse: spouse.ParentsId (ALL)
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

        // Step 4: per contributor — build lineage group
        foreach (var contributorUid in contributors)
        {
            if (!peopleByUid.TryGetValue(contributorUid, out var contributor))
            {
                log.Add($"SKIP_CONTRIBUTOR: {contributorUid} not in map.");
                continue;
            }

            var surname = LineageSurname(contributor);
            if (surname == null)
            {
                log.Add($"SKIP_CONTRIBUTOR: {contributorUid} has null/empty/unknown surname.");
                continue;
            }

            if (result.ContainsKey(surname))
            {
                log.Add($"COLLISION: surname '{surname}' already seeded; contributor {contributorUid} dropped.");
                continue;
            }

            // Build member list: a. universal, b. contributor, c. contributor spouses, d. ancestor walk
            var memberList = new List<Guid>();
            var memberSeen = new HashSet<Guid>();

            // a. universal members
            foreach (var uid in universalMembers)
            {
                if (memberSeen.Add(uid))
                {
                    memberList.Add(uid);
                }
            }

            // b. contributor
            if (memberSeen.Add(contributorUid))
            {
                memberList.Add(contributorUid);
            }

            // c. contributor's spouses (each SpouseId as leaf)
            foreach (var spUid in contributor.SpouseId)
            {
                if (spUid != Guid.Empty && memberSeen.Add(spUid))
                {
                    memberList.Add(spUid);
                }
            }

            // d. ancestor walk (R4 — full bloodline, NO surname gate)
            WalkAncestors(contributor, peopleByUid, memberList, memberSeen, log);

            result[surname] = new LineageGroup(surname, contributorUid, memberList);
        }

        return (result, log);
    }

    #endregion

    #region Helpers

    private static void BuildUniversalMembers(
        MeFile person,
        Guid personUid,
        IReadOnlyDictionary<Guid, MeFile> peopleByUid,
        List<Guid> members,
        HashSet<Guid> seen)
    {
        // DFS down from root: add person, then add each SpouseId as leaf, then recurse into children
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
        // BFS via queue (py uses pop(0) = FIFO); NO surname comparison anywhere in this method
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

            if (ancUid == Guid.Empty || !visited.Add(ancUid))
            {
                continue;
            }

            if (!peopleByUid.TryGetValue(ancUid, out var anc))
            {
                log.Add($"ANCESTOR_MISSING: {ancUid} not readable; branch stopped.");
                continue;
            }

            // Blood ancestor — added unconditionally; NO surname check
            if (seen.Add(ancUid))
            {
                members.Add(ancUid);
            }

            // Spouse-of-ancestor: added as leaf (NOT walked further)
            foreach (var spUid in anc.SpouseId)
            {
                if (spUid != Guid.Empty && seen.Add(spUid))
                {
                    members.Add(spUid);
                }
            }

            // Blood parents: pushed to queue (walk continues upward)
            foreach (var ppUid in anc.ParentsId)
            {
                if (ppUid != Guid.Empty)
                {
                    queue.Enqueue(ppUid);
                }
            }
        }
    }

    private static string LineageSurname(MeFile person)
    {
        var candidate = person.HasMaidenName ? person.MaidenName : person.LastName;
        candidate = candidate.Trim();
        if (string.IsNullOrEmpty(candidate) || candidate == UnknownSentinel)
        {
            return null;
        }

        return candidate;
    }

    #endregion
}
