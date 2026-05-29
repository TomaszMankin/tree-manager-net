using System;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Serilog;
using TreeManager.App.Mappers;
using TreeManager.App.Services;
using TreeManager.Core.Abstractions.Persistence;
using TreeManager.Core.Abstractions.Settings;
using TreeManager.Core.Domain;

namespace TreeManager.App.ViewModels;

public sealed partial class MainViewModel : ObservableObject
{
    private const string PeopleListFolderName = "Lista osób";

    public PersonViewModel Person { get; }
    public DatesTabViewModel Dates { get; }
    public FamilyTabViewModel Family { get; }

    private readonly IPersonRepository _personRepository;
    private readonly IRootPointerStore _rootPointerStore;

    public MainViewModel(
        PersonViewModel person,
        DatesTabViewModel dates,
        FamilyTabViewModel family,
        IPersonRepository personRepository,
        IRootPointerStore rootPointerStore)
    {
        Person = person;
        Dates = dates;
        Family = family;
        _personRepository = personRepository;
        _rootPointerStore = rootPointerStore;
    }

    [ObservableProperty]
    private AppMode _currentMode = AppMode.Add;

    [ObservableProperty]
    private bool _isBusy;

    [RelayCommand]
    private void SwitchMode(AppMode targetMode) => CurrentMode = targetMode;

    [RelayCommand]
    private void Save()
    {
        IsBusy = true;
        try
        {
            var rootPath = _rootPointerStore.Read();

            var meFile = Person.ToMeFile();
            meFile = Dates.ToMeFile(meFile);
            meFile = Family.ToMeFile(meFile);

            if (meFile.UniqueIdentifier == Guid.Empty)
            {
                meFile = meFile with { UniqueIdentifier = Guid.NewGuid() };
            }

            var folderName = Person.ToFolderName();
            var personFolderPath = Path.Combine(rootPath, PeopleListFolderName, folderName);
            meFile = meFile with { PersonName = folderName, Location = personFolderPath };

            _personRepository.Create(meFile, rootPath);

            Family.LoadedPersonId = meFile.UniqueIdentifier;
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Save failed");
        }
        finally
        {
            IsBusy = false;
        }
    }
}
