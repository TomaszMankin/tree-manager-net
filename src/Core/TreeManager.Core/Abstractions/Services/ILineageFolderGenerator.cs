using System;
using System.Collections.Generic;

namespace TreeManager.Core.Abstractions.Services;

/// <summary>Generates the Rody lineage-folder view for a given root person.</summary>
public interface ILineageFolderGenerator
{
    /// <summary>
    /// Scans <paramref name="rootPath"/>, computes lineage membership from <paramref name="rootPersonId"/>,
    /// wipes and recreates the Rody folder, and writes one .lnk per member per surname subfolder.
    /// Returns the count of written shortcuts and any build-log messages.
    /// </summary>
    (int Written, IReadOnlyList<string> Log) Generate(string rootPath, Guid rootPersonId);
}
