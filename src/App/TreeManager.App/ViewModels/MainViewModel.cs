using System;
using System.Collections.Generic;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Serilog;
using TreeManager.App.Commands;
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
    private const string SetRootPersonSuccessTemplate = "Wybrano osobę główną: {0}.";
    private const string SetRootPersonNoRootMessage = "Nie wybrano folderu z danymi.";
    private const string ChangeRootFolderSuccessMessage = "Zmieniono folder z danymi.";
    private const string EmptyRootMessage = "Nie wybrano folderu z danymi.";
    private const string DialogTitleInfo = "Informacja";
    private const string DialogTitleBłąd = "Błąd";
    private const string SavePersonDialogTitle = "Zapisano osobę";
    private const string SaveDraftDialogTitle = "Zapisano szkic";
    private const string UpdateDraftDialogTitle = "Zaktualizowano szkic";
    private const string GenerateTreeDialogTitle = "Generowanie drzewa";
    private const string GenerateLineageDialogTitle = "Generowanie rodów";
    private const string MinRelationshipMessage = "Osoba musi mieć przynajmniej jedną relację.";

    public PersonViewModel Person { get; }
    public DatesTabViewModel Dates { get; }
    public FamilyTabViewModel Family { get; }
    public NotesTabViewModel Notes { get; }

    private readonly IPersonRepository _personRepository;
    private readonly IRootPointerStore _rootPointerStore;
    private readonly IRootPickerService _rootPickerService;
    private readonly ICrashReporter _crashReporter;
    private readonly PersonEditDependencies _editDeps;
    private readonly FolderTreeCommandDependencies _folderTreeDeps;
    private readonly ValidationCommandDependencies _validationDeps;
    private readonly IUserJournalService _journal;
    private readonly ILogger _log;

    private MeFile _originalSnapshot;

    public IRelayCommand SwitchModeCommand { get; }
    public IRelayCommand OpenPersonCommand { get; }
    public IRelayCommand SaveCommand { get; }
    public IRelayCommand SaveNewPersonCommand { get; }
    public IRelayCommand SaveTreeChangesCommand { get; }
    public IRelayCommand SaveAsDraftCommand { get; }
    public IRelayCommand UpdateDraftCommand { get; }
    public IRelayCommand LoadDraftCommand { get; }
    public IRelayCommand PromoteDraftCommand { get; }
    public IRelayCommand OpenDataFolderCommand { get; }
    public IRelayCommand GenerateFolderTreeCommand { get; }
    public IRelayCommand GenerateLineageFoldersCommand { get; }
    public IRelayCommand ValidateTreeCommand { get; }
    public IRelayCommand SetRootPersonCommand { get; }
    public IRelayCommand ChangeRootFolderCommand { get; }
    public IRelayCommand SendTestReportCommand { get; }

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
        ILogger log,
        IRootPickerService rootPickerService,
        ICrashReporter crashReporter,
        IUserJournalService journal)
    {
        Person = person;
        Dates = dates;
        Family = family;
        Notes = notes;
        _personRepository = personRepository;
        _rootPointerStore = rootPointerStore;
        _rootPickerService = rootPickerService;
        _crashReporter = crashReporter;
        _editDeps = editDeps;
        _folderTreeDeps = folderTreeDeps;
        _validationDeps = validationDeps;
        _journal = journal;
        _log = log;

        SwitchModeCommand = new JournalingRelayCommand(
            inner: new RelayCommand<AppMode>(SwitchModeCore),
            journal: _journal,
            label: "Nowa osoba",
            personLabelProvider: () => Family.LoadedPersonId?.ToString() ?? "-");

        OpenPersonCommand = new JournalingRelayCommand(
            inner: new RelayCommand(OpenPersonCore),
            journal: _journal,
            label: "Edytuj osobę z drzewa",
            personLabelProvider: () => Family.LoadedPersonId?.ToString() ?? "-");

        SaveCommand = new RelayCommand(SaveCore, CanSave);

        SaveNewPersonCommand = new JournalingRelayCommand(
            inner: new RelayCommand(SaveCore, () => CurrentMode == AppMode.Add),
            journal: _journal,
            label: "Zapisz osobę i dodaj do drzewa",
            personLabelProvider: () => Family.LoadedPersonId?.ToString() ?? "-");

        SaveTreeChangesCommand = new JournalingRelayCommand(
            inner: new RelayCommand(SaveCore, () => CurrentMode == AppMode.EditTree),
            journal: _journal,
            label: "Zapisz zmiany dla osoby na drzewie",
            personLabelProvider: () => Family.LoadedPersonId?.ToString() ?? "-");

        SaveAsDraftCommand = new JournalingRelayCommand(
            inner: new RelayCommand(SaveAsDraftCore, CanSaveAsDraft),
            journal: _journal,
            label: "Zapisz osobę jako szkic",
            personLabelProvider: () => Family.LoadedPersonId?.ToString() ?? "-");

        UpdateDraftCommand = new JournalingRelayCommand(
            inner: new RelayCommand(UpdateDraftCore, CanUpdateDraft),
            journal: _journal,
            label: "Zaktualizuj szkic osoby",
            personLabelProvider: () => Family.LoadedPersonId?.ToString() ?? "-");

        LoadDraftCommand = new JournalingRelayCommand(
            inner: new RelayCommand(LoadDraftCore),
            journal: _journal,
            label: "Wczytaj szkic osoby",
            personLabelProvider: () => Family.LoadedPersonId?.ToString() ?? "-");

        PromoteDraftCommand = new JournalingRelayCommand(
            inner: new RelayCommand(PromoteDraftCore, CanPromoteDraft),
            journal: _journal,
            label: "Dodaj szkic osoby do drzewa",
            personLabelProvider: () => Family.LoadedPersonId?.ToString() ?? "-");

        OpenDataFolderCommand = new JournalingRelayCommand(
            inner: new RelayCommand(OpenDataFolderCore),
            journal: _journal,
            label: "Otwórz folder z danymi",
            personLabelProvider: () => Family.LoadedPersonId?.ToString() ?? "-");

        GenerateFolderTreeCommand = new JournalingRelayCommand(
            inner: new RelayCommand(GenerateFolderTreeCore),
            journal: _journal,
            label: "Generuj Drzewo",
            personLabelProvider: () => Family.LoadedPersonId?.ToString() ?? "-");

        GenerateLineageFoldersCommand = new JournalingRelayCommand(
            inner: new RelayCommand(GenerateLineageFoldersCore),
            journal: _journal,
            label: "Generuj Rody",
            personLabelProvider: () => Family.LoadedPersonId?.ToString() ?? "-");

        ValidateTreeCommand = new JournalingRelayCommand(
            inner: new RelayCommand(ValidateTreeCore),
            journal: _journal,
            label: "Sprawdź spójność",
            personLabelProvider: () => Family.LoadedPersonId?.ToString() ?? "-");

        SetRootPersonCommand = new JournalingRelayCommand(
            inner: new RelayCommand(SetRootPersonCore),
            journal: _journal,
            label: "Wybierz osobę główną",
            personLabelProvider: () => Family.LoadedPersonId?.ToString() ?? "-");

        ChangeRootFolderCommand = new JournalingRelayCommand(
            inner: new RelayCommand(ChangeRootFolderCore),
            journal: _journal,
            label: "Zmień folder",
            personLabelProvider: () => Family.LoadedPersonId?.ToString() ?? "-");

        SendTestReportCommand = new JournalingRelayCommand(
            inner: new RelayCommand(SendTestReportCore),
            journal: _journal,
            label: "Wyślij raport o błędzie",
            personLabelProvider: () => Family.LoadedPersonId?.ToString() ?? "-");

        ResetToBlank();
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
    private string _windowTitle = "TreeManager — Dodawanie nowej osoby";

    partial void OnCurrentModeChanged(AppMode value)
    {
        WindowTitle = BuildWindowTitle(value);
        SaveCommand.NotifyCanExecuteChanged();
        SaveNewPersonCommand.NotifyCanExecuteChanged();
        SaveTreeChangesCommand.NotifyCanExecuteChanged();
        SaveAsDraftCommand.NotifyCanExecuteChanged();
        UpdateDraftCommand.NotifyCanExecuteChanged();
        PromoteDraftCommand.NotifyCanExecuteChanged();
    }

    private void SwitchModeCore(AppMode targetMode)
    {
        if (targetMode == CurrentMode)
        {
            return;
        }

        var rootPath = _rootPointerStore.Read();
        var current = string.IsNullOrWhiteSpace(rootPath)
            ? AssembleCurrentMeFile()
            : ApplyIdentityOverlay(AssembleCurrentMeFile(), rootPath);
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

    private void OpenPersonCore()
    {
        var rootPath = _rootPointerStore.Read();
        if (string.IsNullOrWhiteSpace(rootPath))
        {
            _log.Warning("OpenPerson called with empty root path");
            ErrorMessage = EmptyRootMessage;
            return;
        }

        var people = _editDeps.DirectoryService.GetAll(rootPath);
        var selected = _editDeps.PickerService.PickPerson(people);
        if (selected == null)
        {
            return;
        }

        var currentSnapshot = ApplyIdentityOverlay(AssembleCurrentMeFile(), rootPath);
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

    private void SaveCore()
    {
        StatusMessage = string.Empty;
        IsBusy = true;
        try
        {
            var rootPath = _rootPointerStore.Read();
            if (string.IsNullOrWhiteSpace(rootPath))
            {
                _log.Warning("Save called with empty root path");
                ErrorMessage = EmptyRootMessage;
                return;
            }

            // 1. Validate minimum relationship requirement
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
                    ErrorMessage = MinRelationshipMessage;
                    _editDeps.InfoDialog.Show(DialogTitleBłąd, MinRelationshipMessage);
                    return;
                }
            }

            var meFile = AssembleCurrentMeFile();
            meFile = ApplyIdentityOverlay(meFile, rootPath);

            // 2. Pre-save confirm
            var summary = BuildSaveSummary(meFile);
            if (!_editDeps.PromoteConfirmService.Confirm(summary))
            {
                return;
            }

            if (_originalSnapshot == null)
            {
                // 3. Create new person
                _personRepository.Create(meFile, rootPath);
                _originalSnapshot = meFile;
                CurrentMode = AppMode.EditTree;
                WindowTitle = BuildWindowTitle(AppMode.EditTree, Person.ToFolderName());
            }
            else
            {
                // 3. Update existing person
                _personRepository.Update(meFile, _originalSnapshot, rootPath);
                _originalSnapshot = meFile;
            }

            Family.LoadedPersonId = meFile.UniqueIdentifier;
            ErrorMessage = string.Empty;
            StatusMessage = SavePersonSuccessMessage;

            // 4. Post-save success dialog
            _editDeps.InfoDialog.Show(SavePersonDialogTitle, meFile.Location);
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

    private bool CanSave() => CurrentMode == AppMode.Add || CurrentMode == AppMode.EditTree;

    private void SaveAsDraftCore()
    {
        StatusMessage = string.Empty;
        var rootPath = _rootPointerStore.Read();
        if (string.IsNullOrWhiteSpace(rootPath))
        {
            _log.Warning("SaveAsDraft called with empty root path");
            ErrorMessage = EmptyRootMessage;
            return;
        }

        try
        {
            var meFile = AssembleCurrentMeFile();
            meFile = ApplyIdentityOverlay(meFile, rootPath);

            _editDeps.DraftRepository.SaveDraft(meFile, rootPath);
            _originalSnapshot = meFile;
            ErrorMessage = string.Empty;
            StatusMessage = SaveDraftSuccessMessage;
            CurrentMode = AppMode.EditDraft;
            _editDeps.InfoDialog.Show(SaveDraftDialogTitle, SaveDraftSuccessMessage);
        }
        catch (Exception ex)
        {
            _log.Error(ex, "SaveAsDraft failed");
            ErrorMessage = "Nie udało się zapisać szkicu. Spróbuj ponownie.";
        }
    }

    private bool CanSaveAsDraft() => CurrentMode == AppMode.Add;

    private void UpdateDraftCore()
    {
        StatusMessage = string.Empty;
        var rootPath = _rootPointerStore.Read();
        if (string.IsNullOrWhiteSpace(rootPath))
        {
            _log.Warning("UpdateDraft called with empty root path");
            ErrorMessage = EmptyRootMessage;
            return;
        }

        try
        {
            var meFile = AssembleCurrentMeFile();
            meFile = ApplyIdentityOverlay(meFile, rootPath);

            _editDeps.DraftRepository.SaveDraft(meFile, rootPath);
            _originalSnapshot = meFile;
            ErrorMessage = string.Empty;
            StatusMessage = SaveDraftSuccessMessage;
            _editDeps.InfoDialog.Show(UpdateDraftDialogTitle, SaveDraftSuccessMessage);
        }
        catch (Exception ex)
        {
            _log.Error(ex, "UpdateDraft failed");
            ErrorMessage = "Nie udało się zaktualizować szkicu. Spróbuj ponownie.";
        }
    }

    private bool CanUpdateDraft() => CurrentMode == AppMode.EditDraft;

    private void LoadDraftCore()
    {
        var rootPath = _rootPointerStore.Read();
        if (string.IsNullOrWhiteSpace(rootPath))
        {
            _log.Warning("LoadDraft called with empty root path");
            ErrorMessage = EmptyRootMessage;
            return;
        }

        var current = ApplyIdentityOverlay(AssembleCurrentMeFile(), rootPath);
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

    private void PromoteDraftCore()
    {
        var rootPath = _rootPointerStore.Read();
        if (string.IsNullOrWhiteSpace(rootPath))
        {
            _log.Warning("PromoteDraft called with empty root path");
            ErrorMessage = EmptyRootMessage;
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

    private void OpenDataFolderCore()
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

    private void GenerateFolderTreeCore()
    {
        var rootPath = _rootPointerStore.Read();
        if (string.IsNullOrWhiteSpace(rootPath))
        {
            _log.Warning("GenerateFolderTree called with empty root path");
            _editDeps.InfoDialog.Show(GenerateTreeDialogTitle, EmptyRootMessage);
            return;
        }

        var rootPersonId = _folderTreeDeps.SettingsStore.GetRootPersonId(rootPath);
        if (rootPersonId == Guid.Empty)
        {
            ErrorMessage = GenerateFolderTreeNoRootMessage;
            _editDeps.InfoDialog.Show(GenerateTreeDialogTitle, GenerateFolderTreeNoRootMessage);
            return;
        }

        IsBusy = true;
        try
        {
            var result = _folderTreeDeps.Generator.Generate(rootPath, rootPersonId);
            ErrorMessage = string.Empty;
            StatusMessage = string.Format(GenerateFolderTreeSuccessTemplate, result.Written);
            _editDeps.InfoDialog.Show(GenerateTreeDialogTitle, string.Format(GenerateFolderTreeSuccessTemplate, result.Written));
        }
        catch (Exception ex)
        {
            _log.Error(ex, "GenerateFolderTree failed");
            ErrorMessage = GenerateFolderTreeErrorMessage;
            StatusMessage = string.Empty;
            _editDeps.InfoDialog.Show(GenerateTreeDialogTitle, GenerateFolderTreeErrorMessage);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void GenerateLineageFoldersCore()
    {
        var rootPath = _rootPointerStore.Read();
        if (string.IsNullOrWhiteSpace(rootPath))
        {
            _log.Warning("GenerateLineageFolders called with empty root path");
            _editDeps.InfoDialog.Show(GenerateLineageDialogTitle, EmptyRootMessage);
            return;
        }

        var rootPersonId = _folderTreeDeps.SettingsStore.GetRootPersonId(rootPath);
        if (rootPersonId == Guid.Empty)
        {
            ErrorMessage = GenerateLineageNoRootMessage;
            _editDeps.InfoDialog.Show(GenerateLineageDialogTitle, GenerateLineageNoRootMessage);
            return;
        }

        IsBusy = true;
        try
        {
            var result = _folderTreeDeps.LineageGenerator.Generate(rootPath, rootPersonId);
            ErrorMessage = string.Empty;
            StatusMessage = string.Format(GenerateLineageSuccessTemplate, result.Written);
            _editDeps.InfoDialog.Show(GenerateLineageDialogTitle, string.Format(GenerateLineageSuccessTemplate, result.Written));
        }
        catch (TreeIntegrityException ex)
        {
            _log.Error(ex, "GenerateLineageFolders: tree integrity violation");
            ErrorMessage = GenerateLineageIntegrityErrorMessage;
            StatusMessage = string.Empty;
            _editDeps.InfoDialog.Show(GenerateLineageDialogTitle, GenerateLineageIntegrityErrorMessage);
        }
        catch (Exception ex)
        {
            _log.Error(ex, "GenerateLineageFolders failed");
            ErrorMessage = GenerateLineageErrorMessage;
            StatusMessage = string.Empty;
            _editDeps.InfoDialog.Show(GenerateLineageDialogTitle, GenerateLineageErrorMessage);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void ValidateTreeCore()
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

    private void SetRootPersonCore()
    {
        var rootPath = _rootPointerStore.Read();
        if (string.IsNullOrWhiteSpace(rootPath))
        {
            _log.Warning("SetRootPerson called with empty root path");
            ErrorMessage = SetRootPersonNoRootMessage;
            return;
        }

        var people = _editDeps.DirectoryService.GetAll(rootPath);
        var selected = _editDeps.PickerService.PickPerson(people);
        if (selected == null)
        {
            return;
        }

        _folderTreeDeps.SettingsStore.SetRootPersonId(rootPath, selected.UniqueIdentifier);
        ErrorMessage = string.Empty;
        StatusMessage = string.Format(SetRootPersonSuccessTemplate, selected.DisplayName);
    }

    private void ChangeRootFolderCore()
    {
        var newRoot = _rootPickerService.PickRoot();
        if (string.IsNullOrWhiteSpace(newRoot))
        {
            return;
        }

        _rootPointerStore.Write(newRoot);
        ResetToBlank();
        ErrorMessage = string.Empty;
        StatusMessage = ChangeRootFolderSuccessMessage;
    }

    private void SendTestReportCore()
    {
        _crashReporter.ReportManual("Ręczny raport z aplikacji.");
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
            AppMode.Add => "Dodawanie nowej osoby",
            AppMode.EditTree => "Edycja osoby z drzewa",
            AppMode.EditDraft => "Edycja szkicu osoby",
            _ => string.Empty
        };

        return string.IsNullOrEmpty(personName)
            ? $"TreeManager — {modeLabel}"
            : $"TreeManager — {modeLabel}: {personName}";
    }

    private static string BuildPromoteSummary(MeFile meFile)
    {
        return $"Imię i nazwisko: {meFile.FirstName} {meFile.LastName}\n"
             + $"Data urodzenia: {meFile.DatesOfBirth}\n"
             + $"Rodzice: {meFile.ParentsId.Count}\n"
             + $"Małżonkowie: {meFile.SpouseId.Count}\n"
             + $"Dzieci: {meFile.ChildrenId.Count}";
    }

    private static string BuildSaveSummary(MeFile meFile)
    {
        return $"Nowa osoba:\n{meFile.FirstName} {meFile.LastName}\n"
             + $"Rodzice: {meFile.ParentsId.Count}\n"
             + $"Małżonkowie: {meFile.SpouseId.Count}\n"
             + $"Dzieci: {meFile.ChildrenId.Count}\n\nZapisać?";
    }
}
