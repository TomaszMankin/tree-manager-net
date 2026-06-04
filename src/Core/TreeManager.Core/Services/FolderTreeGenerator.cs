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

/// <summary>Generates the Drzewo folder-tree view using spouse-seeded hourglass DFS.</summary>
public sealed class FolderTreeGenerator : IFolderTreeGenerator
{
    private const string OutputFolderName = "Drzewo";

    private readonly IMeFileProcessor _processor;
    private readonly IShortcutCreator _shortcutCreator;
    private readonly IFileSystemFacade _fs;
    private readonly ILogger _log;

    public FolderTreeGenerator(
        IMeFileProcessor processor,
        IShortcutCreator shortcutCreator,
        IFileSystemFacade fs,
        ILogger log)
    {
        _processor = processor;
        _shortcutCreator = shortcutCreator;
        _fs = fs;
        _log = log;
    }

    #region IFolderTreeGenerator

    public (int Written, IReadOnlyList<string> Log) Generate(string rootPath, Guid rootPersonId)
    {
        // Build UUID→MeFile map from the full scan
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

        var (members, computeLog) = ComputeMembership(rootPersonId, peopleByUid);
        var buildLog = new List<string>(scanErrors.Count + computeLog.Count);
        buildLog.AddRange(scanErrors);
        buildLog.AddRange(computeLog);

        // Wipe + recreate Drzewo folder
        var outputPath = Path.Combine(rootPath, OutputFolderName);
        _fs.CreateDirectory(outputPath);

        foreach (var filePath in _fs.EnumerateFiles(outputPath, "*").ToList())
        {
            _fs.DeleteFile(filePath);
        }

        foreach (var dirPath in _fs.EnumerateDirectories(outputPath).ToList())
        {
            _fs.DeleteDirectory(dirPath, recursive: true);
        }

        // Write one shortcut per member — dedup filenames
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        int written = 0;

        foreach (var member in members)
        {
            var baseFilename = FolderTreeNaming.RenderFilename(member);
            var filename = DeduplicateFilename(baseFilename, seen);
            seen.Add(filename);

            var lnkPath = Path.Combine(outputPath, filename);
            try
            {
                _shortcutCreator.Create(member.TargetLocation, lnkPath);
                written++;
            }
            catch (Exception ex)
            {
                _log.Error(ex, "Generate: SHORTCUT failed for {Name}", member.FullName);
                buildLog.Add($"SHORTCUT_ERROR: failed to create shortcut for {member.FullName} — {ex.Message}");
            }
        }

        return (written, buildLog);
    }

    #endregion

    #region Membership

    public (IReadOnlyList<FolderTreeMember> Members, IReadOnlyList<string> Log)
        ComputeMembership(Guid rootPersonId, IReadOnlyDictionary<Guid, MeFile> peopleByUid)
    {
        var log = new List<string>();

        if (!peopleByUid.TryGetValue(rootPersonId, out var rootData))
        {
            log.Add($"ERROR: root person {rootPersonId} not found in map.");
            return ([], log);
        }

        var gen0 = BuildRootGeneration(rootPersonId, rootData, peopleByUid);
        var ancestorCouples = DiscoverAncestorCouples(rootPersonId, rootData, gen0, peopleByUid, log);
        var ancestorMembers = BuildAncestorMembers(ancestorCouples, peopleByUid);

        foreach (var uid in ancestorMembers.Keys)
        {
            if (gen0.ContainsKey(uid))
            {
                log.Add($"CYCLE (ancestor): {uid} already in gen-0 members; gen-0 classification wins.");
            }
        }

        var descendantCouples = DiscoverDescendantCouples(rootPersonId, rootData, gen0, ancestorMembers, peopleByUid, log);
        var descendantMembers = BuildDescendantMembers(descendantCouples, peopleByUid);

        return (AssembleMembers(gen0, ancestorMembers, descendantMembers), log);
    }

    #endregion

    #region Helpers

    private Dictionary<Guid, FolderTreeMember> BuildRootGeneration(
        Guid rootPersonId, MeFile rootData, IReadOnlyDictionary<Guid, MeFile> peopleByUid)
    {
        var gen0Members = new Dictionary<Guid, FolderTreeMember>();
        gen0Members[rootPersonId] = new FolderTreeMember(
            Uid: rootPersonId,
            Generation: 0,
            CoupleIndex: 0,
            TotalCouplesInGeneration: 1,
            Role: "self",
            Gender: GenderToken(rootData.Sex),
            FullName: FolderTreeNaming.FullName(rootData),
            TargetLocation: rootData.Location);

        foreach (var spouseId in rootData.SpouseId)
        {
            if (spouseId == Guid.Empty || gen0Members.ContainsKey(spouseId))
            {
                continue;
            }

            if (!peopleByUid.TryGetValue(spouseId, out var spouseData))
            {
                continue;
            }

            gen0Members[spouseId] = new FolderTreeMember(
                Uid: spouseId,
                Generation: 0,
                CoupleIndex: 0,
                TotalCouplesInGeneration: 1,
                Role: "spouse",
                Gender: GenderToken(spouseData.Sex),
                FullName: FolderTreeNaming.FullName(spouseData),
                TargetLocation: spouseData.Location);
        }

        return gen0Members;
    }

    private Dictionary<int, List<(Guid BloodUid, Guid PartnerUid)>> DiscoverAncestorCouples(
        Guid rootPersonId, MeFile rootData,
        Dictionary<Guid, FolderTreeMember> gen0Members,
        IReadOnlyDictionary<Guid, MeFile> peopleByUid,
        List<string> log)
    {
        // ── Ancestor DFS (upward) ─────────────────────────────────────────
        // Two-pass: pass 1 discovers couples in DFS order; pass 2 assigns letters.
        var couplesByGen = new Dictionary<int, List<(Guid BloodUid, Guid PartnerUid)>>();
        var coupleUidSet = new HashSet<Guid>();
        var visited = new HashSet<Guid> { rootPersonId };
        var dfsStack = new Stack<(Guid PersonUid, int Gen)>();

        // Push spouses first (root pops first via LIFO)
        foreach (var spouseId in rootData.SpouseId)
        {
            if (spouseId != Guid.Empty && !visited.Contains(spouseId))
            {
                dfsStack.Push((spouseId, 1));
                visited.Add(spouseId);
            }
        }

        // Push root last so it pops first
        dfsStack.Push((rootPersonId, 1));

        while (dfsStack.Count > 0)
        {
            var (personUid, gen) = dfsStack.Pop();

            if (!peopleByUid.TryGetValue(personUid, out var personData))
            {
                continue;
            }

            if (personData.ParentsId.Count == 0)
            {
                continue;
            }

            // Classify parents by gender → paternal-first order
            var parentsWithEdge = new List<(Guid Uid, string Edge)>();
            for (int i = 0; i < personData.ParentsId.Count; i++)
            {
                var pid = personData.ParentsId[i];
                if (pid == Guid.Empty)
                {
                    continue;
                }

                string edge;
                if (peopleByUid.TryGetValue(pid, out var pData))
                {
                    if (pData.Sex == Sex.Male)
                    {
                        edge = "F"; // father edge
                    }
                    else if (pData.Sex == Sex.Female)
                    {
                        edge = "M"; // mother edge
                    }
                    else
                    {
                        edge = (i == 0) ? "F" : "M";
                        log.Add($"GENDER_FALLBACK: {pid} has unknown sex; assigned edge '{edge}' from list position {i}.");
                    }
                }
                else
                {
                    edge = (i == 0) ? "F" : "M";
                    log.Add($"MISSING: parent {pid} not in map.");
                }

                parentsWithEdge.Add((pid, edge));
            }

            // Sort paternal-first: 'F' (father) before 'M' (mother)
            var parentsSorted = parentsWithEdge
                .OrderBy(p => p.Edge)
                .ToList();

            var fatherUid = parentsSorted.FirstOrDefault(p => p.Edge == "F").Uid;
            var motherUid = parentsSorted.FirstOrDefault(p => p.Edge == "M").Uid;

            // Register as one couple if not already seen
            if (fatherUid != Guid.Empty && !coupleUidSet.Contains(fatherUid))
            {
                if (!couplesByGen.ContainsKey(gen))
                {
                    couplesByGen[gen] = [];
                }

                couplesByGen[gen].Add((fatherUid, motherUid));
                coupleUidSet.Add(fatherUid);
                if (motherUid != Guid.Empty)
                {
                    coupleUidSet.Add(motherUid);
                }
            }
            else if (motherUid != Guid.Empty && !coupleUidSet.Contains(motherUid))
            {
                if (!couplesByGen.ContainsKey(gen))
                {
                    couplesByGen[gen] = [];
                }

                couplesByGen[gen].Add((motherUid, Guid.Empty));
                coupleUidSet.Add(motherUid);
            }

            // Push parents reversed-paternal-first (mother first, father last → father pops first)
            foreach (var (pid, _) in Enumerable.Reverse(parentsSorted))
            {
                if (!visited.Contains(pid))
                {
                    visited.Add(pid);
                    dfsStack.Push((pid, gen + 1));
                }
                else
                {
                    log.Add($"CYCLE detected at UUID {pid}; skipping to prevent infinite loop.");
                }
            }
        }

        return couplesByGen;
    }

    private static Dictionary<Guid, FolderTreeMember> BuildAncestorMembers(
        Dictionary<int, List<(Guid BloodUid, Guid PartnerUid)>> couplesByGen,
        IReadOnlyDictionary<Guid, MeFile> peopleByUid)
    {
        var ancestorMembers = new Dictionary<Guid, FolderTreeMember>();
        foreach (var (gen, couples) in couplesByGen)
        {
            int total = couples.Count;
            for (int coupleIdx = 0; coupleIdx < couples.Count; coupleIdx++)
            {
                var (bloodUid, partnerUid) = couples[coupleIdx];

                if (bloodUid != Guid.Empty && !ancestorMembers.ContainsKey(bloodUid))
                {
                    peopleByUid.TryGetValue(bloodUid, out var bData);
                    ancestorMembers[bloodUid] = new FolderTreeMember(
                        Uid: bloodUid,
                        Generation: gen,
                        CoupleIndex: coupleIdx,
                        TotalCouplesInGeneration: total,
                        Role: "ancestor",
                        Gender: bData != null ? GenderToken(bData.Sex) : string.Empty,
                        FullName: bData != null ? FolderTreeNaming.FullName(bData) : bloodUid.ToString(),
                        TargetLocation: bData?.Location ?? string.Empty);
                }

                if (partnerUid != Guid.Empty && !ancestorMembers.ContainsKey(partnerUid))
                {
                    peopleByUid.TryGetValue(partnerUid, out var pData);
                    ancestorMembers[partnerUid] = new FolderTreeMember(
                        Uid: partnerUid,
                        Generation: gen,
                        CoupleIndex: coupleIdx,
                        TotalCouplesInGeneration: total,
                        Role: "ancestor",
                        Gender: pData != null ? GenderToken(pData.Sex) : string.Empty,
                        FullName: pData != null ? FolderTreeNaming.FullName(pData) : partnerUid.ToString(),
                        TargetLocation: pData?.Location ?? string.Empty);
                }
            }
        }

        return ancestorMembers;
    }

    private Dictionary<int, List<(Guid ChildUid, Guid SpouseUid)>> DiscoverDescendantCouples(
        Guid rootPersonId, MeFile rootData,
        Dictionary<Guid, FolderTreeMember> gen0Members,
        Dictionary<Guid, FolderTreeMember> ancestorMembers,
        IReadOnlyDictionary<Guid, MeFile> peopleByUid,
        List<string> log)
    {
        // ── Descendant traversal (downward) — FIFO BFS-like ──────────────
        var couplesByGen = new Dictionary<int, List<(Guid ChildUid, Guid SpouseUid)>>();
        var coupleChildSet = new HashSet<Guid>();
        var visited = new HashSet<Guid> { rootPersonId };
        var descQueue = new Queue<(Guid PersonUid, int NextGen)>();

        // Seed from root's children at gen -1
        foreach (var childId in rootData.ChildrenId)
        {
            if (childId == Guid.Empty || visited.Contains(childId))
            {
                continue;
            }

            visited.Add(childId);

            if (!coupleChildSet.Contains(childId))
            {
                if (!couplesByGen.ContainsKey(-1))
                {
                    couplesByGen[-1] = [];
                }

                Guid firstSpouseId = Guid.Empty;
                if (peopleByUid.TryGetValue(childId, out var childData))
                {
                    firstSpouseId = childData.SpouseId.FirstOrDefault(
                        s => s != Guid.Empty && s != rootPersonId && !gen0Members.ContainsKey(s),
                        Guid.Empty);
                }

                couplesByGen[-1].Add((childId, firstSpouseId));
                coupleChildSet.Add(childId);
            }

            descQueue.Enqueue((childId, -2));
        }

        while (descQueue.Count > 0)
        {
            var (personUid, nextGen) = descQueue.Dequeue();

            if (!peopleByUid.TryGetValue(personUid, out var personData))
            {
                continue;
            }

            foreach (var childId in personData.ChildrenId)
            {
                if (childId == Guid.Empty || visited.Contains(childId))
                {
                    continue;
                }

                visited.Add(childId);

                if (!coupleChildSet.Contains(childId))
                {
                    if (!couplesByGen.ContainsKey(nextGen))
                    {
                        couplesByGen[nextGen] = [];
                    }

                    var alreadyPlaced = new HashSet<Guid>(gen0Members.Keys);
                    foreach (var k in ancestorMembers.Keys)
                    {
                        alreadyPlaced.Add(k);
                    }

                    Guid firstSpouseId = Guid.Empty;
                    if (peopleByUid.TryGetValue(childId, out var childData))
                    {
                        firstSpouseId = childData.SpouseId.FirstOrDefault(
                            s => s != Guid.Empty && s != rootPersonId && !alreadyPlaced.Contains(s),
                            Guid.Empty);
                    }

                    couplesByGen[nextGen].Add((childId, firstSpouseId));
                    coupleChildSet.Add(childId);
                }

                descQueue.Enqueue((childId, nextGen - 1));
            }
        }

        return couplesByGen;
    }

    private static Dictionary<Guid, FolderTreeMember> BuildDescendantMembers(
        Dictionary<int, List<(Guid ChildUid, Guid SpouseUid)>> couplesByGen,
        IReadOnlyDictionary<Guid, MeFile> peopleByUid)
    {
        var descendantMembers = new Dictionary<Guid, FolderTreeMember>();
        foreach (var (gen, couples) in couplesByGen)
        {
            int total = couples.Count;
            for (int coupleIdx = 0; coupleIdx < couples.Count; coupleIdx++)
            {
                var (childUid, spouseUid) = couples[coupleIdx];

                if (childUid != Guid.Empty && !descendantMembers.ContainsKey(childUid))
                {
                    peopleByUid.TryGetValue(childUid, out var cData);
                    string cGender = cData != null ? GenderToken(cData.Sex) : string.Empty;

                    descendantMembers[childUid] = new FolderTreeMember(
                        Uid: childUid,
                        Generation: gen,
                        CoupleIndex: coupleIdx,
                        TotalCouplesInGeneration: total,
                        Role: "descendant",
                        Gender: cGender,
                        FullName: cData != null ? FolderTreeNaming.FullName(cData) : childUid.ToString(),
                        TargetLocation: cData?.Location ?? string.Empty);

                    // Descendant spouse inherits the descendant's gender (rule B)
                    if (spouseUid != Guid.Empty && !descendantMembers.ContainsKey(spouseUid))
                    {
                        peopleByUid.TryGetValue(spouseUid, out var spData);
                        descendantMembers[spouseUid] = new FolderTreeMember(
                            Uid: spouseUid,
                            Generation: gen,
                            CoupleIndex: coupleIdx,
                            TotalCouplesInGeneration: total,
                            Role: "descendant_spouse",
                            Gender: cGender, // rule B: descendant's gender, not spouse's own
                            FullName: spData != null ? FolderTreeNaming.FullName(spData) : spouseUid.ToString(),
                            TargetLocation: spData?.Location ?? string.Empty);
                    }
                }
            }
        }

        return descendantMembers;
    }

    private static IReadOnlyList<FolderTreeMember> AssembleMembers(
        Dictionary<Guid, FolderTreeMember> gen0,
        Dictionary<Guid, FolderTreeMember> ancestors,
        Dictionary<Guid, FolderTreeMember> descendants)
    {
        // Assemble: gen0, then ancestors, then descendants (gen-0 wins collisions)
        var allMembers = new Dictionary<Guid, FolderTreeMember>();
        foreach (var kv in gen0) { allMembers[kv.Key] = kv.Value; }
        foreach (var kv in ancestors)
        {
            if (!allMembers.ContainsKey(kv.Key))
            {
                allMembers[kv.Key] = kv.Value;
            }
        }

        foreach (var kv in descendants)
        {
            if (!allMembers.ContainsKey(kv.Key))
            {
                allMembers[kv.Key] = kv.Value;
            }
        }

        return allMembers.Values.ToList();
    }

    private static string GenderToken(Sex sex)
    {
        if (sex == Sex.Male) { return "M"; }
        if (sex == Sex.Female) { return "F"; }
        return string.Empty;
    }

    private static string DeduplicateFilename(string filename, HashSet<string> seen)
    {
        if (!seen.Contains(filename))
        {
            return filename;
        }

        // Strip .lnk, append (N), re-add .lnk
        string withoutExt = filename.EndsWith(".lnk", StringComparison.OrdinalIgnoreCase)
            ? filename[..^4]
            : filename;

        int n = 2;
        while (true)
        {
            string candidate = $"{withoutExt} ({n}).lnk";
            if (!seen.Contains(candidate))
            {
                return candidate;
            }

            n++;
        }
    }

    #endregion
}
