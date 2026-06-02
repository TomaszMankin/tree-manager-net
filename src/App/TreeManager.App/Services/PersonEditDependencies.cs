using TreeManager.Core.Abstractions.Persistence;

namespace TreeManager.App.Services;

public sealed record PersonEditDependencies(
    IPersonDirectoryService DirectoryService,
    IPersonPickerService PickerService,
    IPersonLoaderService LoaderService);
