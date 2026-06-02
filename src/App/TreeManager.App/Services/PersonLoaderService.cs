using System;
using TreeManager.App.ViewModels;
using TreeManager.Core.Abstractions.Persistence;
using TreeManager.Core.Domain;

namespace TreeManager.App.Services;

public sealed class PersonLoaderService : IPersonLoaderService
{
    private readonly IMeFileProcessor _processor;
    private readonly IPersonDirectoryService _directoryService;

    public PersonLoaderService(IMeFileProcessor processor, IPersonDirectoryService directoryService)
    {
        ArgumentNullException.ThrowIfNull(processor);
        ArgumentNullException.ThrowIfNull(directoryService);
        _processor = processor;
        _directoryService = directoryService;
    }

    public MeFile Load(
        string meFilePath,
        string rootPath,
        PersonViewModel personVm,
        DatesTabViewModel datesVm,
        FamilyTabViewModel familyVm)
    {
        ArgumentNullException.ThrowIfNull(personVm);
        ArgumentNullException.ThrowIfNull(datesVm);
        ArgumentNullException.ThrowIfNull(familyVm);

        var meFile = _processor.ReadMeFile(meFilePath);
        var allPeople = _directoryService.GetAll(rootPath);

        personVm.Reset(meFile);
        datesVm.Reset(meFile);
        familyVm.Reset(meFile, allPeople);

        return meFile;
    }
}
