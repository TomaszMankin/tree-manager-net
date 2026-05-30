using System;
using System.IO;
using Moq;
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
    private readonly MainViewModel _sut;

    public MainViewModelTests()
    {
        _mockPersonRepository = new Mock<IPersonRepository>();
        _mockRootPointerStore = new Mock<IRootPointerStore>();
        _mockDirectoryService = new Mock<IPersonDirectoryService>();
        _mockPickerService = new Mock<IPersonPickerService>();
        _mockLoaderService = new Mock<IPersonLoaderService>();

        _mockRootPointerStore.Setup(x => x.Read()).Returns(FakeRoot);

        var deps = new PersonEditDependencies(
            _mockDirectoryService.Object,
            _mockPickerService.Object,
            _mockLoaderService.Object);

        _sut = new MainViewModel(
            new PersonViewModel(),
            new DatesTabViewModel(),
            new FamilyTabViewModel(),
            _mockPersonRepository.Object,
            _mockRootPointerStore.Object,
            deps);
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
            .Setup(x => x.Load(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<PersonViewModel>(), It.IsAny<DatesTabViewModel>(), It.IsAny<FamilyTabViewModel>()))
            .Returns(loadedMeFile);
        _mockDirectoryService
            .Setup(x => x.GetAll(FakeRoot))
            .Returns(new System.Collections.Generic.List<PersonSummary>());
        _sut.OpenPersonCommand.Execute(null);

        //Act — switch back to Add mode
        _sut.SwitchModeCommand.Execute(AppMode.Add);
        _sut.SaveCommand.Execute(null);

        //Assert — should call Create (snapshot cleared), not Update
        _mockPersonRepository.Verify(x => x.Create(It.IsAny<MeFile>(), FakeRoot), Times.Once());
        _mockPersonRepository.Verify(x => x.Update(It.IsAny<MeFile>(), It.IsAny<MeFile>(), It.IsAny<string>()), Times.Never());
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

        //Act + Assert — must not throw; IsBusy must be reset
        _sut.SaveCommand.Execute(null);
        Assert.False(_sut.IsBusy);
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void Save_CallsRepositoryUpdate_WhenOriginalSnapshotIsSet()
    {
        //Arrange — load a person to set the snapshot
        var selectedPerson = new PersonSummary(Guid.NewGuid(), "Jan Kowalski");
        var loadedMeFile = new MeFile { UniqueIdentifier = selectedPerson.UniqueIdentifier, PersonName = "Jan Kowalski" };
        _mockPickerService
            .Setup(x => x.PickPerson(It.IsAny<System.Collections.Generic.IReadOnlyList<PersonSummary>>()))
            .Returns(selectedPerson);
        _mockLoaderService
            .Setup(x => x.Load(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<PersonViewModel>(), It.IsAny<DatesTabViewModel>(), It.IsAny<FamilyTabViewModel>()))
            .Returns(loadedMeFile);
        _mockDirectoryService
            .Setup(x => x.GetAll(FakeRoot))
            .Returns(new System.Collections.Generic.List<PersonSummary>());
        _sut.OpenPersonCommand.Execute(null);

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

        //Act
        _sut.SaveCommand.Execute(null);

        //Assert
        Assert.False(string.IsNullOrEmpty(_sut.ErrorMessage));
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void Save_ClearsErrorMessage_OnSuccessfulSave()
    {
        //Arrange — first make a failing save to set ErrorMessage
        _mockPersonRepository
            .Setup(x => x.Create(It.IsAny<MeFile>(), It.IsAny<string>()))
            .Throws<IOException>();
        _sut.SaveCommand.Execute(null);
        Assert.False(string.IsNullOrEmpty(_sut.ErrorMessage));

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
            .Setup(x => x.Load(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<PersonViewModel>(), It.IsAny<DatesTabViewModel>(), It.IsAny<FamilyTabViewModel>()))
            .Returns(loadedMeFile);
        _mockDirectoryService
            .Setup(x => x.GetAll(FakeRoot))
            .Returns(new System.Collections.Generic.List<PersonSummary>());
        _sut.OpenPersonCommand.Execute(null);

        //Act — save once
        _sut.SaveCommand.Execute(null);
        // save second time — snapshot must be the MeFile from first save, not the original load
        _sut.SaveCommand.Execute(null);

        //Assert — Update called twice (not with stale original each time)
        _mockPersonRepository.Verify(x => x.Update(It.IsAny<MeFile>(), It.IsAny<MeFile>(), FakeRoot), Times.Exactly(2));
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
            .Setup(x => x.Load(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<PersonViewModel>(), It.IsAny<DatesTabViewModel>(), It.IsAny<FamilyTabViewModel>()))
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
            x => x.Load(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<PersonViewModel>(), It.IsAny<DatesTabViewModel>(), It.IsAny<FamilyTabViewModel>()),
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
            x => x.Load(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<PersonViewModel>(), It.IsAny<DatesTabViewModel>(), It.IsAny<FamilyTabViewModel>()),
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
            .Setup(x => x.Load(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<PersonViewModel>(), It.IsAny<DatesTabViewModel>(), It.IsAny<FamilyTabViewModel>()))
            .Throws<InvalidOperationException>();

        //Act
        _sut.OpenPersonCommand.Execute(null);

        //Assert
        Assert.False(string.IsNullOrEmpty(_sut.ErrorMessage));
        Assert.False(_sut.IsBusy);
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
}
