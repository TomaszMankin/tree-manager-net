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

/// <summary>
/// Generates the Drzewo folder-tree view.
/// Generates the Drzewo folder-tree view using spouse-seeded hourglass DFS.
/// </summary>
public sealed class DrzewoGenerator : IDrzewoGenerator
{
    private const string DrzewoFolderName = "Drzewo";

    private readonly IMeFileProcessor _processor;
    private readonly IShortcutCreator _shortcutCreator;
    private readonly IFileSystemFacade _fs;
    private readonly ILogger _log;

    public DrzewoGenerator(
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

    // -------------------------------------------------------------------------
    // IDrzewoGenerator
    // -------------------------------------------------------------------------

    public (int Written, IReadOnlyList<string> Log) Generate(string rootPath, Guid rootPersonId)
    {
        // Build UUID→MeFile map from the full scan
        var peopleByUid = new Dictionary<Guid, MeFile>();
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
                _log.Warning(ex, "Generate: failed to read {Path}", meFilePath);
            }
        }

        var (members, computeLog) = ComputeMembership(rootPersonId, peopleByUid);
        var buildLog = new List<string>(computeLog);

        // Wipe + recreate Drzewo folder
        var drzewoPath = Path.Combine(rootPath, DrzewoFolderName);
        _fs.CreateDirectory(drzewoPath);

        foreach (var filePath in _fs.EnumerateFiles(drzewoPath, "*").ToList())
        {
            _fs.DeleteFile(filePath);
        }

        foreach (var dirPath in _fs.EnumerateDirectories(drzewoPath).ToList())
        {
            _fs.DeleteDirectory(dirPath, recursive: true);
        }

        // Write one shortcut per member — dedup filenames
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        int written = 0;

        foreach (var member in members)
        {
            var baseFilename = DrzewoNaming.RenderFilename(member);
            var filename = DeduplicateFilename(baseFilename, seen);
            seen.Add(filename);

            var lnkPath = Path.Combine(drzewoPath, filename);
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

    // -------------------------------------------------------------------------
    // Pure membership computation
    // -------------------------------------------------------------------------

    /// <summary>
    /// Computes the hourglass membership for the given root person.
    /// Pure: takes an in-memory map, returns ordered list + build log.
    /// Spouse-seeded hourglass DFS: ancestors paternal-first, descendants BFS birth-order.
    /// </summary>
    public (IReadOnlyList<FolderTreeMember> Members, IReadOnlyList<string> Log)
        ComputeMembership(Guid rootPersonId, IReadOnlyDictionary<Guid, MeFile> peopleByUid)
    {
        var log = new List<string>();

        if (!peopleByUid.TryGetValue(rootPersonId, out var rootData))
        {
            log.Add($"ERROR: root person {rootPersonId} not found in map.");
            return ([], log);
        }

        // ── Gen 0: root + spouses ──────────────────────────────────────────
        var gen0Members = new Dictionary<Guid, FolderTreeMember>();
        gen0Members[rootPersonId] = new FolderTreeMember(
            Uid: rootPersonId,
            Generation: 0,
            CoupleIndex: 0,
            TotalCouplesInGeneration: 1,
            Role: "self",
            Gender: GenderToken(rootData.Sex),
            FullName: DrzewoNaming.FullName(rootData),
            TargetLocation: rootData.Location);

        foreach (var spouseId in rootData.SpouseId)
        {
            if (spouseId == Guid.Empty || gen0Members.ContainsKey(spouseId))
            {
                continue;
            }

            if (!peopleByUid.TryGetValue(spouseId, out var spouseData))
            {
                log.Add($"MISSING: spouse {spouseId} not in map.");
                continue;
            }

            gen0Members[spouseId] = new FolderTreeMember(
                Uid: spouseId,
                Generation: 0,
                CoupleIndex: 0,
                TotalCouplesInGeneration: 1,
                Role: "spouse",
                Gender: GenderToken(spouseData.Sex),
                FullName: DrzewoNaming.FullName(spouseData),
                TargetLocation: spouseData.Location);
        }

        // ── Ancestor DFS (upward) ─────────────────────────────────────────
        // Two-pass: pass 1 discovers couples in DFS order; pass 2 assigns letters.
        // Spouse-seeded: push spouses first, root last → root pops first via LIFO.
        var ancestorCouplesByGen = new Dictionary<int, List<(Guid BloodUid, Guid PartnerUid)>>();
        var ancestorCoupleUidSet = new HashSet<Guid>();
        var ancestorVisited = new HashSet<Guid> { rootPersonId };
        var dfsStack = new Stack<(Guid PersonUid, int Gen)>();

        // Push spouses first (root pops first via LIFO)
        foreach (var spouseId in rootData.SpouseId)
        {
            if (spouseId != Guid.Empty && !ancestorVisited.Contains(spouseId))
            {
                dfsStack.Push((spouseId, 1));
                ancestorVisited.Add(spouseId);
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
            if (fatherUid != Guid.Empty && !ancestorCoupleUidSet.Contains(fatherUid))
            {
                if (!ancestorCouplesByGen.ContainsKey(gen))
                {
                    ancestorCouplesByGen[gen] = [];
                }

                ancestorCouplesByGen[gen].Add((fatherUid, motherUid));
                ancestorCoupleUidSet.Add(fatherUid);
                if (motherUid != Guid.Empty)
                {
                    ancestorCoupleUidSet.Add(motherUid);
                }
            }
            else if (motherUid != Guid.Empty && !ancestorCoupleUidSet.Contains(motherUid))
            {
                if (!ancestorCouplesByGen.ContainsKey(gen))
                {
                    ancestorCouplesByGen[gen] = [];
                }

                ancestorCouplesByGen[gen].Add((motherUid, Guid.Empty));
                ancestorCoupleUidSet.Add(motherUid);
            }

            // Push parents reversed-paternal-first (mother first, father last → father pops first)
            foreach (var (pid, _) in Enumerable.Reverse(parentsSorted))
            {
                if (!ancestorVisited.Contains(pid))
                {
                    ancestorVisited.Add(pid);
                    dfsStack.Push((pid, gen + 1));
                }
                else
                {
                    log.Add($"CYCLE detected at UUID {pid}; skipping to prevent infinite loop.");
                }
            }
        }

        // Pass 2: build ancestor FolderTreeMember objects
        var ancestorMembers = new Dictionary<Guid, FolderTreeMember>();
        foreach (var (gen, couples) in ancestorCouplesByGen)
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
                        FullName: bData != null ? DrzewoNaming.FullName(bData) : bloodUid.ToString(),
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
                        FullName: pData != null ? DrzewoNaming.FullName(pData) : partnerUid.ToString(),
                        TargetLocation: pData?.Location ?? string.Empty);
                }
            }
        }

        // Log cycle collisions with gen-0
        foreach (var uid in ancestorMembers.Keys)
        {
            if (gen0Members.ContainsKey(uid))
            {
                log.Add($"CYCLE (ancestor): {uid} already in gen-0 members; gen-0 classification wins.");
            }
        }

        // ── Descendant traversal (downward) — FIFO BFS-like ──────────────
        var descendantCouplesByGen = new Dictionary<int, List<(Guid ChildUid, Guid SpouseUid)>>();
        var descendantCoupleChildSet = new HashSet<Guid>();
        var descendantVisited = new HashSet<Guid> { rootPersonId };
        var descQueue = new Queue<(Guid PersonUid, int NextGen)>();

        // Seed from root's children at gen -1
        foreach (var childId in rootData.ChildrenId)
        {
            if (childId == Guid.Empty || descendantVisited.Contains(childId))
            {
                continue;
            }

            descendantVisited.Add(childId);

            if (!descendantCoupleChildSet.Contains(childId))
            {
                if (!descendantCouplesByGen.ContainsKey(-1))
                {
                    descendantCouplesByGen[-1] = [];
                }

                Guid firstSpouseId = Guid.Empty;
                if (peopleByUid.TryGetValue(childId, out var childData))
                {
                    firstSpouseId = childData.SpouseId.FirstOrDefault(
                        s => s != Guid.Empty && s != rootPersonId && !gen0Members.ContainsKey(s),
                        Guid.Empty);
                }

                descendantCouplesByGen[-1].Add((childId, firstSpouseId));
                descendantCoupleChildSet.Add(childId);
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
                if (childId == Guid.Empty || descendantVisited.Contains(childId))
                {
                    continue;
                }

                descendantVisited.Add(childId);

                if (!descendantCoupleChildSet.Contains(childId))
                {
                    if (!descendantCouplesByGen.ContainsKey(nextGen))
                    {
                        descendantCouplesByGen[nextGen] = [];
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

                    descendantCouplesByGen[nextGen].Add((childId, firstSpouseId));
                    descendantCoupleChildSet.Add(childId);
                }

                descQueue.Enqueue((childId, nextGen - 1));
            }
        }

        // Pass 2: build descendant FolderTreeMember objects
        var descendantMembers = new Dictionary<Guid, FolderTreeMember>();
        foreach (var (gen, couples) in descendantCouplesByGen)
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
                        FullName: cData != null ? DrzewoNaming.FullName(cData) : childUid.ToString(),
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
                            FullName: spData != null ? DrzewoNaming.FullName(spData) : spouseUid.ToString(),
                            TargetLocation: spData?.Location ?? string.Empty);
                    }
                }
            }
        }

        // Assemble: gen0, then ancestors, then descendants (gen-0 wins collisions)
        var allMembers = new Dictionary<Guid, FolderTreeMember>();
        foreach (var kv in gen0Members) { allMembers[kv.Key] = kv.Value; }
        foreach (var kv in ancestorMembers)
        {
            if (!allMembers.ContainsKey(kv.Key))
            {
                allMembers[kv.Key] = kv.Value;
            }
        }

        foreach (var kv in descendantMembers)
        {
            if (!allMembers.ContainsKey(kv.Key))
            {
                allMembers[kv.Key] = kv.Value;
            }
        }

        return (allMembers.Values.ToList(), log);
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

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
}
