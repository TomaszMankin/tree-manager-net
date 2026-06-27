using System;
using System.IO;
using System.Text.Json;
using TreeManager.Core.Abstractions.IO;
using TreeManager.Core.Abstractions.Shell;
using TreeManager.Core.Domain;
using TreeManager.UiTesting.Graph;

namespace TreeManager.UiTesting.Reading;

/// <summary>
/// Walks a tree root on disk and builds an <see cref="ActualTreeGraph"/>: for every
/// person folder under 'Lista osób' it reads me.json, enumerates the four relationship
/// subfolders and resolves every .lnk target. Targets are normalised to folder name so
/// host-path differences (temp roots, drive letters) never affect comparison.
/// </summary>
public sealed class DiskTreeGraphReader
{
    private const string PeopleListFolder = "Lista osób";
    private const string MeFileName = "me.json";
    private const string LnkPattern = "*.lnk";

    private readonly IFileSystemFacade _fs;
    private readonly IShortcutCreator _shortcutCreator;

    public DiskTreeGraphReader(IFileSystemFacade fs, IShortcutCreator shortcutCreator)
    {
        _fs = fs;
        _shortcutCreator = shortcutCreator;
    }

    public ActualTreeGraph Read(string rootPath)
    {
        var graph = new ActualTreeGraph();

        var peopleListPath = Path.Combine(rootPath, PeopleListFolder);
        if (!_fs.DirectoryExists(peopleListPath))
        {
            return graph;
        }

        foreach (var personFolder in _fs.EnumerateDirectories(peopleListPath))
        {
            var node = ReadPersonFolder(personFolder);
            if (node != null)
            {
                graph.Add(node);
            }
        }

        return graph;
    }

    private PersonNode ReadPersonFolder(string personFolder)
    {
        var meFilePath = Path.Combine(personFolder, MeFileName);
        if (!_fs.FileExists(meFilePath))
        {
            return null;
        }

        var meFile = JsonSerializer.Deserialize<MeFile>(_fs.ReadAllText(meFilePath), MeFile.DefaultOptions);
        var folderName = Path.GetFileName(personFolder);
        var node = new PersonNode(folderName, meFile.UniqueIdentifier);

        CopyReferenceSets(meFile, node);
        ReadLinkEdges(personFolder, node);

        return node;
    }

    private static void CopyReferenceSets(MeFile meFile, PersonNode node)
    {
        AddAll(meFile.ParentsId, node.ParentIds);
        AddAll(meFile.ChildrenId, node.ChildIds);
        AddAll(meFile.SpouseId, node.SpouseIds);
        AddAll(meFile.SiblingsId, node.SiblingIds);
    }

    private void ReadLinkEdges(string personFolder, PersonNode node)
    {
        foreach (var subfolder in RelationshipSubfolders.All)
        {
            var subfolderPath = Path.Combine(personFolder, subfolder);
            if (!_fs.DirectoryExists(subfolderPath))
            {
                continue;
            }

            foreach (var linkPath in _fs.EnumerateFiles(subfolderPath, LnkPattern))
            {
                var resolvedTarget = _shortcutCreator.Resolve(linkPath);
                var targetFolderName = Path.GetFileName(NormalizeTrailingSeparator(resolvedTarget));
                node.LinkEdges.Add(new LinkEdge(subfolder, targetFolderName));
            }
        }
    }

    private static string NormalizeTrailingSeparator(string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return path;
        }
        return path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
    }

    private static void AddAll(System.Collections.Generic.IEnumerable<Guid> source, System.Collections.Generic.HashSet<Guid> target)
    {
        foreach (var id in source)
        {
            target.Add(id);
        }
    }
}
