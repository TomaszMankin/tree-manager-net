using System;
using System.Collections.Generic;
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
    private const string GenerateFolderTreeSuccessTemplate = "Wygenerowano drzewo: {0} skrótów.";
    private const string GenerateFolderTreeErrorMessage = "Nie udało się wygenerować drzewa. Spróbuj ponownie.";
    private const string GenerateFolderTreeNoRootMessage = "Nie wybrano osoby głównej. Wybierz osobę główną przed generowaniem drzewa.";
    private const string GenerateLineageSuccessTemplate = "Wygenerowano rody: {0} skrótów.";
    private const string GenerateLineageErrorMessage = "Nie udało się wygenerować rodów. Spróbuj ponownie.";
    private const string GenerateLineageNoRootMessage = "Nie wybrano osoby głównej. Wybierz osobę główną przed generowaniem rodów.";
    private const string GenerateLineageIntegrityErrorMessage = "Błąd integralności drzewa. Dane zostały zmienione poza aplikacją.";
    private const string ValidateTreeErrorMessage = "Nie udało się sprawdzić spójności drzewa. Spróbuj ponownie.";
    private const string SavePersonSuccessMessage = "Zapisano osobę.";
    private const string SaveDraftSuccessMessage = "Zapisano szkic.";
    private const string OpenDataFolderMissingMessage = "Nie wybrano folderu z danymi.";

    public PersonViewModel Person { get; }
    public DatesTabViewModel Dates { get; }
    public FamilyTabViewModel Family { get; }
    public NotesTabViewModel Notes { get; }

    private readonly IPersonRepository _personRepository;
    private readonly IRootPointerStore _rootPointerStore;
    private readonly PersonEditDependencies _editDeps;
    private readonly FolderTreeCommandDependencies _folderTreeDeps;
    private readonly ValidationCommandDependencies _validationDeps;
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
        FolderTreeCommandDependencies folderTreeDeps,
        ValidationCommandDependencies validationDeps,
        ILogger log)
    {
        Person = person;
        Dates = dates;
        Family = family;
        Notes = notes;
        _personRepository = personRepository;
        _rootPointerStore = rootPointerStore;
        _editDeps = editDeps;
        _folderTreeDeps = folderTreeDeps;
        _validationDeps = validationDeps;
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

    [ObservableProperty]
    private string _windowTitle = "TreeManager — Nowa osoba";

    partial void OnCurrentModeChanged(AppMode value)
    {
        WindowTitle = BuildWindowTitle(value);
        PromoteDraftCommand.NotifyCanExecuteChanged();
    }

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
            CurrentMode = targetMode;
            ResetToBlank();
            return;
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
            WindowTitle = BuildWindowTitle(AppMode.EditTree, selected.DisplayName);
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
        StatusMessage = string.Empty;
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
            StatusMessage = SavePersonSuccessMessage;
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
        StatusMessage = string.Empty;
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
            StatusMessage = SaveDraftSuccessMessage;
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

    [RelayCommand(CanExecute = nameof(CanPromoteDraft))]
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

            var summary = BuildPromoteSummary(meFile);
            if (!_editDeps.PromoteConfirmService.Confirm(summary))
            {
                return;
            }

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

    private bool CanPromoteDraft() => CurrentMode == AppMode.EditDraft;

    [RelayCommand]
    private void OpenDataFolder()
    {
        try
        {
            var rootPath = _rootPointerStore.Read();
            if (string.IsNullOrWhiteSpace(rootPath))
            {
                ErrorMessage = OpenDataFolderMissingMessage;
                return;
            }

            _editDeps.FolderRevealService.Reveal(rootPath);
        }
        catch (Exception ex)
        {
            _log.Error(ex, "OpenDataFolder failed");
            ErrorMessage = "Nie udało się otworzyć folderu z danymi.";
        }
    }

    [RelayCommand]
    private void GenerateFolderTree()
    {
        var rootPath = _rootPointerStore.Read();
        if (string.IsNullOrWhiteSpace(rootPath))
        {
            _log.Warning("GenerateFolderTree called with empty root path");
            return;
        }

        var rootPersonId = _folderTreeDeps.SettingsStore.GetRootPersonId(rootPath);
        if (rootPersonId == Guid.Empty)
        {
            ErrorMessage = GenerateFolderTreeNoRootMessage;
            return;
        }

        IsBusy = true;
        try
        {
            var result = _folderTreeDeps.Generator.Generate(rootPath, rootPersonId);
            ErrorMessage = string.Empty;
            StatusMessage = string.Format(GenerateFolderTreeSuccessTemplate, result.Written);
        }
        catch (Exception ex)
        {
            _log.Error(ex, "GenerateFolderTree failed");
            ErrorMessage = GenerateFolderTreeErrorMessage;
            StatusMessage = string.Empty;
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void GenerateLineageFolders()
    {
        var rootPath = _rootPointerStore.Read();
        if (string.IsNullOrWhiteSpace(rootPath))
        {
            _log.Warning("GenerateLineageFolders called with empty root path");
            return;
        }

        var rootPersonId = _folderTreeDeps.SettingsStore.GetRootPersonId(rootPath);
        if (rootPersonId == Guid.Empty)
        {
            ErrorMessage = GenerateLineageNoRootMessage;
            return;
        }

        IsBusy = true;
        try
        {
            var result = _folderTreeDeps.LineageGenerator.Generate(rootPath, rootPersonId);
            ErrorMessage = string.Empty;
            StatusMessage = string.Format(GenerateLineageSuccessTemplate, result.Written);
        }
        catch (TreeIntegrityException ex)
        {
            _log.Error(ex, "GenerateLineageFolders: tree integrity violation");
            ErrorMessage = GenerateLineageIntegrityErrorMessage;
            StatusMessage = string.Empty;
        }
        catch (Exception ex)
        {
            _log.Error(ex, "GenerateLineageFolders failed");
            ErrorMessage = GenerateLineageErrorMessage;
            StatusMessage = string.Empty;
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void ValidateTree()
    {
        var rootPath = _rootPointerStore.Read();
        if (string.IsNullOrWhiteSpace(rootPath))
        {
            _log.Warning("ValidateTree called with empty root path");
            return;
        }

        try
        {
            var people = BuildPeopleMap(rootPath);
            var issues = _validationDeps.Validator.Validate(people);
            var messages = _validationDeps.Formatter.Format(issues, people);
            _validationDeps.ReportService.Show(messages);
            ErrorMessage = string.Empty;
        }
        catch (Exception ex)
        {
            _log.Error(ex, "ValidateTree failed");
            ErrorMessage = ValidateTreeErrorMessage;
        }
    }

    private void ResetToBlank()
    {
        var rootPath = _rootPointerStore.Read();
        var people = string.IsNullOrWhiteSpace(rootPath)
            ? new System.Collections.Generic.List<PersonSummary>()
            : _editDeps.DirectoryService.GetAll(rootPath);

        Person.Reset(new MeFile());
        Dates.Reset(new MeFile());
        Family.Reset(new MeFile(), people);
        Notes.Reset(new MeFile());
    }

    private Dictionary<Guid, MeFile> BuildPeopleMap(string rootPath)
    {
        var map = new Dictionary<Guid, MeFile>();

        foreach (var path in _validationDeps.Processor.ScanMeFiles(rootPath))
        {
            try
            {
                var meFile = _validationDeps.Processor.ReadMeFile(path);
                if (meFile.UniqueIdentifier != Guid.Empty)
                {
                    map[meFile.UniqueIdentifier] = meFile;
                }
            }
            catch (Exception ex)
            {
                _log.Error(ex, "BuildPeopleMap: failed to read {Path}", path);
            }
        }

        return map;
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

    private static string BuildWindowTitle(AppMode mode, string personName = null)
    {
        var modeLabel = mode switch
        {
            AppMode.Add => "Nowa osoba",
            AppMode.EditTree => "Edycja osoby",
            AppMode.EditDraft => "Edycja szkicu",
            _ => string.Empty
        };

        return string.IsNullOrEmpty(personName)
            ? $"TreeManager — {modeLabel}"
            : $"TreeManager — {modeLabel}: {personName}";
    }

    private static string BuildPromoteSummary(MeFile meFile)
    {
        return $"Imię i nazwisko: {meFile.FirstName} {meFile.LastName}\nData urodzenia: {meFile.DatesOfBirth}";
    }
}
