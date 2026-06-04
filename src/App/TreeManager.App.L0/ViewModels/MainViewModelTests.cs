using System;
using System.IO;
using Moq;
using Serilog;
using TreeManager.App.Services;
using TreeManager.App.ViewModels;
using TreeManager.Common.TestUtilities;
using TreeManager.Core.Abstractions.Persistence;
using TreeManager.Core.Abstractions.Settings;
using TreeManager.Core.Domain;

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
        _mockLog = new Mock<ILogger>();

        _mockRootPointerStore.Setup(x => x.Read()).Returns(FakeRoot);

        // Default: not dirty — existing tests proceed unchanged
        _mockDirtyTracker.Setup(x => x.IsDirty(It.IsAny<MeFile>(), It.IsAny<MeFile>())).Returns(false);

        var deps = new PersonEditDependencies(
            _mockDirectoryService.Object,
            _mockPickerService.Object,
            _mockLoaderService.Object,
            _mockDirtyTracker.Object,
            _mockDirtyGuard.Object);

        _sut = new MainViewModel(
            new PersonViewModel(),
            new DatesTabViewModel(),
            new FamilyTabViewModel(),
            new NotesTabViewModel(),
            _mockPersonRepository.Object,
            _mockRootPointerStore.Object,
            deps,
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
