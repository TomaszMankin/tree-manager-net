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
    private const string GenerateDrzewoSuccessTemplate = "Wygenerowano drzewo: {0} skrótów.";
    private const string GenerateDrzewoErrorMessage = "Nie udało się wygenerować drzewa. Spróbuj ponownie.";

    public PersonViewModel Person { get; }
    public DatesTabViewModel Dates { get; }
    public FamilyTabViewModel Family { get; }
    public NotesTabViewModel Notes { get; }

    private readonly IPersonRepository _personRepository;
    private readonly IRootPointerStore _rootPointerStore;
    private readonly PersonEditDependencies _editDeps;
    private readonly DrzewoCommandDependencies _drzewoDeps;
    private readonly ILogger _log;

    private MeFile _originalSnapshot;

    public MainViewModel(
        PersonViewModel person,
        DatesTabViewModel dates,
        FamilyTabViewModel family,
        NotesTabViewModel notes,
        IPersonRepository personRepository,
        IRootPointerStore rootPointerStore,
        PersonEditDependencies editDeps,
        DrzewoCommandDependencies drzewoDeps,
        ILogger log)
    {
        Person = person;
        Dates = dates;
        Family = family;
        Notes = notes;
        _personRepository = personRepository;
        _rootPointerStore = rootPointerStore;
        _editDeps = editDeps;
        _drzewoDeps = drzewoDeps;
        _log = log;
    }

    [ObservableProperty]
    private AppMode _currentMode = AppMode.Add;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [RelayCommand]
    private void SwitchMode(AppMode targetMode)
    {
        if (targetMode == CurrentMode)
        {
            return;
        }

        var current = AssembleCurrentMeFile();
        if (_editDeps.DirtyTracker.IsDirty(_originalSnapshot, current))
        {
            if (!_editDeps.DirtyGuard.ConfirmDiscard())
            {
                return;
            }
        }

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
            _log.Warning("OpenPerson called with empty root path");
            return;
        }

        var people = _editDeps.DirectoryService.GetAll(rootPath);
        var selected = _editDeps.PickerService.PickPerson(people);
        if (selected == null)
        {
            return;
        }

        var currentSnapshot = AssembleCurrentMeFile();
        if (_editDeps.DirtyTracker.IsDirty(_originalSnapshot, currentSnapshot))
        {
            if (!_editDeps.DirtyGuard.ConfirmDiscard())
            {
                return;
            }
        }

        try
        {
            var meFilePath = Path.Combine(rootPath, PeopleListFolderName, selected.DisplayName, "me.json");
            _originalSnapshot = _editDeps.LoaderService.Load(meFilePath, rootPath, Person, Dates, Family, Notes);
            CurrentMode = AppMode.EditTree;
            ErrorMessage = string.Empty;
        }
        catch (Exception ex)
        {
            _log.Error(ex, "OpenPerson failed to load {Person}", selected.DisplayName);
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
                _log.Warning("Save called with empty root path");
                return;
            }

            var totalRelationships =
                Family.Parents.Selected.Count +
                Family.Children.Selected.Count +
                Family.Spouses.Selected.Count +
                Family.Siblings.Selected.Count;
            if (totalRelationships == 0)
            {
                var existingPeople = _editDeps.DirectoryService.GetAll(rootPath);
                if (existingPeople.Count > 0)
                {
                    ErrorMessage = "Osoba musi mieć przynajmniej jedną relację.";
                    return;
                }
            }

            var meFile = AssembleCurrentMeFile();
            meFile = ApplyIdentityOverlay(meFile, rootPath);

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
            _log.Error(ex, "Save failed");
            ErrorMessage = "Zapis nie powiódł się. Spróbuj ponownie.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void SaveAsDraft()
    {
        var rootPath = _rootPointerStore.Read();
        if (string.IsNullOrWhiteSpace(rootPath))
        {
            _log.Warning("SaveAsDraft called with empty root path");
            return;
        }

        try
        {
            var meFile = AssembleCurrentMeFile();
            meFile = ApplyIdentityOverlay(meFile, rootPath);

            _editDeps.DraftRepository.SaveDraft(meFile, rootPath);
            ErrorMessage = string.Empty;
        }
        catch (Exception ex)
        {
            _log.Error(ex, "SaveAsDraft failed");
            ErrorMessage = "Nie udało się zapisać szkicu. Spróbuj ponownie.";
        }
    }

    [RelayCommand]
    private void LoadDraft()
    {
        var rootPath = _rootPointerStore.Read();
        if (string.IsNullOrWhiteSpace(rootPath))
        {
            _log.Warning("LoadDraft called with empty root path");
            return;
        }

        var current = AssembleCurrentMeFile();
        if (_editDeps.DirtyTracker.IsDirty(_originalSnapshot, current))
        {
            if (!_editDeps.DirtyGuard.ConfirmDiscard())
            {
                return;
            }
        }

        var drafts = _editDeps.DraftRepository.GetAllDrafts(rootPath);
        var selected = _editDeps.PickerService.PickPerson(drafts);
        if (selected == null)
        {
            return;
        }

        try
        {
            var draft = _editDeps.DraftRepository.ReadDraft(rootPath, selected.DisplayName);
            var people = _editDeps.DirectoryService.GetAll(rootPath);

            Person.Reset(draft);
            Dates.Reset(draft);
            Family.Reset(draft, people);
            Notes.Reset(draft);

            _originalSnapshot = draft;
            CurrentMode = AppMode.EditDraft;
            ErrorMessage = string.Empty;
        }
        catch (Exception ex)
        {
            _log.Error(ex, "LoadDraft failed for {Draft}", selected.DisplayName);
            ErrorMessage = "Nie udało się wczytać szkicu. Spróbuj ponownie.";
        }
    }

    [RelayCommand]
    private void PromoteDraft()
    {
        var rootPath = _rootPointerStore.Read();
        if (string.IsNullOrWhiteSpace(rootPath))
        {
            _log.Warning("PromoteDraft called with empty root path");
            return;
        }

        if (CurrentMode != AppMode.EditDraft)
        {
            return;
        }

        try
        {
            var meFile = AssembleCurrentMeFile();
            meFile = ApplyIdentityOverlay(meFile, rootPath);

            _editDeps.DraftPromoter.Promote(meFile, rootPath);

            _originalSnapshot = meFile;
            Family.LoadedPersonId = meFile.UniqueIdentifier;
            CurrentMode = AppMode.EditTree;
            ErrorMessage = string.Empty;
        }
        catch (Exception ex)
        {
            _log.Error(ex, "PromoteDraft failed");
            ErrorMessage = "Nie udało się przenieść szkicu do drzewa. Spróbuj ponownie.";
        }
    }

    [RelayCommand]
    private void GenerateDrzewo()
    {
        var rootPath = _rootPointerStore.Read();
        if (string.IsNullOrWhiteSpace(rootPath))
        {
            _log.Warning("GenerateDrzewo called with empty root path");
            return;
        }

        var people = _editDeps.DirectoryService.GetAll(rootPath);
        var selected = _editDeps.PickerService.PickPerson(people);
        if (selected == null)
        {
            return;
        }

        IsBusy = true;
        try
        {
            _drzewoDeps.SettingsStore.SetRootPersonId(rootPath, selected.UniqueIdentifier);
            var result = _drzewoDeps.Generator.Generate(rootPath, selected.UniqueIdentifier);
            ErrorMessage = string.Empty;
            StatusMessage = string.Format(GenerateDrzewoSuccessTemplate, result.Written);
        }
        catch (Exception ex)
        {
            _log.Error(ex, "GenerateDrzewo failed");
            ErrorMessage = GenerateDrzewoErrorMessage;
            StatusMessage = string.Empty;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private MeFile AssembleCurrentMeFile()
    {
        var meFile = Person.ToMeFile();
        meFile = Dates.ToMeFile(meFile);
        meFile = Family.ToMeFile(meFile);
        meFile = Notes.ToMeFile(meFile);
        return meFile;
    }

    private MeFile ApplyIdentityOverlay(MeFile meFile, string rootPath)
    {
        if (meFile.UniqueIdentifier == Guid.Empty)
        {
            meFile = meFile with { UniqueIdentifier = Guid.NewGuid() };
        }

        var folderName = Person.ToFolderName();
        var personFolderPath = Path.Combine(rootPath, PeopleListFolderName, folderName);
        meFile = meFile with { PersonName = folderName, Location = personFolderPath };

        return meFile;
    }
}
