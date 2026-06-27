using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
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
using TreeManager.Core.Services;
using TreeManager.Infrastructure.IO;
using TreeManager.Infrastructure.Persistence;
using TreeManager.Infrastructure.Shell;

namespace TreeManager.App.L1.ViewModels;

public sealed class MainViewModelBidirSaveIntegrationTests : IDisposable
{
    private const string PersonAName = "Anna Testowa";
    private const string PersonBName = "Bartosz Testowy";
    private const string PeopleListFolder = "Lista osób";

    private readonly string _rootPath;
    private readonly MainViewModel _sut;
    private readonly Mock<IRootPointerStore> _mockRootStore;
    private readonly Mock<IPersonPickerService> _mockPickerService;
    private readonly Mock<IPromoteConfirmService> _mockConfirm;
    private readonly Mock<IInfoDialogService> _mockInfoDialog;

    public MainViewModelBidirSaveIntegrationTests()
    {
        _rootPath = Path.Combine(Path.GetTempPath(), "TM_BidirL1_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(_rootPath, PeopleListFolder));

        var fs = new FileSystemFacade();
        var processor = new MeFileProcessor(fs);
        var shortcutCreator = new ShellLinkShortcutCreator(Log.Logger);
        var folderMirror = new RelationshipFolderMirror(fs, shortcutCreator, Log.Logger);
        var personRepo = new PersonRepository(fs, processor, folderMirror, Log.Logger);
        var directoryService = new PersonDirectoryService(processor);
        var loaderService = new PersonLoaderService(processor, directoryService);

        _mockRootStore = new Mock<IRootPointerStore>();
        _mockRootStore.Setup(s => s.Read()).Returns(_rootPath);

        _mockPickerService = new Mock<IPersonPickerService>();
        _mockConfirm = new Mock<IPromoteConfirmService>();
        _mockConfirm.Setup(s => s.Confirm(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).Returns(true);
        _mockInfoDialog = new Mock<IInfoDialogService>();

        var mockDirtyTracker = new Mock<IDirtyTracker>();
        mockDirtyTracker.Setup(t => t.IsDirty(It.IsAny<MeFile>(), It.IsAny<MeFile>())).Returns(false);

        var mockDraftRepo = new Mock<IDraftRepository>();
        var mockDraftPromoter = new Mock<IDraftPromoter>();
        var mockDirtyGuard = new Mock<IDirtyGuardService>();
        var mockFolderReveal = new Mock<IFolderRevealService>();

        var editDeps = new PersonEditDependencies(
            directoryService,
            _mockPickerService.Object,
            loaderService,
            mockDirtyTracker.Object,
            mockDirtyGuard.Object,
            mockDraftRepo.Object,
            mockDraftPromoter.Object,
            _mockConfirm.Object,
            mockFolderReveal.Object,
            _mockInfoDialog.Object);

        var mockFolderTreeGenerator = new Mock<IFolderTreeGenerator>();
        var mockFolderTreeSettings = new Mock<IFolderTreeSettingsStore>();
        var mockLineageGenerator = new Mock<ILineageFolderGenerator>();
        var folderTreeDeps = new FolderTreeCommandDependencies(
            mockFolderTreeGenerator.Object,
            mockFolderTreeSettings.Object,
            mockLineageGenerator.Object);

        var mockValidator = new Mock<ITreeConsistencyValidator>();
        var mockFormatter = new Mock<IValidationMessageFormatter>();
        var mockReportService = new Mock<IValidationReportService>();
        var mockMeProcessor = new Mock<IMeFileProcessor>();
        var validationDeps = new ValidationCommandDependencies(
            mockValidator.Object,
            mockFormatter.Object,
            mockReportService.Object,
            mockMeProcessor.Object);

        _sut = new MainViewModel(
            new PersonViewModel(),
            new DatesTabViewModel(),
            new FamilyTabViewModel(),
            new NotesTabViewModel(),
            personRepo,
            _mockRootStore.Object,
            editDeps,
            folderTreeDeps,
            validationDeps,
            Log.Logger,
            new Mock<IRootPickerService>().Object,
            new Mock<ICrashReporter>().Object,
            new Mock<IUserJournalService>().Object);
    }

    #region S-001 bidirectional save

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L1)]
    public void Save_WritesBidirectionalLink_WhenPersonBSavedWithPersonAAsParent()
    {
        //Arrange — step 1: save person A (no relationships; first person in empty tree)
        _sut.Person.FirstName = "Anna";
        _sut.Person.LastName = "Testowa";
        _sut.SaveCommand.Execute(null);
        Assert.Equal(AppMode.EditTree, _sut.CurrentMode);

        var personAId = _sut.Family.LoadedPersonId;
        Assert.True(personAId.HasValue);
        Assert.NotEqual(Guid.Empty, personAId.Value);

        // Step 2: switch back to Add mode for person B
        _sut.SwitchModeCommand.Execute(AppMode.Add);
        Assert.Equal(AppMode.Add, _sut.CurrentMode);

        // Step 3: set person B name and pick person A as parent
        _sut.Person.FirstName = "Bartosz";
        _sut.Person.LastName = "Testowy";
        var personASummary = new PersonSummary(personAId.Value, PersonAName);
        _sut.Family.Parents.Selected.Add(personASummary);

        //Act — save person B
        _sut.SaveCommand.Execute(null);

        //Assert — person A's me.json on disk must contain person B's UUID in ChildrenId
        Assert.Equal(AppMode.EditTree, _sut.CurrentMode);
        var personAMeJsonPath = Path.Combine(_rootPath, PeopleListFolder, PersonAName, "me.json");
        Assert.True(File.Exists(personAMeJsonPath));

        var personBId = _sut.Family.LoadedPersonId;
        Assert.True(personBId.HasValue);
        Assert.NotEqual(Guid.Empty, personBId.Value);

        var personAUpdated = JsonSerializer.Deserialize<MeFile>(
            File.ReadAllText(personAMeJsonPath), MeFile.DefaultOptions);
        Assert.Contains(personBId.Value, personAUpdated.ChildrenId);
        Assert.Contains(PersonBName, personAUpdated.Children);
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L1)]
    public void Save_WritesNonEmptyUniqueIdentifier_WhenCreatingNewPerson()
    {
        //Arrange — empty tree; no relationships needed
        _sut.Person.FirstName = "Celina";
        _sut.Person.LastName = "Testowa";

        //Act
        _sut.SaveCommand.Execute(null);

        //Assert
        var meJsonPath = Path.Combine(_rootPath, PeopleListFolder, "Celina Testowa", "me.json");
        Assert.True(File.Exists(meJsonPath));
        var persisted = JsonSerializer.Deserialize<MeFile>(
            File.ReadAllText(meJsonPath), MeFile.DefaultOptions);
        Assert.NotEqual(Guid.Empty, persisted.UniqueIdentifier);
    }

    #endregion

    public void Dispose()
    {
        if (Directory.Exists(_rootPath))
        {
            Directory.Delete(_rootPath, recursive: true);
        }
    }
}
