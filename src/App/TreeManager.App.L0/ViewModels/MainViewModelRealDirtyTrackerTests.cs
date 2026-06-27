using System;
using System.Collections.Generic;
using Moq;
using Serilog;
using TreeManager.App.Services;
using TreeManager.App.ViewModels;
using TreeManager.Common.TestUtilities;
using TreeManager.Core.Abstractions.Persistence;
using TreeManager.Core.Abstractions.Services;
using TreeManager.Core.Abstractions.Settings;
using TreeManager.Core.Abstractions.Validation;
using TreeManager.Core.Domain;

namespace TreeManager.App.L0.ViewModels;

public class MainViewModelRealDirtyTrackerTests
{
    private const string FakeRoot = @"C:\fake\root";

    private readonly Mock<IPersonRepository> _mockPersonRepository;
    private readonly Mock<IRootPointerStore> _mockRootPointerStore;
    private readonly Mock<IPersonDirectoryService> _mockDirectoryService;
    private readonly Mock<IPersonPickerService> _mockPickerService;
    private readonly Mock<IPersonLoaderService> _mockLoaderService;
    private readonly Mock<IDirtyGuardService> _mockDirtyGuard;
    private readonly Mock<IDraftRepository> _mockDraftRepository;
    private readonly Mock<IDraftPromoter> _mockDraftPromoter;
    private readonly Mock<IPromoteConfirmService> _mockPromoteConfirm;
    private readonly Mock<IFolderRevealService> _mockFolderReveal;
    private readonly Mock<IInfoDialogService> _mockInfoDialog;
    private readonly Mock<IFolderTreeSettingsStore> _mockFolderTreeSettings;
    private readonly MainViewModel _sut;

    public MainViewModelRealDirtyTrackerTests()
    {
        _mockPersonRepository = new Mock<IPersonRepository>();
        _mockRootPointerStore = new Mock<IRootPointerStore>();
        _mockDirectoryService = new Mock<IPersonDirectoryService>();
        _mockPickerService = new Mock<IPersonPickerService>();
        _mockLoaderService = new Mock<IPersonLoaderService>();
        _mockDirtyGuard = new Mock<IDirtyGuardService>();
        _mockDraftRepository = new Mock<IDraftRepository>();
        _mockDraftPromoter = new Mock<IDraftPromoter>();
        _mockPromoteConfirm = new Mock<IPromoteConfirmService>();
        _mockFolderReveal = new Mock<IFolderRevealService>();
        _mockInfoDialog = new Mock<IInfoDialogService>();
        _mockFolderTreeSettings = new Mock<IFolderTreeSettingsStore>();

        _mockRootPointerStore.Setup(x => x.Read()).Returns(FakeRoot);
        _mockDirectoryService
            .Setup(s => s.GetAll(It.IsAny<string>()))
            .Returns(new List<PersonSummary>());
        _mockDirtyGuard.Setup(g => g.ConfirmDiscard()).Returns(true);
        _mockPromoteConfirm
            .Setup(s => s.Confirm(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(true);

        var deps = new PersonEditDependencies(
            _mockDirectoryService.Object,
            _mockPickerService.Object,
            _mockLoaderService.Object,
            new DirtyTracker(),
            _mockDirtyGuard.Object,
            _mockDraftRepository.Object,
            _mockDraftPromoter.Object,
            _mockPromoteConfirm.Object,
            _mockFolderReveal.Object,
            _mockInfoDialog.Object);

        var folderTreeDeps = new FolderTreeCommandDependencies(
            new Mock<IFolderTreeGenerator>().Object,
            _mockFolderTreeSettings.Object,
            new Mock<ILineageFolderGenerator>().Object);

        var validationDeps = new ValidationCommandDependencies(
            new Mock<ITreeConsistencyValidator>().Object,
            new Mock<IValidationMessageFormatter>().Object,
            new Mock<IValidationReportService>().Object,
            new Mock<IMeFileProcessor>().Object);

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
            new Mock<ILogger>().Object,
            new Mock<IRootPickerService>().Object,
            new Mock<ICrashReporter>().Object,
            new Mock<IUserJournalService>().Object);
    }

    #region N-001 false dirty on blank Add form

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void LoadDraft_DoesNotPromptDiscard_WhenAddFormIsBlank()
    {
        //Arrange
        var draftSummary = new PersonSummary(Guid.NewGuid(), "Maria Wiśniewska");
        SetupDraftPickAndRead(draftSummary, new MeFile { UniqueIdentifier = draftSummary.UniqueIdentifier });

        //Act
        _sut.LoadDraftCommand.Execute(null);

        //Assert
        _mockDirtyGuard.Verify(g => g.ConfirmDiscard(), Times.Never());
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void SwitchMode_DoesNotPromptDiscard_WhenAddFormIsBlank()
    {
        //Arrange
        //Act
        _sut.SwitchModeCommand.Execute(AppMode.EditTree);

        //Assert
        _mockDirtyGuard.Verify(g => g.ConfirmDiscard(), Times.Never());
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void SwitchMode_PromptsDiscard_WhenFieldEditedOnBlankForm()
    {
        //Arrange
        _sut.Person.FirstName = "Jan";

        //Act
        _sut.SwitchModeCommand.Execute(AppMode.EditTree);

        //Assert
        _mockDirtyGuard.Verify(g => g.ConfirmDiscard(), Times.Once());
    }

    #endregion

    #region N-005 false dirty after SaveAsDraft then LoadDraft

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void LoadDraft_DoesNotPromptDiscard_AfterSaveAsDraftWithNoEdits()
    {
        //Arrange
        _sut.Person.FirstName = "Maria";
        _sut.Person.LastName = "Wiśniewska";
        _sut.SaveAsDraftCommand.Execute(null);

        var draftSummary = new PersonSummary(Guid.NewGuid(), "Maria Wiśniewska");
        SetupDraftPickAndRead(draftSummary, new MeFile { UniqueIdentifier = draftSummary.UniqueIdentifier });

        //Act
        _sut.LoadDraftCommand.Execute(null);

        //Assert
        _mockDirtyGuard.Verify(g => g.ConfirmDiscard(), Times.Never());
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void SaveAsDraft_WritesAssignedIdentityBackToPerson_Always()
    {
        //Arrange
        MeFile savedDraft = null;
        _mockDraftRepository
            .Setup(r => r.SaveDraft(It.IsAny<MeFile>(), It.IsAny<string>()))
            .Callback<MeFile, string>((m, _) => savedDraft = m);
        _sut.Person.FirstName = "Maria";
        _sut.Person.LastName = "Wiśniewska";

        //Act
        _sut.SaveAsDraftCommand.Execute(null);

        //Assert
        Assert.NotNull(savedDraft);
        Assert.NotEqual(Guid.Empty, savedDraft.UniqueIdentifier);
        Assert.Equal(savedDraft.UniqueIdentifier, _sut.Person.UniqueIdentifier);
    }

    #endregion

    private void SetupDraftPickAndRead(PersonSummary draftSummary, MeFile draftMeFile)
    {
        _mockDraftRepository
            .Setup(x => x.GetAllDrafts(FakeRoot))
            .Returns(new List<PersonSummary> { draftSummary });
        _mockPickerService
            .Setup(x => x.PickPerson(It.IsAny<IReadOnlyList<PersonSummary>>()))
            .Returns(draftSummary);
        _mockDraftRepository
            .Setup(x => x.ReadDraft(FakeRoot, draftSummary.DisplayName))
            .Returns(draftMeFile);
    }
}
