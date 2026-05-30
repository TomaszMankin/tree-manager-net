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
    private readonly PersonEditDependencies _editDeps;

    private MeFile _originalSnapshot;

    public MainViewModel(
        PersonViewModel person,
        DatesTabViewModel dates,
        FamilyTabViewModel family,
        IPersonRepository personRepository,
        IRootPointerStore rootPointerStore,
        PersonEditDependencies editDeps)
    {
        Person = person;
        Dates = dates;
        Family = family;
        _personRepository = personRepository;
        _rootPointerStore = rootPointerStore;
        _editDeps = editDeps;
    }

    [ObservableProperty]
    private AppMode _currentMode = AppMode.Add;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    [RelayCommand]
    private void SwitchMode(AppMode targetMode)
    {
        if (targetMode == AppMode.Add)
        {
            _originalSnapshot = null;
        }

        CurrentMode = targetMode;
    }

    [RelayCommand]
    private void OpenPerson()
    {
        var rootPath = _rootPointerStore.Read();
        if (string.IsNullOrWhiteSpace(rootPath))
        {
            Log.Warning("OpenPerson called with empty root path");
            return;
        }

        var people = _editDeps.DirectoryService.GetAll(rootPath);
        var selected = _editDeps.PickerService.PickPerson(people);
        if (selected == null)
        {
            return;
        }

        try
        {
            var meFilePath = Path.Combine(rootPath, PeopleListFolderName, selected.DisplayName, "me.json");
            _originalSnapshot = _editDeps.LoaderService.Load(meFilePath, rootPath, Person, Dates, Family);
            CurrentMode = AppMode.EditTree;
            ErrorMessage = string.Empty;
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "OpenPerson failed to load {Person}", selected.DisplayName);
            ErrorMessage = "Nie udało się wczytać osoby. Spróbuj ponownie.";
        }
    }

    [RelayCommand]
    private void Save()
    {
        IsBusy = true;
        try
        {
            var rootPath = _rootPointerStore.Read();
            if (string.IsNullOrWhiteSpace(rootPath))
            {
                Log.Warning("Save called with empty root path");
                return;
            }

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

            if (_originalSnapshot == null)
            {
                _personRepository.Create(meFile, rootPath);
            }
            else
            {
                _personRepository.Update(meFile, _originalSnapshot, rootPath);
                _originalSnapshot = meFile;
            }

            Family.LoadedPersonId = meFile.UniqueIdentifier;
            ErrorMessage = string.Empty;
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Save failed");
            ErrorMessage = "Zapis nie powiódł się. Spróbuj ponownie.";
        }
        finally
        {
            IsBusy = false;
        }
    }
}
