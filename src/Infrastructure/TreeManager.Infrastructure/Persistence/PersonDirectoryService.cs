using TreeManager.Core.Abstractions.Persistence;
using TreeManager.Core.Domain;

namespace TreeManager.Infrastructure.Persistence;

public sealed class PersonDirectoryService : IPersonDirectoryService
{
    private readonly IMeFileProcessor _processor;

    public PersonDirectoryService(IMeFileProcessor processor)
    {
        ArgumentNullException.ThrowIfNull(processor);
        _processor = processor;
    }

    public IReadOnlyList<PersonSummary> GetAll(string rootPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rootPath);

        return _processor.ScanMeFiles(rootPath)
            .Select(_processor.ReadMeFile)
            .Select(mf => new PersonSummary(mf.UniqueIdentifier, mf.PersonName ?? string.Empty))
            .ToList();
    }
}
