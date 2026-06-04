using System;
using System.Collections.Generic;

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
}
