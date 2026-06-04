using System;
using System.Collections.Generic;
using TreeManager.Core.Domain;

namespace TreeManager.Core.Abstractions.Services;

/// <summary>Generates the Drzewo folder-tree view for a given root person.</summary>
public interface IFolderTreeGenerator
{
    /// <summary>
    /// Scans <paramref name="rootPath"/>, computes membership from <paramref name="rootPersonId"/>,
    /// wipes and recreates the Drzewo folder, and writes one .lnk per member.
    /// Returns the count of written shortcuts and any build-log messages.
    /// </summary>
    (int Written, IReadOnlyList<string> Log) Generate(string rootPath, Guid rootPersonId);

    /// <summary>
    /// Computes the Drzewo membership (hourglass DFS) for <paramref name="rootPersonId"/>
    /// using the pre-built <paramref name="peopleByUid"/> map.
    /// Returns the ordered member list and any build-log messages.
    /// Consumed by both <see cref="Generate"/> and the lineage folder generator for token reuse.
    /// </summary>
    (IReadOnlyList<FolderTreeMember> Members, IReadOnlyList<string> Log) ComputeMembership(
        Guid rootPersonId,
        IReadOnlyDictionary<Guid, MeFile> peopleByUid);
}
