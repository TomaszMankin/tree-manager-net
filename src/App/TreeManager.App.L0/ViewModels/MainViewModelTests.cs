using System;
using Moq;
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
    private readonly MainViewModel _sut;

    public MainViewModelTests()
    {
        _mockPersonRepository = new Mock<IPersonRepository>();
        _mockRootPointerStore = new Mock<IRootPointerStore>();
        _mockRootPointerStore.Setup(x => x.Read()).Returns(FakeRoot);

        _sut = new MainViewModel(
            new PersonViewModel(),
            new DatesTabViewModel(),
            new FamilyTabViewModel(),
            _mockPersonRepository.Object,
            _mockRootPointerStore.Object);
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
    public void Save_SetsIsBusyFalsAfterExecution_WhenCommandCompletes()
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

    #endregion
}
