using System;
using System.Collections.Generic;
using Moq;
using Serilog;
using TreeManager.Common.TestUtilities;
using TreeManager.Core.Abstractions.IO;
using TreeManager.Core.Abstractions.Persistence;
using TreeManager.Core.Domain;
using TreeManager.Core.Services;

namespace TreeManager.Core.L0.Services;

public class DraftPromoterTests
{
    private const string RootPath = @"C:\fake\root";
    private const string PersonName = "Jan Kowalski";
    private const string ListaOsobPersonFolder = @"C:\fake\root\Lista osób\Jan Kowalski";

    private static readonly Guid DraftId = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001");

    private readonly Mock<IPersonRepository> _mockPersonRepo;
    private readonly Mock<IDraftRepository> _mockDraftRepo;
    private readonly Mock<IFileSystemFacade> _mockFs;
    private readonly Mock<ILogger> _mockLog;
    private readonly DraftPromoter _sut;

    public DraftPromoterTests()
    {
        _mockPersonRepo = new Mock<IPersonRepository>();
        _mockDraftRepo = new Mock<IDraftRepository>();
        _mockFs = new Mock<IFileSystemFacade>();
        _mockLog = new Mock<ILogger>();

        // Default: no collision — destination folder does not exist
        _mockFs.Setup(x => x.DirectoryExists(It.IsAny<string>())).Returns(false);

        _sut = new DraftPromoter(_mockPersonRepo.Object, _mockDraftRepo.Object, _mockFs.Object, _mockLog.Object);
    }

    #region Promote

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void Promote_CreatesPersonInMainTree_WhenCalled()
    {
        //Arrange
        var draft = BuildDraft();

        //Act
        _sut.Promote(draft, RootPath);

        //Assert
        _mockPersonRepo.Verify(
            x => x.Create(draft, RootPath, It.IsAny<string>()),
            Times.Once());
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void Promote_DeletesDraftFolder_AfterSuccessfulCreate()
    {
        //Arrange
        var draft = BuildDraft();
        var createCallOrder = 0;
        var deleteCallOrder = 0;
        var callCounter = 0;

        _mockPersonRepo
            .Setup(x => x.Create(It.IsAny<MeFile>(), It.IsAny<string>(), It.IsAny<string>()))
            .Callback(() => createCallOrder = ++callCounter);
        _mockDraftRepo
            .Setup(x => x.DeleteDraft(It.IsAny<string>(), It.IsAny<string>()))
            .Callback(() => deleteCallOrder = ++callCounter);

        //Act
        _sut.Promote(draft, RootPath);

        //Assert — create happens before delete
        Assert.True(createCallOrder > 0, "Create was never called");
        Assert.True(deleteCallOrder > 0, "DeleteDraft was never called");
        Assert.True(createCallOrder < deleteCallOrder, "Create must precede DeleteDraft");
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void Promote_DoesNotDeleteDraft_WhenCreateThrows()
    {
        //Arrange
        var draft = BuildDraft();
        _mockPersonRepo
            .Setup(x => x.Create(It.IsAny<MeFile>(), It.IsAny<string>(), It.IsAny<string>()))
            .Throws<InvalidOperationException>();

        //Act
        try { _sut.Promote(draft, RootPath); } catch { }

        //Assert — draft delete never called
        _mockDraftRepo.Verify(x => x.DeleteDraft(It.IsAny<string>(), It.IsAny<string>()), Times.Never());
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void Promote_RemovesPartialDestination_WhenCreateThrows()
    {
        //Arrange
        var draft = BuildDraft();

        // Sequence: first DirectoryExists call (during collision check) returns false so no suffix is applied.
        // The Create then throws to simulate a partial write.
        // After Create throws, the catch block calls DirectoryExists(destinationFolder) again — return true
        // to simulate the partial folder having been created.
        var callCount = 0;
        _mockFs
            .Setup(x => x.DirectoryExists(ListaOsobPersonFolder))
            .Returns(() =>
            {
                callCount++;
                return callCount > 1; // first call (collision check) → false; second call (cleanup check) → true
            });

        _mockPersonRepo
            .Setup(x => x.Create(It.IsAny<MeFile>(), It.IsAny<string>(), It.IsAny<string>()))
            .Throws<InvalidOperationException>();

        //Act
        try { _sut.Promote(draft, RootPath); } catch { }

        //Assert — partial destination cleaned up
        _mockFs.Verify(
            x => x.DeleteDirectory(
                It.Is<string>(p => p.Contains("Lista osób") && p.Contains(PersonName)),
                It.IsAny<bool>()),
            Times.Once());
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void Promote_Rethrows_WhenCreateThrows()
    {
        //Arrange
        var draft = BuildDraft();
        _mockPersonRepo
            .Setup(x => x.Create(It.IsAny<MeFile>(), It.IsAny<string>(), It.IsAny<string>()))
            .Throws<InvalidOperationException>();

        //Act + Assert
        Assert.Throws<InvalidOperationException>(() => _sut.Promote(draft, RootPath));
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void Promote_AppliesFolderSuffix_WhenDestinationAlreadyExists()
    {
        //Arrange
        var draft = BuildDraft();

        // Simulate that "Jan Kowalski" folder already exists but "Jan Kowalski (2)" does not
        _mockFs.Setup(x => x.DirectoryExists(ListaOsobPersonFolder)).Returns(true);
        _mockFs.Setup(x => x.DirectoryExists(@"C:\fake\root\Lista osób\Jan Kowalski (2)")).Returns(false);

        //Act
        _sut.Promote(draft, RootPath);

        //Assert — Create called with "(2)" folder name; PersonName in MeFile unchanged
        _mockPersonRepo.Verify(
            x => x.Create(
                It.Is<MeFile>(m => m.PersonName == PersonName),
                RootPath,
                "Jan Kowalski (2)"),
            Times.Once());
    }

    #endregion

    #region Helpers

    private static MeFile BuildDraft()
    {
        return new MeFile
        {
            UniqueIdentifier = DraftId,
            PersonName = PersonName,
        };
    }

    #endregion
}
