using System;
using System.Collections.Generic;
using Moq;
using Serilog;
using TreeManager.Common.TestUtilities;
using TreeManager.Core.Abstractions.IO;
using TreeManager.Core.Abstractions.Persistence;
using TreeManager.Core.Domain;
using TreeManager.Infrastructure.Persistence;

namespace TreeManager.Infrastructure.L0.Persistence;

public class DraftRepositoryTests
{
    private const string RootPath = @"C:\fake\root";
    private const string PoczekalniaPath = @"C:\fake\root\Poczekalnia";
    private const string DraftFolderName = "Jan Kowalski";
    private const string DraftFolder = @"C:\fake\root\Poczekalnia\Jan Kowalski";
    private const string DraftMeJson = @"C:\fake\root\Poczekalnia\Jan Kowalski\me.json";

    private static readonly Guid DraftId = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001");

    private readonly Mock<IFileSystemFacade> _fs;
    private readonly Mock<IMeFileProcessor> _processor;
    private readonly Mock<ILogger> _mockLog;
    private readonly DraftRepository _sut;

    public DraftRepositoryTests()
    {
        _fs = new Mock<IFileSystemFacade>();
        _processor = new Mock<IMeFileProcessor>();
        _mockLog = new Mock<ILogger>();
        _sut = new DraftRepository(_fs.Object, _processor.Object, _mockLog.Object);
    }

    #region SaveDraft

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void SaveDraft_WritesMeJsonUnderPoczekalnia_WhenCalled()
    {
        //Arrange
        var draft = new MeFile { UniqueIdentifier = DraftId, PersonName = DraftFolderName };

        //Act
        _sut.SaveDraft(draft, RootPath);

        //Assert
        _processor.Verify(
            x => x.WriteMeFile(
                It.Is<string>(p => p.Contains("Poczekalnia") && p.Contains(DraftFolderName)),
                draft),
            Times.Once());
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void SaveDraft_DoesNotRunRelationshipSync_WhenCalled()
    {
        //Arrange
        var draft = new MeFile { UniqueIdentifier = DraftId, PersonName = DraftFolderName };

        //Act
        _sut.SaveDraft(draft, RootPath);

        //Assert — only the one draft me.json write; no extra writes
        _processor.Verify(
            x => x.WriteMeFile(It.IsAny<string>(), It.IsAny<MeFile>()),
            Times.Once());
    }

    #endregion

    #region GetAllDrafts

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void GetAllDrafts_ReturnsEmpty_WhenPoczekalniaMissing()
    {
        //Arrange
        _fs.Setup(x => x.DirectoryExists(PoczekalniaPath)).Returns(false);

        //Act
        var result = _sut.GetAllDrafts(RootPath);

        //Assert
        Assert.Empty(result);
        _processor.Verify(x => x.ReadMeFile(It.IsAny<string>()), Times.Never());
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void GetAllDrafts_ReturnsOneSummary_WhenOneDraftPresent()
    {
        //Arrange
        _fs.Setup(x => x.DirectoryExists(PoczekalniaPath)).Returns(true);
        _fs.Setup(x => x.EnumerateDirectories(PoczekalniaPath)).Returns(new[] { DraftFolder });
        _fs.Setup(x => x.FileExists(DraftMeJson)).Returns(true);
        _processor.Setup(x => x.ReadMeFile(DraftMeJson))
            .Returns(new MeFile { UniqueIdentifier = DraftId, PersonName = DraftFolderName });

        //Act
        var result = _sut.GetAllDrafts(RootPath);

        //Assert
        Assert.Single(result);
        Assert.Equal(DraftFolderName, result[0].DisplayName);
        Assert.Equal(DraftId, result[0].UniqueIdentifier);
    }

    #endregion

    #region ReadDraft

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void ReadDraft_ReturnsMeFile_WhenDraftExists()
    {
        //Arrange
        var expected = new MeFile { UniqueIdentifier = DraftId, PersonName = DraftFolderName };
        _processor.Setup(x => x.ReadMeFile(DraftMeJson)).Returns(expected);

        //Act
        var result = _sut.ReadDraft(RootPath, DraftFolderName);

        //Assert
        Assert.Equal(expected, result);
        _processor.Verify(x => x.ReadMeFile(DraftMeJson), Times.Once());
    }

    #endregion

    #region DeleteDraft

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void DeleteDraft_DeletesDraftFolder_WhenCalled()
    {
        //Arrange
        // no special setup — just verify the correct call

        //Act
        _sut.DeleteDraft(RootPath, DraftFolderName);

        //Assert
        _fs.Verify(
            x => x.DeleteDirectory(
                It.Is<string>(p => p.Contains("Poczekalnia") && p.Contains(DraftFolderName)),
                true),
            Times.Once());
    }

    #endregion
}
