using System;
using System.Collections.Generic;
using System.IO;
using Moq;
using Serilog;
using TreeManager.App.Services;
using TreeManager.App.ViewModels;
using TreeManager.App.Converters;
using TreeManager.Common.TestUtilities;
using TreeManager.Core.Abstractions.Persistence;
using TreeManager.Core.Abstractions.Services;
using TreeManager.Core.Abstractions.Settings;
using TreeManager.Core.Abstractions.Validation;
using TreeManager.Core.Domain;
using TreeManager.Core.Validation;

namespace TreeManager.App.L0.ViewModels;

public class MainViewModelTests
{
    private const string FakeRoot = @"C:\fake\root";

    private readonly Mock<IPersonRepository> _mockPersonRepository;
    private readonly Mock<IRootPointerStore> _mockRootPointerStore;
    private readonly Mock<IPersonDirectoryService> _mockDirectoryService;
    private readonly Mock<IPersonPickerService> _mockPickerService;
    private readonly Mock<IPersonLoaderService> _mockLoaderService;
    private readonly Mock<IDirtyTracker> _mockDirtyTracker;
    private readonly Mock<IDirtyGuardService> _mockDirtyGuard;
    private readonly Mock<IDraftRepository> _mockDraftRepository;
    private readonly Mock<IDraftPromoter> _mockDraftPromoter;
    private readonly Mock<IFolderTreeGenerator> _mockFolderTreeGenerator;
    private readonly Mock<IFolderTreeSettingsStore> _mockFolderTreeSettings;
    private readonly Mock<ILineageFolderGenerator> _mockLineageFolderGenerator;
    private readonly Mock<ITreeConsistencyValidator> _mockValidator;
    private readonly Mock<IValidationMessageFormatter> _mockFormatter;
    private readonly Mock<IValidationReportService> _mockReportService;
    private readonly Mock<IMeFileProcessor> _mockProcessor;
    private readonly Mock<IPromoteConfirmService> _mockPromoteConfirm;
    private readonly Mock<IFolderRevealService> _mockFolderReveal;
    private readonly Mock<ILogger> _mockLog;
    private readonly MainViewModel _sut;

    public MainViewModelTests()
    {
        _mockPersonRepository = new Mock<IPersonRepository>();
        _mockRootPointerStore = new Mock<IRootPointerStore>();
        _mockDirectoryService = new Mock<IPersonDirectoryService>();
        _mockPickerService = new Mock<IPersonPickerService>();
        _mockLoaderService = new Mock<IPersonLoaderService>();
        _mockDirtyTracker = new Mock<IDirtyTracker>();
        _mockDirtyGuard = new Mock<IDirtyGuardService>();
        _mockDraftRepository = new Mock<IDraftRepository>();
        _mockDraftPromoter = new Mock<IDraftPromoter>();
        _mockFolderTreeGenerator = new Mock<IFolderTreeGenerator>();
        _mockFolderTreeSettings = new Mock<IFolderTreeSettingsStore>();
        _mockLineageFolderGenerator = new Mock<ILineageFolderGenerator>();
        _mockValidator = new Mock<ITreeConsistencyValidator>();
        _mockFormatter = new Mock<IValidationMessageFormatter>();
        _mockReportService = new Mock<IValidationReportService>();
        _mockProcessor = new Mock<IMeFileProcessor>();
        _mockPromoteConfirm = new Mock<IPromoteConfirmService>();
        _mockFolderReveal = new Mock<IFolderRevealService>();
        _mockLog = new Mock<ILogger>();

        _mockRootPointerStore.Setup(x => x.Read()).Returns(FakeRoot);

        // Default: not dirty — existing tests proceed unchanged
        _mockDirtyTracker.Setup(x => x.IsDirty(It.IsAny<MeFile>(), It.IsAny<MeFile>())).Returns(false);

        // Default: FolderTreeGenerator.Generate returns success
        _mockFolderTreeGenerator
            .Setup(g => g.Generate(It.IsAny<string>(), It.IsAny<Guid>()))
            .Returns((5, new List<string>()));

        // Default: LineageFolderGenerator.Generate returns success
        _mockLineageFolderGenerator
            .Setup(g => g.Generate(It.IsAny<string>(), It.IsAny<Guid>()))
            .Returns((3, new List<string>()));

        // Default: validator returns no issues
        _mockValidator
            .Setup(v => v.Validate(It.IsAny<IReadOnlyDictionary<Guid, MeFile>>()))
            .Returns(new List<ValidationIssue>());

        // Default: formatter returns empty list
        _mockFormatter
            .Setup(f => f.Format(It.IsAny<IReadOnlyList<ValidationIssue>>(), It.IsAny<IReadOnlyDictionary<Guid, MeFile>>()))
            .Returns(new List<string>());

        // Default: ScanMeFiles returns empty
        _mockProcessor
            .Setup(p => p.ScanMeFiles(It.IsAny<string>()))
            .Returns(new List<string>());

        // Default: promote confirm accepts — keeps all existing PromoteDraft tests green
        _mockPromoteConfirm.Setup(s => s.Confirm(It.IsAny<string>())).Returns(true);

        // Default: folder reveal is a no-op

        var deps = new PersonEditDependencies(
            _mockDirectoryService.Object,
            _mockPickerService.Object,
            _mockLoaderService.Object,
            _mockDirtyTracker.Object,
            _mockDirtyGuard.Object,
            _mockDraftRepository.Object,
            _mockDraftPromoter.Object,
            _mockPromoteConfirm.Object,
            _mockFolderReveal.Object);

        var folderTreeDeps = new FolderTreeCommandDependencies(
            _mockFolderTreeGenerator.Object,
            _mockFolderTreeSettings.Object,
            _mockLineageFolderGenerator.Object);

        var validationDeps = new ValidationCommandDependencies(
            _mockValidator.Object,
            _mockFormatter.Object,
            _mockReportService.Object,
            _mockProcessor.Object);

        _sut = new MainViewModel(
            new PersonViewModel(),
            new DatesTabViewModel(),
            new FamilyTabViewModel(),
            new NotesTabViewModel(),
            _mockPersonRepository.Object,
            _mockRootPointerStore.Object,
            deps,
            folderTreeDeps,
            validationDeps,
            _mockLog.Object);
    }

    #region SwitchMode

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void SwitchMode_UpdatesCurrentMode_WhenInvokedWithTarget()
    {
        //Arrange
        //Act
        _sut.SwitchModeCommand.Execute(AppMode.EditTree);

        //Assert
        Assert.Equal(AppMode.EditTree, _sut.CurrentMode);
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void SwitchMode_RaisesPropertyChanged_WhenModeChanges()
    {
        //Arrange
        string raisedFor = null;
        _sut.PropertyChanged += (_, e) => raisedFor = e.PropertyName;

        //Act
        _sut.SwitchModeCommand.Execute(AppMode.EditDraft);

        //Assert
        Assert.Equal(nameof(MainViewModel.CurrentMode), raisedFor);
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void SwitchMode_DoesNotRaisePropertyChanged_WhenTargetEqualsCurrent()
    {
        //Arrange
        int raiseCount = 0;
        _sut.PropertyChanged += (_, _) => raiseCount++;

        //Act
        _sut.SwitchModeCommand.Execute(AppMode.Add);

        //Assert
        Assert.Equal(0, raiseCount);
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void SwitchMode_ClearsOriginalSnapshot_WhenSwitchingToAdd()
    {
        //Arrange — put sut into edit mode by simulating a successful load
        var selectedPerson = new PersonSummary(Guid.NewGuid(), "Jan Kowalski");
        var loadedMeFile = new MeFile { UniqueIdentifier = selectedPerson.UniqueIdentifier, PersonName = "Jan Kowalski" };
        _mockPickerService
            .Setup(x => x.PickPerson(It.IsAny<System.Collections.Generic.IReadOnlyList<PersonSummary>>()))
            .Returns(selectedPerson);
        _mockLoaderService
            .Setup(x => x.Load(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<PersonViewModel>(), It.IsAny<DatesTabViewModel>(), It.IsAny<FamilyTabViewModel>(), It.IsAny<NotesTabViewModel>()))
            .Returns(loadedMeFile);
        _mockDirectoryService
            .Setup(x => x.GetAll(FakeRoot))
            .Returns(new System.Collections.Generic.List<PersonSummary>());
        _sut.OpenPersonCommand.Execute(null);

        //Act — switch back to Add mode
        _sut.SwitchModeCommand.Execute(AppMode.Add);
        AddOneRelationship();
        _sut.SaveCommand.Execute(null);

        //Assert — should call Create (snapshot cleared), not Update
        _mockPersonRepository.Verify(x => x.Create(It.IsAny<MeFile>(), FakeRoot), Times.Once());
        _mockPersonRepository.Verify(x => x.Update(It.IsAny<MeFile>(), It.IsAny<MeFile>(), It.IsAny<string>()), Times.Never());
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void SwitchMode_DoesNotSwitch_WhenDirtyAndUserCancels()
    {
        //Arrange — simulate loaded person, then mark dirty
        SimulateLoadedPerson();
        _mockDirtyTracker.Setup(x => x.IsDirty(It.IsAny<MeFile>(), It.IsAny<MeFile>())).Returns(true);
        _mockDirtyGuard.Setup(x => x.ConfirmDiscard()).Returns(false);

        //Act
        _sut.SwitchModeCommand.Execute(AppMode.Add);

        //Assert — mode unchanged (was EditTree after load)
        Assert.Equal(AppMode.EditTree, _sut.CurrentMode);
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void SwitchMode_Switches_WhenDirtyAndUserConfirmsDiscard()
    {
        //Arrange
        SimulateLoadedPerson();
        _mockDirtyTracker.Setup(x => x.IsDirty(It.IsAny<MeFile>(), It.IsAny<MeFile>())).Returns(true);
        _mockDirtyGuard.Setup(x => x.ConfirmDiscard()).Returns(true);

        //Act
        _sut.SwitchModeCommand.Execute(AppMode.Add);

        //Assert
        Assert.Equal(AppMode.Add, _sut.CurrentMode);
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void SwitchMode_DoesNotShowGuard_WhenNotDirty()
    {
        //Arrange
        SimulateLoadedPerson();
        _mockDirtyTracker.Setup(x => x.IsDirty(It.IsAny<MeFile>(), It.IsAny<MeFile>())).Returns(false);

        //Act
        _sut.SwitchModeCommand.Execute(AppMode.Add);

        //Assert — guard never called
        _mockDirtyGuard.Verify(x => x.ConfirmDiscard(), Times.Never());
    }

    #endregion

    #region Constructor

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void Constructor_InitializesCurrentModeToAdd_WhenInstantiated()
    {
        Assert.Equal(AppMode.Add, _sut.CurrentMode);
    }

    #endregion

    #region Save

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void Save_CallsRepositoryCreate_WhenInvoked()
    {
        //Arrange
        _sut.Person.FirstName = "Jan";
        _sut.Person.LastName = "Kowalski";
        AddOneRelationship();

        //Act
        _sut.SaveCommand.Execute(null);

        //Assert
        _mockPersonRepository.Verify(x => x.Create(It.IsAny<MeFile>(), FakeRoot), Times.Once());
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void Save_SetsIsBusyFalseAfterExecution_WhenCommandCompletes()
    {
        //Arrange
        //Act
        _sut.SaveCommand.Execute(null);

        //Assert
        Assert.False(_sut.IsBusy);
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void Save_SetsLoadedPersonId_AfterSuccessfulSave()
    {
        //Arrange
        _sut.Person.FirstName = "Maria";
        _sut.Person.LastName = "Kowalska";
        AddOneRelationship();

        //Act
        _sut.SaveCommand.Execute(null);

        //Assert
        Assert.True(_sut.Family.LoadedPersonId.HasValue);
        Assert.NotEqual(Guid.Empty, _sut.Family.LoadedPersonId.Value);
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void Save_AssignsNewGuid_WhenUniqueIdentifierIsEmpty()
    {
        //Arrange
        _sut.Person.UniqueIdentifier = Guid.Empty;
        AddOneRelationship();

        //Act
        _sut.SaveCommand.Execute(null);

        //Assert
        _mockPersonRepository.Verify(
            x => x.Create(It.Is<MeFile>(m => m.UniqueIdentifier != Guid.Empty), FakeRoot),
            Times.Once());
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void Save_SetsIsBusyFalse_WhenRepositoryThrows()
    {
        //Arrange
        _mockPersonRepository
            .Setup(x => x.Create(It.IsAny<MeFile>(), It.IsAny<string>()))
            .Throws<IOException>();
        AddOneRelationship();

        //Act + Assert — must not throw; IsBusy must be reset
        _sut.SaveCommand.Execute(null);
        Assert.False(_sut.IsBusy);
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void Save_CallsUpdate_WhenPersonWasPreviouslyLoaded()
    {
        //Arrange — load a person to set the snapshot
        var selectedPerson = new PersonSummary(Guid.NewGuid(), "Jan Kowalski");
        var loadedMeFile = new MeFile { UniqueIdentifier = selectedPerson.UniqueIdentifier, PersonName = "Jan Kowalski" };
        _mockPickerService
            .Setup(x => x.PickPerson(It.IsAny<System.Collections.Generic.IReadOnlyList<PersonSummary>>()))
            .Returns(selectedPerson);
        _mockLoaderService
            .Setup(x => x.Load(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<PersonViewModel>(), It.IsAny<DatesTabViewModel>(), It.IsAny<FamilyTabViewModel>(), It.IsAny<NotesTabViewModel>()))
            .Returns(loadedMeFile);
        _mockDirectoryService
            .Setup(x => x.GetAll(FakeRoot))
            .Returns(new System.Collections.Generic.List<PersonSummary>());
        _sut.OpenPersonCommand.Execute(null);
        AddOneRelationship();

        //Act
        _sut.SaveCommand.Execute(null);

        //Assert
        _mockPersonRepository.Verify(x => x.Update(It.IsAny<MeFile>(), loadedMeFile, FakeRoot), Times.Once());
        _mockPersonRepository.Verify(x => x.Create(It.IsAny<MeFile>(), It.IsAny<string>()), Times.Never());
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void Save_CallsRepositoryCreate_WhenOriginalSnapshotIsNull()
    {
        //Arrange — no load performed; snapshot is null
        AddOneRelationship();

        //Act
        _sut.SaveCommand.Execute(null);

        //Assert
        _mockPersonRepository.Verify(x => x.Create(It.IsAny<MeFile>(), FakeRoot), Times.Once());
        _mockPersonRepository.Verify(x => x.Update(It.IsAny<MeFile>(), It.IsAny<MeFile>(), It.IsAny<string>()), Times.Never());
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void Save_SetsErrorMessage_WhenRepositoryThrows()
    {
        //Arrange
        _mockPersonRepository
            .Setup(x => x.Create(It.IsAny<MeFile>(), It.IsAny<string>()))
            .Throws<IOException>();
        AddOneRelationship();

        //Act
        _sut.SaveCommand.Execute(null);

        //Assert
        Assert.False(string.IsNullOrEmpty(_sut.ErrorMessage));
        _mockLog.Verify(x => x.Error(It.IsAny<Exception>(), It.IsAny<string>()), Times.Once());
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void Save_ClearsErrorMessage_OnSuccessfulSave()
    {
        //Arrange — first make a failing save to set ErrorMessage
        AddOneRelationship();
        _mockPersonRepository
            .Setup(x => x.Create(It.IsAny<MeFile>(), It.IsAny<string>()))
            .Throws<IOException>();
        _sut.SaveCommand.Execute(null);

        //Act — now a successful save
        _mockPersonRepository
            .Setup(x => x.Create(It.IsAny<MeFile>(), It.IsAny<string>()));
        _sut.SaveCommand.Execute(null);

        //Assert
        Assert.True(string.IsNullOrEmpty(_sut.ErrorMessage));
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void Save_DoesNotSave_WhenRootPathIsEmpty()
    {
        //Arrange
        _mockRootPointerStore.Setup(x => x.Read()).Returns(string.Empty);

        //Act
        _sut.SaveCommand.Execute(null);

        //Assert
        _mockPersonRepository.Verify(x => x.Create(It.IsAny<MeFile>(), It.IsAny<string>()), Times.Never());
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void Save_UpdatesOriginalSnapshot_AfterSuccessfulUpdate()
    {
        //Arrange — load a person to set the snapshot
        var selectedPerson = new PersonSummary(Guid.NewGuid(), "Jan Kowalski");
        var loadedMeFile = new MeFile { UniqueIdentifier = selectedPerson.UniqueIdentifier, PersonName = "Jan Kowalski" };
        _mockPickerService
            .Setup(x => x.PickPerson(It.IsAny<System.Collections.Generic.IReadOnlyList<PersonSummary>>()))
            .Returns(selectedPerson);
        _mockLoaderService
            .Setup(x => x.Load(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<PersonViewModel>(), It.IsAny<DatesTabViewModel>(), It.IsAny<FamilyTabViewModel>(), It.IsAny<NotesTabViewModel>()))
            .Returns(loadedMeFile);
        _mockDirectoryService
            .Setup(x => x.GetAll(FakeRoot))
            .Returns(new System.Collections.Generic.List<PersonSummary>());
        _sut.OpenPersonCommand.Execute(null);
        AddOneRelationship();

        //Act — save once
        _sut.SaveCommand.Execute(null);
        // save second time — snapshot must be the MeFile from first save, not the original load
        _sut.SaveCommand.Execute(null);

        //Assert — Update called twice (not with stale original each time)
        _mockPersonRepository.Verify(x => x.Update(It.IsAny<MeFile>(), It.IsAny<MeFile>(), FakeRoot), Times.Exactly(2));
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void Save_DoesNotSave_WhenNoRelationshipsSelected()
    {
        //Arrange — tree has existing people so the guard fires
        _mockDirectoryService
            .Setup(x => x.GetAll(FakeRoot))
            .Returns(new System.Collections.Generic.List<PersonSummary> { new PersonSummary(Guid.NewGuid(), "Existing") });

        //Act
        _sut.SaveCommand.Execute(null);

        //Assert
        _mockPersonRepository.Verify(x => x.Create(It.IsAny<MeFile>(), It.IsAny<string>()), Times.Never());
        Assert.False(string.IsNullOrEmpty(_sut.ErrorMessage));
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void Save_Saves_WhenAtLeastOneRelationshipSelected()
    {
        //Arrange
        AddOneRelationship();

        //Act
        _sut.SaveCommand.Execute(null);

        //Assert
        _mockPersonRepository.Verify(x => x.Create(It.IsAny<MeFile>(), It.IsAny<string>()), Times.Once());
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void Save_Saves_WhenNoRelationshipsButTreeIsEmpty()
    {
        //Arrange — tree has no existing people; first person needs no relations
        _mockDirectoryService
            .Setup(x => x.GetAll(FakeRoot))
            .Returns(new System.Collections.Generic.List<PersonSummary>());

        //Act
        _sut.SaveCommand.Execute(null);

        //Assert
        _mockPersonRepository.Verify(x => x.Create(It.IsAny<MeFile>(), FakeRoot), Times.Once());
        Assert.True(string.IsNullOrEmpty(_sut.ErrorMessage));
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void Save_IncludesNotes_WhenSaving()
    {
        //Arrange
        _sut.Notes.Notes = "Ważna notatka";
        AddOneRelationship();

        //Act
        _sut.SaveCommand.Execute(null);

        //Assert
        _mockPersonRepository.Verify(
            x => x.Create(It.Is<MeFile>(m => m.Notes == "Ważna notatka"), FakeRoot),
            Times.Once());
    }

    #endregion

    #region OpenPerson

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void OpenPerson_SetsCurrentModeToEditTree_AfterSuccessfulLoad()
    {
        //Arrange
        var selectedPerson = new PersonSummary(Guid.NewGuid(), "Jan Kowalski");
        var loadedMeFile = new MeFile { UniqueIdentifier = selectedPerson.UniqueIdentifier, PersonName = "Jan Kowalski" };
        _mockPickerService
            .Setup(x => x.PickPerson(It.IsAny<System.Collections.Generic.IReadOnlyList<PersonSummary>>()))
            .Returns(selectedPerson);
        _mockLoaderService
            .Setup(x => x.Load(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<PersonViewModel>(), It.IsAny<DatesTabViewModel>(), It.IsAny<FamilyTabViewModel>(), It.IsAny<NotesTabViewModel>()))
            .Returns(loadedMeFile);
        _mockDirectoryService
            .Setup(x => x.GetAll(FakeRoot))
            .Returns(new System.Collections.Generic.List<PersonSummary>());

        //Act
        _sut.OpenPersonCommand.Execute(null);

        //Assert
        Assert.Equal(AppMode.EditTree, _sut.CurrentMode);
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void OpenPerson_DoesNotLoad_WhenDialogCancelled()
    {
        //Arrange
        _mockPickerService
            .Setup(x => x.PickPerson(It.IsAny<System.Collections.Generic.IReadOnlyList<PersonSummary>>()))
            .Returns((PersonSummary)null);
        _mockDirectoryService
            .Setup(x => x.GetAll(FakeRoot))
            .Returns(new System.Collections.Generic.List<PersonSummary>());

        //Act
        _sut.OpenPersonCommand.Execute(null);

        //Assert — loader not called; mode unchanged
        _mockLoaderService.Verify(
            x => x.Load(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<PersonViewModel>(), It.IsAny<DatesTabViewModel>(), It.IsAny<FamilyTabViewModel>(), It.IsAny<NotesTabViewModel>()),
            Times.Never());
        Assert.Equal(AppMode.Add, _sut.CurrentMode);
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void OpenPerson_DoesNotLoad_WhenRootPathIsEmpty()
    {
        //Arrange
        _mockRootPointerStore.Setup(x => x.Read()).Returns(string.Empty);

        //Act
        _sut.OpenPersonCommand.Execute(null);

        //Assert
        _mockLoaderService.Verify(
            x => x.Load(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<PersonViewModel>(), It.IsAny<DatesTabViewModel>(), It.IsAny<FamilyTabViewModel>(), It.IsAny<NotesTabViewModel>()),
            Times.Never());
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void OpenPerson_SetsErrorMessage_WhenLoaderThrows()
    {
        //Arrange
        var selectedPerson = new PersonSummary(Guid.NewGuid(), "Jan Kowalski");
        _mockPickerService
            .Setup(x => x.PickPerson(It.IsAny<System.Collections.Generic.IReadOnlyList<PersonSummary>>()))
            .Returns(selectedPerson);
        _mockDirectoryService
            .Setup(x => x.GetAll(FakeRoot))
            .Returns(new System.Collections.Generic.List<PersonSummary>());
        _mockLoaderService
            .Setup(x => x.Load(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<PersonViewModel>(), It.IsAny<DatesTabViewModel>(), It.IsAny<FamilyTabViewModel>(), It.IsAny<NotesTabViewModel>()))
            .Throws<InvalidOperationException>();

        //Act
        _sut.OpenPersonCommand.Execute(null);

        //Assert
        Assert.False(string.IsNullOrEmpty(_sut.ErrorMessage));
        Assert.False(_sut.IsBusy);
        _mockLog.Verify(x => x.Error(It.IsAny<Exception>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once());
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void OpenPerson_DoesNotShowGuard_WhenNotDirty()
    {
        //Arrange
        _mockDirtyTracker.Setup(x => x.IsDirty(It.IsAny<MeFile>(), It.IsAny<MeFile>())).Returns(false);
        SetupPickerAndLoader();

        //Act
        _sut.OpenPersonCommand.Execute(null);

        //Assert — guard never called
        _mockDirtyGuard.Verify(x => x.ConfirmDiscard(), Times.Never());
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void OpenPerson_ShowsGuard_WhenDirty()
    {
        //Arrange
        SimulateLoadedPerson();
        _mockDirtyTracker.Setup(x => x.IsDirty(It.IsAny<MeFile>(), It.IsAny<MeFile>())).Returns(true);
        _mockDirtyGuard.Setup(x => x.ConfirmDiscard()).Returns(true);
        SetupPickerAndLoader();

        //Act
        _sut.OpenPersonCommand.Execute(null);

        //Assert — guard called exactly once
        _mockDirtyGuard.Verify(x => x.ConfirmDiscard(), Times.Once());
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void OpenPerson_DoesNotLoad_WhenDirtyAndUserCancels()
    {
        //Arrange — first load to have a snapshot
        SimulateLoadedPerson();
        var loaderCallCount = 0;

        _mockDirtyTracker.Setup(x => x.IsDirty(It.IsAny<MeFile>(), It.IsAny<MeFile>())).Returns(true);
        _mockDirtyGuard.Setup(x => x.ConfirmDiscard()).Returns(false);

        var secondPerson = new PersonSummary(Guid.NewGuid(), "Anna Nowak");
        _mockPickerService
            .Setup(x => x.PickPerson(It.IsAny<System.Collections.Generic.IReadOnlyList<PersonSummary>>()))
            .Returns(secondPerson);
        _mockLoaderService
            .Setup(x => x.Load(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<PersonViewModel>(), It.IsAny<DatesTabViewModel>(), It.IsAny<FamilyTabViewModel>(), It.IsAny<NotesTabViewModel>()))
            .Callback(() => loaderCallCount++)
            .Returns(new MeFile());

        //Act
        _sut.OpenPersonCommand.Execute(null);

        //Assert — loader not called again; mode unchanged
        Assert.Equal(0, loaderCallCount);
        Assert.Equal(AppMode.EditTree, _sut.CurrentMode);
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void OpenPerson_Loads_WhenDirtyAndUserConfirmsDiscard()
    {
        //Arrange
        SimulateLoadedPerson();
        _mockDirtyTracker.Setup(x => x.IsDirty(It.IsAny<MeFile>(), It.IsAny<MeFile>())).Returns(true);
        _mockDirtyGuard.Setup(x => x.ConfirmDiscard()).Returns(true);

        var secondPerson = new PersonSummary(Guid.NewGuid(), "Anna Nowak");
        var secondMeFile = new MeFile { UniqueIdentifier = secondPerson.UniqueIdentifier };
        _mockPickerService
            .Setup(x => x.PickPerson(It.IsAny<System.Collections.Generic.IReadOnlyList<PersonSummary>>()))
            .Returns(secondPerson);
        _mockLoaderService
            .Setup(x => x.Load(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<PersonViewModel>(), It.IsAny<DatesTabViewModel>(), It.IsAny<FamilyTabViewModel>(), It.IsAny<NotesTabViewModel>()))
            .Returns(secondMeFile);

        //Act
        _sut.OpenPersonCommand.Execute(null);

        //Assert — loader called twice total (first load + second load)
        _mockLoaderService.Verify(
            x => x.Load(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<PersonViewModel>(), It.IsAny<DatesTabViewModel>(), It.IsAny<FamilyTabViewModel>(), It.IsAny<NotesTabViewModel>()),
            Times.Exactly(2));
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void SwitchMode_ShowsGuard_WhenDirtyOnAddPath()
    {
        //Arrange — no SimulateLoadedPerson; snapshot stays null
        _mockDirtyTracker.Setup(x => x.IsDirty(null, It.IsAny<MeFile>())).Returns(true);
        _mockDirtyGuard.Setup(x => x.ConfirmDiscard()).Returns(false);

        //Act
        _sut.SwitchModeCommand.Execute(AppMode.EditTree);

        //Assert — mode did not change; guard was invoked
        Assert.Equal(AppMode.Add, _sut.CurrentMode);
        _mockDirtyGuard.Verify(x => x.ConfirmDiscard(), Times.Once());
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void OpenPerson_ShowsGuard_WhenDirtyOnAddPath()
    {
        //Arrange — no prior load; snapshot stays null
        _mockDirtyTracker.Setup(x => x.IsDirty(null, It.IsAny<MeFile>())).Returns(true);
        _mockDirtyGuard.Setup(x => x.ConfirmDiscard()).Returns(false);
        SetupPickerAndLoader();

        //Act
        _sut.OpenPersonCommand.Execute(null);

        //Assert — navigation blocked; guard invoked once
        _mockLoaderService.Verify(
            x => x.Load(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<PersonViewModel>(), It.IsAny<DatesTabViewModel>(), It.IsAny<FamilyTabViewModel>(), It.IsAny<NotesTabViewModel>()),
            Times.Never());
        _mockDirtyGuard.Verify(x => x.ConfirmDiscard(), Times.Once());
    }

    #endregion

    #region FB-007

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void Save_SetsIsBusyFalse_WhenRootPathIsEmpty()
    {
        //Arrange
        _mockRootPointerStore.Setup(x => x.Read()).Returns(string.Empty);

        //Act
        _sut.SaveCommand.Execute(null);

        //Assert
        Assert.False(_sut.IsBusy);
    }

    #endregion

    #region SaveAsDraft

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void SaveAsDraft_WritesDraft_WhenInvoked()
    {
        //Arrange
        _sut.Person.FirstName = "Jan";
        _sut.Person.LastName = "Kowalski";

        //Act
        _sut.SaveAsDraftCommand.Execute(null);

        //Assert
        _mockDraftRepository.Verify(
            x => x.SaveDraft(It.Is<MeFile>(m => m.PersonName == "Jan Kowalski"), FakeRoot),
            Times.Once());
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void SaveAsDraft_AssignsGuid_WhenNew()
    {
        //Arrange
        _sut.Person.FirstName = "Jan";
        _sut.Person.LastName = "Kowalski";
        _sut.Person.UniqueIdentifier = Guid.Empty;

        //Act
        _sut.SaveAsDraftCommand.Execute(null);

        //Assert
        _mockDraftRepository.Verify(
            x => x.SaveDraft(It.Is<MeFile>(m => m.UniqueIdentifier != Guid.Empty), FakeRoot),
            Times.Once());
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void SaveAsDraft_DoesNotApplyRelationshipGate_WhenNoRelationships()
    {
        //Arrange — tree has existing people (gate would fire for main-tree Save)
        _mockDirectoryService
            .Setup(x => x.GetAll(FakeRoot))
            .Returns(new System.Collections.Generic.List<PersonSummary> { new PersonSummary(Guid.NewGuid(), "Existing") });

        //Act
        _sut.SaveAsDraftCommand.Execute(null);

        //Assert — draft saved with no error; no relationship gate
        _mockDraftRepository.Verify(x => x.SaveDraft(It.IsAny<MeFile>(), FakeRoot), Times.Once());
        Assert.True(string.IsNullOrEmpty(_sut.ErrorMessage));
    }

    #endregion

    #region LoadDraft

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void LoadDraft_ShowsGuard_WhenDirty()
    {
        //Arrange
        SimulateLoadedPerson();
        _mockDirtyTracker.Setup(x => x.IsDirty(It.IsAny<MeFile>(), It.IsAny<MeFile>())).Returns(true);
        _mockDirtyGuard.Setup(x => x.ConfirmDiscard()).Returns(false);

        //Act
        _sut.LoadDraftCommand.Execute(null);

        //Assert — guard called
        _mockDirtyGuard.Verify(x => x.ConfirmDiscard(), Times.Once());
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void LoadDraft_DoesNotLoad_WhenDirtyAndUserCancels()
    {
        //Arrange
        SimulateLoadedPerson();
        _mockDirtyTracker.Setup(x => x.IsDirty(It.IsAny<MeFile>(), It.IsAny<MeFile>())).Returns(true);
        _mockDirtyGuard.Setup(x => x.ConfirmDiscard()).Returns(false);

        //Act
        _sut.LoadDraftCommand.Execute(null);

        //Assert — draft repo never consulted; mode unchanged
        _mockDraftRepository.Verify(x => x.GetAllDrafts(It.IsAny<string>()), Times.Never());
        Assert.Equal(AppMode.EditTree, _sut.CurrentMode);
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void LoadDraft_EntersEditDraftMode_WhenDraftSelected()
    {
        //Arrange
        var draftId = Guid.NewGuid();
        var draftSummary = new PersonSummary(draftId, "Jan Kowalski");
        var draftMeFile = new MeFile { UniqueIdentifier = draftId, PersonName = "Jan Kowalski" };

        SetupDraftPickAndRead(draftSummary, draftMeFile);

        //Act
        _sut.LoadDraftCommand.Execute(null);

        //Assert
        Assert.Equal(AppMode.EditDraft, _sut.CurrentMode);
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void LoadDraft_SetsLoadedPersonId_WhenDraftLoaded()
    {
        //Arrange
        var draftId = Guid.NewGuid();
        var draftSummary = new PersonSummary(draftId, "Jan Kowalski");
        var draftMeFile = new MeFile { UniqueIdentifier = draftId, PersonName = "Jan Kowalski" };

        SetupDraftPickAndRead(draftSummary, draftMeFile);

        //Act
        _sut.LoadDraftCommand.Execute(null);

        //Assert — Family.Reset sets LoadedPersonId from MeFile
        Assert.Equal(draftId, _sut.Family.LoadedPersonId);
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void LoadDraft_DoesNothing_WhenNoDraftSelected()
    {
        //Arrange
        _mockDraftRepository
            .Setup(x => x.GetAllDrafts(FakeRoot))
            .Returns(new System.Collections.Generic.List<PersonSummary>());
        _mockPickerService
            .Setup(x => x.PickPerson(It.IsAny<System.Collections.Generic.IReadOnlyList<PersonSummary>>()))
            .Returns((PersonSummary)null);

        //Act
        _sut.LoadDraftCommand.Execute(null);

        //Assert — mode unchanged; draft not read
        Assert.Equal(AppMode.Add, _sut.CurrentMode);
        _mockDraftRepository.Verify(x => x.ReadDraft(It.IsAny<string>(), It.IsAny<string>()), Times.Never());
    }

    #endregion

    #region PromoteDraft

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void PromoteDraft_CallsPromoter_WhenInEditDraftMode()
    {
        //Arrange
        SimulateLoadedDraft();

        //Act
        _sut.PromoteDraftCommand.Execute(null);

        //Assert
        _mockDraftPromoter.Verify(x => x.Promote(It.IsAny<MeFile>(), FakeRoot), Times.Once());
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void PromoteDraft_SwitchesToEditTree_WhenPromoteSucceeds()
    {
        //Arrange
        SimulateLoadedDraft();

        //Act
        _sut.PromoteDraftCommand.Execute(null);

        //Assert
        Assert.Equal(AppMode.EditTree, _sut.CurrentMode);
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void PromoteDraft_ShowsError_WhenPromoteThrows()
    {
        //Arrange
        SimulateLoadedDraft();
        _mockDraftPromoter
            .Setup(x => x.Promote(It.IsAny<MeFile>(), It.IsAny<string>()))
            .Throws<InvalidOperationException>();

        //Act
        _sut.PromoteDraftCommand.Execute(null);

        //Assert
        Assert.False(string.IsNullOrEmpty(_sut.ErrorMessage));
        Assert.Equal(AppMode.EditDraft, _sut.CurrentMode);
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void PromoteDraft_DoesNothing_WhenNotInEditDraftMode()
    {
        //Arrange — mode is Add (default)
        Assert.Equal(AppMode.Add, _sut.CurrentMode);

        //Act
        _sut.PromoteDraftCommand.Execute(null);

        //Assert
        _mockDraftPromoter.Verify(x => x.Promote(It.IsAny<MeFile>(), It.IsAny<string>()), Times.Never());
    }

    #endregion

    #region GenerateFolderTree

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void GenerateFolderTree_CallsPickerWithDirectoryPeople_WhenInvoked()
    {
        //Arrange
        var people = new List<PersonSummary> { new PersonSummary(Guid.NewGuid(), "Adam Kowalski") };
        _mockDirectoryService.Setup(s => s.GetAll(FakeRoot)).Returns(people);
        _mockPickerService
            .Setup(s => s.PickPerson(It.IsAny<IReadOnlyList<PersonSummary>>()))
            .Returns(people[0]);

        //Act
        _sut.GenerateFolderTreeCommand.Execute(null);

        //Assert
        _mockDirectoryService.Verify(s => s.GetAll(FakeRoot), Times.Once());
        _mockPickerService.Verify(s => s.PickPerson(people), Times.Once());
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void GenerateFolderTree_DoesNothing_WhenPickerReturnsNull()
    {
        //Arrange
        _mockDirectoryService.Setup(s => s.GetAll(FakeRoot)).Returns(new List<PersonSummary>());
        _mockPickerService
            .Setup(s => s.PickPerson(It.IsAny<IReadOnlyList<PersonSummary>>()))
            .Returns((PersonSummary)null);

        //Act
        _sut.GenerateFolderTreeCommand.Execute(null);

        //Assert
        _mockFolderTreeGenerator.Verify(g => g.Generate(It.IsAny<string>(), It.IsAny<Guid>()), Times.Never());
        _mockFolderTreeSettings.Verify(s => s.SetRootPersonId(It.IsAny<string>(), It.IsAny<Guid>()), Times.Never());
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void GenerateFolderTree_PersistsSelectedRootPerson_WhenPersonChosen()
    {
        //Arrange
        var chosen = new PersonSummary(Guid.NewGuid(), "Adam Kowalski");
        _mockDirectoryService.Setup(s => s.GetAll(FakeRoot)).Returns(new List<PersonSummary> { chosen });
        _mockPickerService
            .Setup(s => s.PickPerson(It.IsAny<IReadOnlyList<PersonSummary>>()))
            .Returns(chosen);

        //Act
        _sut.GenerateFolderTreeCommand.Execute(null);

        //Assert
        _mockFolderTreeSettings.Verify(s => s.SetRootPersonId(FakeRoot, chosen.UniqueIdentifier), Times.Once());
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void GenerateFolderTree_CallsGenerateWithRootPathAndChosenGuid_WhenPersonChosen()
    {
        //Arrange
        var chosen = new PersonSummary(Guid.NewGuid(), "Eva Nowakowska");
        _mockDirectoryService.Setup(s => s.GetAll(FakeRoot)).Returns(new List<PersonSummary> { chosen });
        _mockPickerService
            .Setup(s => s.PickPerson(It.IsAny<IReadOnlyList<PersonSummary>>()))
            .Returns(chosen);

        //Act
        _sut.GenerateFolderTreeCommand.Execute(null);

        //Assert
        _mockFolderTreeGenerator.Verify(g => g.Generate(FakeRoot, chosen.UniqueIdentifier), Times.Once());
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void GenerateFolderTree_SetsSuccessMessage_WhenGenerateSucceeds()
    {
        //Arrange
        var chosen = new PersonSummary(Guid.NewGuid(), "Adam Kowalski");
        _mockDirectoryService.Setup(s => s.GetAll(FakeRoot)).Returns(new List<PersonSummary> { chosen });
        _mockPickerService
            .Setup(s => s.PickPerson(It.IsAny<IReadOnlyList<PersonSummary>>()))
            .Returns(chosen);
        _mockFolderTreeGenerator
            .Setup(g => g.Generate(FakeRoot, chosen.UniqueIdentifier))
            .Returns((7, new List<string>()));

        //Act
        _sut.GenerateFolderTreeCommand.Execute(null);

        //Assert
        Assert.Contains("7", _sut.StatusMessage);
        Assert.True(string.IsNullOrEmpty(_sut.ErrorMessage));
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void GenerateFolderTree_SetsErrorMessage_WhenGenerateThrows()
    {
        //Arrange
        var chosen = new PersonSummary(Guid.NewGuid(), "Adam Kowalski");
        _mockDirectoryService.Setup(s => s.GetAll(FakeRoot)).Returns(new List<PersonSummary> { chosen });
        _mockPickerService
            .Setup(s => s.PickPerson(It.IsAny<IReadOnlyList<PersonSummary>>()))
            .Returns(chosen);
        _mockFolderTreeGenerator
            .Setup(g => g.Generate(It.IsAny<string>(), It.IsAny<Guid>()))
            .Throws<InvalidOperationException>();

        //Act
        _sut.GenerateFolderTreeCommand.Execute(null);

        //Assert
        Assert.False(string.IsNullOrEmpty(_sut.ErrorMessage));
        Assert.Contains("drzewa", _sut.ErrorMessage);
        _mockLog.Verify(x => x.Error(It.IsAny<Exception>(), It.IsAny<string>()), Times.Once());
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void GenerateFolderTree_DoesNothing_WhenRootPathEmpty()
    {
        //Arrange
        _mockRootPointerStore.Setup(x => x.Read()).Returns(string.Empty);

        //Act
        _sut.GenerateFolderTreeCommand.Execute(null);

        //Assert
        _mockFolderTreeGenerator.Verify(g => g.Generate(It.IsAny<string>(), It.IsAny<Guid>()), Times.Never());
        _mockPickerService.Verify(s => s.PickPerson(It.IsAny<IReadOnlyList<PersonSummary>>()), Times.Never());
    }

    #endregion

    #region ValidateTree

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void ValidateTree_CallsValidatorWithBuiltMap_WhenInvoked()
    {
        //Arrange
        var personId = Guid.NewGuid();
        var meFile = PersonFixtureFactory.Build(personId, "Jan", "Kowalski", Sex.Male);
        var fakePath = @"C:\fake\root\Lista osób\Jan Kowalski\me.json";

        _mockProcessor.Setup(p => p.ScanMeFiles(FakeRoot)).Returns(new List<string> { fakePath });
        _mockProcessor.Setup(p => p.ReadMeFile(fakePath)).Returns(meFile);

        //Act
        _sut.ValidateTreeCommand.Execute(null);

        //Assert
        _mockValidator.Verify(
            v => v.Validate(It.Is<IReadOnlyDictionary<Guid, MeFile>>(m => m.ContainsKey(personId))),
            Times.Once());
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void ValidateTree_FormatsValidatorOutput_WhenIssuesFound()
    {
        //Arrange
        var idA = Guid.NewGuid();
        var issues = new List<ValidationIssue>
        {
            new() { Kind = ValidationIssueKind.Orphan, Subjects = [idA] },
            new() { Kind = ValidationIssueKind.Orphan, Subjects = [Guid.NewGuid()] },
        };
        _mockValidator
            .Setup(v => v.Validate(It.IsAny<IReadOnlyDictionary<Guid, MeFile>>()))
            .Returns(issues);

        //Act
        _sut.ValidateTreeCommand.Execute(null);

        //Assert
        _mockFormatter.Verify(
            f => f.Format(
                It.Is<IReadOnlyList<ValidationIssue>>(list => list.Count == 2),
                It.IsAny<IReadOnlyDictionary<Guid, MeFile>>()),
            Times.Once());
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void ValidateTree_ShowsReport_WhenInvoked()
    {
        //Arrange
        var expectedMessages = new List<string> { "Cykl: Jan Kowalski → Anna Nowak" };
        _mockFormatter
            .Setup(f => f.Format(It.IsAny<IReadOnlyList<ValidationIssue>>(), It.IsAny<IReadOnlyDictionary<Guid, MeFile>>()))
            .Returns(expectedMessages);

        //Act
        _sut.ValidateTreeCommand.Execute(null);

        //Assert
        _mockReportService.Verify(s => s.Show(expectedMessages), Times.Once());
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void ValidateTree_ShowsReportWithNoIssues_WhenTreeClean()
    {
        //Arrange — validator returns empty, formatter returns empty
        _mockValidator
            .Setup(v => v.Validate(It.IsAny<IReadOnlyDictionary<Guid, MeFile>>()))
            .Returns(new List<ValidationIssue>());
        _mockFormatter
            .Setup(f => f.Format(It.IsAny<IReadOnlyList<ValidationIssue>>(), It.IsAny<IReadOnlyDictionary<Guid, MeFile>>()))
            .Returns(new List<string>());

        //Act
        _sut.ValidateTreeCommand.Execute(null);

        //Assert — report shown even when empty (user gets "no problems" confirmation)
        _mockReportService.Verify(s => s.Show(It.IsAny<IReadOnlyList<string>>()), Times.Once());
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void ValidateTree_DoesNothing_WhenRootPathEmpty()
    {
        //Arrange
        _mockRootPointerStore.Setup(x => x.Read()).Returns(string.Empty);

        //Act
        _sut.ValidateTreeCommand.Execute(null);

        //Assert
        _mockValidator.Verify(v => v.Validate(It.IsAny<IReadOnlyDictionary<Guid, MeFile>>()), Times.Never());
        _mockReportService.Verify(s => s.Show(It.IsAny<IReadOnlyList<string>>()), Times.Never());
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void ValidateTree_SetsError_WhenScanThrows()
    {
        //Arrange
        _mockProcessor
            .Setup(p => p.ScanMeFiles(It.IsAny<string>()))
            .Throws<InvalidOperationException>();

        //Act
        _sut.ValidateTreeCommand.Execute(null);

        //Assert
        Assert.False(string.IsNullOrEmpty(_sut.ErrorMessage));
        _mockReportService.Verify(s => s.Show(It.IsAny<IReadOnlyList<string>>()), Times.Never());
        _mockLog.Verify(x => x.Error(It.IsAny<Exception>(), It.IsAny<string>()), Times.Once());
    }

    #endregion

    #region StatusMessage — Save and SaveAsDraft

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void Save_SetsStatusMessage_OnSuccessfulSave()
    {
        //Arrange
        AddOneRelationship();

        //Act
        _sut.SaveCommand.Execute(null);

        //Assert
        Assert.Equal("Zapisano osobę.", _sut.StatusMessage);
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void SaveAsDraft_SetsStatusMessage_WhenDraftSaved()
    {
        //Arrange
        _sut.Person.FirstName = "Jan";

        //Act
        _sut.SaveAsDraftCommand.Execute(null);

        //Assert
        Assert.Equal("Zapisano szkic.", _sut.StatusMessage);
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void Save_ClearsStatusMessage_AtStartOfCommand()
    {
        //Arrange — set a stale status first
        _sut.SaveAsDraftCommand.Execute(null);
        _mockPersonRepository
            .Setup(x => x.Create(It.IsAny<MeFile>(), It.IsAny<string>()))
            .Throws<IOException>();
        AddOneRelationship();

        //Act
        _sut.SaveCommand.Execute(null);

        //Assert — error path: StatusMessage was cleared at start
        Assert.True(string.IsNullOrEmpty(_sut.StatusMessage));
    }

    #endregion

    #region SwitchMode — Add reset and picker load

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void SwitchMode_ClearsPersonFields_WhenSwitchingToAdd()
    {
        //Arrange — load a person so fields have data
        SimulateLoadedPerson();
        _sut.Person.FirstName = "SomeValue";

        //Act
        _sut.SwitchModeCommand.Execute(AppMode.Add);

        //Assert
        Assert.Equal(string.Empty, _sut.Person.FirstName);
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void SwitchMode_LoadsTreePeopleIntoPickers_WhenSwitchingToAdd()
    {
        //Arrange — put sut in EditTree mode first so switching to Add is not a no-op
        _sut.SwitchModeCommand.Execute(AppMode.EditTree);
        var person1 = new PersonSummary(Guid.NewGuid(), "Adam Kowalski");
        var person2 = new PersonSummary(Guid.NewGuid(), "Ewa Nowak");
        _mockDirectoryService
            .Setup(x => x.GetAll(FakeRoot))
            .Returns(new List<PersonSummary> { person1, person2 });

        //Act
        _sut.SwitchModeCommand.Execute(AppMode.Add);

        //Assert — pickers loaded with 2 people; no selections, no exclusions → 2 candidates
        Assert.Equal(2, _sut.Family.Parents.Candidates.Count);
    }

    #endregion

    #region WindowTitle

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void WindowTitle_ReflectsMode_WhenModeChanges()
    {
        //Arrange — start in Add
        Assert.Contains("Nowa osoba", _sut.WindowTitle);

        //Act
        _sut.SwitchModeCommand.Execute(AppMode.EditTree);

        //Assert
        Assert.Contains("Edycja osoby", _sut.WindowTitle);
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void WindowTitle_ContainsEditDraftLabel_WhenInEditDraftMode()
    {
        //Arrange
        SimulateLoadedDraft();

        //Assert
        Assert.Contains("Edycja szkicu", _sut.WindowTitle);
    }

    #endregion

    #region PromoteDraft — CanExecute

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void PromoteDraft_CanExecuteFalse_WhenModeIsAdd()
    {
        //Arrange — default mode is Add
        Assert.Equal(AppMode.Add, _sut.CurrentMode);

        //Assert
        Assert.False(_sut.PromoteDraftCommand.CanExecute(null));
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void PromoteDraft_CanExecuteTrue_WhenModeIsEditDraft()
    {
        //Arrange
        SimulateLoadedDraft();

        //Assert
        Assert.True(_sut.PromoteDraftCommand.CanExecute(null));
    }

    #endregion

    #region PromoteDraft — confirm gate

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void PromoteDraft_DoesNotPromote_WhenConfirmDeclined()
    {
        //Arrange
        SimulateLoadedDraft();
        _mockPromoteConfirm.Setup(s => s.Confirm(It.IsAny<string>())).Returns(false);

        //Act
        _sut.PromoteDraftCommand.Execute(null);

        //Assert
        _mockDraftPromoter.Verify(x => x.Promote(It.IsAny<MeFile>(), It.IsAny<string>()), Times.Never());
        Assert.Equal(AppMode.EditDraft, _sut.CurrentMode);
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void PromoteDraft_Promotes_WhenConfirmAccepted()
    {
        //Arrange
        SimulateLoadedDraft();
        _mockPromoteConfirm.Setup(s => s.Confirm(It.IsAny<string>())).Returns(true);

        //Act
        _sut.PromoteDraftCommand.Execute(null);

        //Assert
        _mockDraftPromoter.Verify(x => x.Promote(It.IsAny<MeFile>(), It.IsAny<string>()), Times.Once());
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void PromoteDraft_PassesSummaryContainingPersonName_WhenConfirming()
    {
        //Arrange
        var draftId = Guid.NewGuid();
        var draftSummary = new PersonSummary(draftId, "Maria Wiśniewska");
        var draftMeFile = new MeFile { UniqueIdentifier = draftId, PersonName = "Maria Wiśniewska", FirstName = "Maria", LastName = "Wiśniewska" };
        SetupDraftPickAndRead(draftSummary, draftMeFile);
        _sut.LoadDraftCommand.Execute(null);

        string capturedSummary = null;
        _mockPromoteConfirm
            .Setup(s => s.Confirm(It.IsAny<string>()))
            .Callback<string>(s => capturedSummary = s)
            .Returns(true);

        //Act
        _sut.PromoteDraftCommand.Execute(null);

        //Assert
        Assert.NotNull(capturedSummary);
        Assert.Contains("Maria", capturedSummary);
    }

    #endregion

    #region OpenDataFolder

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void OpenDataFolder_RevealsRootPath_WhenRootSet()
    {
        //Arrange — root is set to FakeRoot by default

        //Act
        _sut.OpenDataFolderCommand.Execute(null);

        //Assert
        _mockFolderReveal.Verify(s => s.Reveal(FakeRoot), Times.Once());
        Assert.True(string.IsNullOrEmpty(_sut.ErrorMessage));
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void OpenDataFolder_SetsError_WhenRootPathEmpty()
    {
        //Arrange
        _mockRootPointerStore.Setup(x => x.Read()).Returns(string.Empty);

        //Act
        _sut.OpenDataFolderCommand.Execute(null);

        //Assert
        Assert.False(string.IsNullOrEmpty(_sut.ErrorMessage));
        _mockFolderReveal.Verify(s => s.Reveal(It.IsAny<string>()), Times.Never());
    }

    #endregion

    #region GenerateLineageFolders

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void GenerateLineageFolders_GeneratesAndReportsCount_WhenPersonPicked()
    {
        //Arrange
        var chosen = new PersonSummary(Guid.NewGuid(), "Adam Kowalski");
        _mockDirectoryService.Setup(s => s.GetAll(FakeRoot)).Returns(new List<PersonSummary> { chosen });
        _mockPickerService
            .Setup(s => s.PickPerson(It.IsAny<IReadOnlyList<PersonSummary>>()))
            .Returns(chosen);
        _mockLineageFolderGenerator
            .Setup(g => g.Generate(FakeRoot, chosen.UniqueIdentifier))
            .Returns((9, new List<string>()));

        //Act
        _sut.GenerateLineageFoldersCommand.Execute(null);

        //Assert
        _mockFolderTreeSettings.Verify(s => s.SetRootPersonId(FakeRoot, chosen.UniqueIdentifier), Times.Once());
        _mockLineageFolderGenerator.Verify(g => g.Generate(FakeRoot, chosen.UniqueIdentifier), Times.Once());
        Assert.Contains("9", _sut.StatusMessage);
        Assert.True(string.IsNullOrEmpty(_sut.ErrorMessage));
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void GenerateLineageFolders_DoesNothing_WhenPickerCancels()
    {
        //Arrange
        _mockDirectoryService.Setup(s => s.GetAll(FakeRoot)).Returns(new List<PersonSummary>());
        _mockPickerService
            .Setup(s => s.PickPerson(It.IsAny<IReadOnlyList<PersonSummary>>()))
            .Returns((PersonSummary)null);

        //Act
        _sut.GenerateLineageFoldersCommand.Execute(null);

        //Assert
        _mockLineageFolderGenerator.Verify(g => g.Generate(It.IsAny<string>(), It.IsAny<Guid>()), Times.Never());
        _mockFolderTreeSettings.Verify(s => s.SetRootPersonId(It.IsAny<string>(), It.IsAny<Guid>()), Times.Never());
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void GenerateLineageFolders_SetsError_WhenGeneratorThrows()
    {
        //Arrange
        var chosen = new PersonSummary(Guid.NewGuid(), "Adam Kowalski");
        _mockDirectoryService.Setup(s => s.GetAll(FakeRoot)).Returns(new List<PersonSummary> { chosen });
        _mockPickerService
            .Setup(s => s.PickPerson(It.IsAny<IReadOnlyList<PersonSummary>>()))
            .Returns(chosen);
        _mockLineageFolderGenerator
            .Setup(g => g.Generate(It.IsAny<string>(), It.IsAny<Guid>()))
            .Throws<InvalidOperationException>();

        //Act
        _sut.GenerateLineageFoldersCommand.Execute(null);

        //Assert
        Assert.False(string.IsNullOrEmpty(_sut.ErrorMessage));
        Assert.Contains("rodów", _sut.ErrorMessage);
        Assert.True(string.IsNullOrEmpty(_sut.StatusMessage));
        _mockLog.Verify(x => x.Error(It.IsAny<Exception>(), It.IsAny<string>()), Times.Once());
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void GenerateLineageFolders_DoesNothing_WhenRootPathEmpty()
    {
        //Arrange
        _mockRootPointerStore.Setup(x => x.Read()).Returns(string.Empty);

        //Act
        _sut.GenerateLineageFoldersCommand.Execute(null);

        //Assert
        _mockLineageFolderGenerator.Verify(g => g.Generate(It.IsAny<string>(), It.IsAny<Guid>()), Times.Never());
        _mockPickerService.Verify(s => s.PickPerson(It.IsAny<IReadOnlyList<PersonSummary>>()), Times.Never());
    }

    #endregion

    private void AddOneRelationship()
    {
        _sut.Family.Parents.Selected.Add(new PersonSummary(Guid.NewGuid(), "Testowy Rodzic"));
    }

    private void SimulateLoadedPerson()
    {
        var selectedPerson = new PersonSummary(Guid.NewGuid(), "Jan Kowalski");
        var loadedMeFile = new MeFile { UniqueIdentifier = selectedPerson.UniqueIdentifier, PersonName = "Jan Kowalski" };
        _mockPickerService
            .Setup(x => x.PickPerson(It.IsAny<System.Collections.Generic.IReadOnlyList<PersonSummary>>()))
            .Returns(selectedPerson);
        _mockLoaderService
            .Setup(x => x.Load(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<PersonViewModel>(), It.IsAny<DatesTabViewModel>(), It.IsAny<FamilyTabViewModel>(), It.IsAny<NotesTabViewModel>()))
            .Returns(loadedMeFile);
        _mockDirectoryService
            .Setup(x => x.GetAll(FakeRoot))
            .Returns(new System.Collections.Generic.List<PersonSummary>());
        _sut.OpenPersonCommand.Execute(null);
    }

    private void SimulateLoadedDraft()
    {
        var draftId = Guid.NewGuid();
        var draftSummary = new PersonSummary(draftId, "Jan Kowalski");
        var draftMeFile = new MeFile { UniqueIdentifier = draftId, PersonName = "Jan Kowalski" };
        SetupDraftPickAndRead(draftSummary, draftMeFile);
        _sut.LoadDraftCommand.Execute(null);
    }

    private void SetupDraftPickAndRead(PersonSummary draftSummary, MeFile draftMeFile)
    {
        _mockDraftRepository
            .Setup(x => x.GetAllDrafts(FakeRoot))
            .Returns(new System.Collections.Generic.List<PersonSummary> { draftSummary });
        _mockPickerService
            .Setup(x => x.PickPerson(It.IsAny<System.Collections.Generic.IReadOnlyList<PersonSummary>>()))
            .Returns(draftSummary);
        _mockDraftRepository
            .Setup(x => x.ReadDraft(FakeRoot, draftSummary.DisplayName))
            .Returns(draftMeFile);
        _mockDirectoryService
            .Setup(x => x.GetAll(FakeRoot))
            .Returns(new System.Collections.Generic.List<PersonSummary>());
    }

    private void SetupPickerAndLoader()
    {
        var selectedPerson = new PersonSummary(Guid.NewGuid(), "Anna Nowak");
        var loadedMeFile = new MeFile { UniqueIdentifier = selectedPerson.UniqueIdentifier };
        _mockPickerService
            .Setup(x => x.PickPerson(It.IsAny<System.Collections.Generic.IReadOnlyList<PersonSummary>>()))
            .Returns(selectedPerson);
        _mockLoaderService
            .Setup(x => x.Load(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<PersonViewModel>(), It.IsAny<DatesTabViewModel>(), It.IsAny<FamilyTabViewModel>(), It.IsAny<NotesTabViewModel>()))
            .Returns(loadedMeFile);
        _mockDirectoryService
            .Setup(x => x.GetAll(FakeRoot))
            .Returns(new System.Collections.Generic.List<PersonSummary>());
    }
}
