using System;
using System.Collections.Generic;
using Moq;
using Serilog;
using TreeManager.Common.TestUtilities;
using TreeManager.Core.Abstractions.IO;
using TreeManager.Core.Abstractions.Persistence;
using TreeManager.Core.Domain;
using TreeManager.Core.Domain.Relationships;
using TreeManager.Infrastructure.Persistence;

namespace TreeManager.Infrastructure.L0.Persistence;

public class PersonRepositoryTests
{
    private const string RootPath = @"C:\fake\root";
    private const string ListaOsobPath = @"C:\fake\root\Lista osób";
    private const string PersonFolder = @"C:\fake\root\Lista osób\Jan Kowalski";
    private const string PersonMeJson = @"C:\fake\root\Lista osób\Jan Kowalski\me.json";

    private static readonly Guid PersonId = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001");
    private static readonly Guid RelatedId = Guid.Parse("bbbbbbbb-0000-0000-0000-000000000002");
    private const string RelatedFolder = @"C:\fake\root\Lista osób\Anna Nowak";
    private const string RelatedMeJson = @"C:\fake\root\Lista osób\Anna Nowak\me.json";

    private readonly Mock<IFileSystemFacade> _fs;
    private readonly Mock<IMeFileProcessor> _processor;
    private readonly Mock<ILogger> _mockLogger;
    private readonly PersonRepository _sut;

    public PersonRepositoryTests()
    {
        _fs = new Mock<IFileSystemFacade>();
        _processor = new Mock<IMeFileProcessor>();
        _mockLogger = new Mock<ILogger>();
        _sut = new PersonRepository(_fs.Object, _processor.Object, _mockLogger.Object);
    }

    #region Create

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void Create_CallsCreateDirectory_WhenPersonFolderDoesNotExist()
    {
        //Arrange
        var person = BuildPerson("Jan Kowalski");
        SetupEmptyScan();

        //Act
        _sut.Create(person, RootPath);

        //Assert
        _fs.Verify(x => x.CreateDirectory(PersonFolder), Times.Once());
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void Create_WritesOwnMeFile_WhenCalled()
    {
        //Arrange
        var person = BuildPerson("Jan Kowalski");
        SetupEmptyScan();

        //Act
        _sut.Create(person, RootPath);

        //Assert
        _processor.Verify(x => x.WriteMeFile(PersonMeJson, person), Times.Once());
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void Create_WritesRelatedMeFile_WhenRelationshipExists()
    {
        //Arrange
        var person = BuildPersonWithParent("Jan Kowalski", RelatedId);
        var relatedMeFile = new MeFile { UniqueIdentifier = RelatedId, PersonName = "Anna Nowak" };
        SetupScanWithRelated(relatedMeFile);

        //Act
        _sut.Create(person, RootPath);

        //Assert
        _processor.Verify(x => x.WriteMeFile(RelatedMeJson, It.IsAny<MeFile>()), Times.Once());
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void Create_DoesNotWriteRelatedFile_WhenAlreadySynced()
    {
        //Arrange
        var person = BuildPersonWithParent("Jan Kowalski", RelatedId);
        var relatedMeFile = new MeFile
        {
            UniqueIdentifier = RelatedId,
            PersonName = "Anna Nowak",
            ChildrenId = new List<Guid> { PersonId },
            Children = new List<string> { "Jan Kowalski" },
        };
        SetupScanWithRelated(relatedMeFile);

        //Act
        _sut.Create(person, RootPath);

        //Assert
        _processor.Verify(x => x.WriteMeFile(RelatedMeJson, It.IsAny<MeFile>()), Times.Never());
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void Create_ContinuesAfterRelatedFileFailure_WhenWriteThrows()
    {
        //Arrange
        var person = BuildPersonWithParent("Jan Kowalski", RelatedId);
        var relatedMeFile = new MeFile { UniqueIdentifier = RelatedId, PersonName = "Anna Nowak" };
        SetupScanWithRelated(relatedMeFile);
        _processor
            .Setup(x => x.WriteMeFile(RelatedMeJson, It.IsAny<MeFile>()))
            .Throws<IOException>();

        //Act — must not throw
        _sut.Create(person, RootPath);

        //Assert — own file still written and failure was logged
        _processor.Verify(x => x.WriteMeFile(PersonMeJson, person), Times.Once());
        _mockLogger.Verify(
            x => x.Warning(It.IsAny<Exception>(), "Failed to sync relationship to {Path}", It.IsAny<string>()),
            Times.Once());
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void Create_UsesPersonNameAsFolder_WhenCalled()
    {
        //Arrange
        var person = BuildPerson("Jan Kowalski");
        SetupEmptyScan();

        //Act
        _sut.Create(person, RootPath);

        //Assert
        _fs.Verify(x => x.CreateDirectory(PersonFolder), Times.Once());
        _processor.Verify(x => x.WriteMeFile(PersonMeJson, person), Times.Once());
    }

    #endregion

    #region Update

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void Update_WritesOwnMeFile_WhenCalled()
    {
        //Arrange
        var person = BuildPerson("Jan Kowalski");
        var snapshot = BuildPerson("Jan Kowalski");
        SetupEmptyScan();

        //Act
        _sut.Update(person, snapshot, RootPath);

        //Assert
        _processor.Verify(x => x.WriteMeFile(PersonMeJson, person), Times.Once());
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void Update_CallsSyncForAddedRelationship_WhenRelationshipAdded()
    {
        //Arrange
        var snapshot = BuildPerson("Jan Kowalski");
        var person = new MeFile
        {
            UniqueIdentifier = PersonId,
            PersonName = "Jan Kowalski",
            ParentsId = new List<Guid> { RelatedId },
            Parents = new List<string> { "Anna Nowak" },
        };
        var relatedMeFile = new MeFile { UniqueIdentifier = RelatedId, PersonName = "Anna Nowak" };
        SetupScanWithRelated(relatedMeFile);

        //Act
        _sut.Update(person, snapshot, RootPath);

        //Assert — related file written because relationship was added
        _processor.Verify(x => x.WriteMeFile(RelatedMeJson, It.IsAny<MeFile>()), Times.Once());
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void Update_RemovesSyncForRemovedRelationship_WhenRelationshipRemoved()
    {
        //Arrange
        var snapshot = new MeFile
        {
            UniqueIdentifier = PersonId,
            PersonName = "Jan Kowalski",
            ParentsId = new List<Guid> { RelatedId },
            Parents = new List<string> { "Anna Nowak" },
        };
        var person = BuildPerson("Jan Kowalski");
        var relatedMeFile = new MeFile
        {
            UniqueIdentifier = RelatedId,
            PersonName = "Anna Nowak",
            ChildrenId = new List<Guid> { PersonId },
            Children = new List<string> { "Jan Kowalski" },
        };
        SetupScanWithRelated(relatedMeFile);

        //Act
        _sut.Update(person, snapshot, RootPath);

        //Assert — related file written to remove reverse entry
        _processor.Verify(x => x.WriteMeFile(RelatedMeJson, It.IsAny<MeFile>()), Times.Once());
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void Update_ProducesZeroRelatedWrites_WhenNoRelationshipChanges()
    {
        //Arrange
        var person = BuildPersonWithParent("Jan Kowalski", RelatedId);
        var snapshot = BuildPersonWithParent("Jan Kowalski", RelatedId);
        var relatedMeFile = new MeFile
        {
            UniqueIdentifier = RelatedId,
            PersonName = "Anna Nowak",
            ChildrenId = new List<Guid> { PersonId },
            Children = new List<string> { "Jan Kowalski" },
        };
        SetupScanWithRelated(relatedMeFile);

        //Act
        _sut.Update(person, snapshot, RootPath);

        //Assert — related file NOT written (already in sync; apply is idempotent → same ref → no write)
        _processor.Verify(x => x.WriteMeFile(RelatedMeJson, It.IsAny<MeFile>()), Times.Never());
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void Update_LogsWarning_WhenPersonNameChangedVsSnapshot()
    {
        //Arrange
        var snapshot = BuildPerson("Jan Kowalski");
        var person = new MeFile { UniqueIdentifier = PersonId, PersonName = "Jan Nowy" };
        SetupEmptyScan();

        //Act
        _sut.Update(person, snapshot, RootPath);

        //Assert
        _mockLogger.Verify(
            x => x.Warning(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()),
            Times.Once());
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void Update_ContinuesAfterRelatedFileFailure_WhenWriteThrows()
    {
        //Arrange
        var snapshot = BuildPerson("Jan Kowalski");
        var person = new MeFile
        {
            UniqueIdentifier = PersonId,
            PersonName = "Jan Kowalski",
            ParentsId = new List<Guid> { RelatedId },
            Parents = new List<string> { "Anna Nowak" },
        };
        var relatedMeFile = new MeFile { UniqueIdentifier = RelatedId, PersonName = "Anna Nowak" };
        SetupScanWithRelated(relatedMeFile);
        _processor
            .Setup(x => x.WriteMeFile(RelatedMeJson, It.IsAny<MeFile>()))
            .Throws<IOException>();

        //Act — must not throw
        _sut.Update(person, snapshot, RootPath);

        //Assert — own file written; failure logged
        _processor.Verify(x => x.WriteMeFile(PersonMeJson, person), Times.Once());
        _mockLogger.Verify(
            x => x.Warning(It.IsAny<Exception>(), It.IsAny<string>(), It.IsAny<string>()),
            Times.Once());
    }

    #endregion

    #region Helpers

    private MeFile BuildPerson(string personName)
    {
        return new MeFile
        {
            UniqueIdentifier = PersonId,
            PersonName = personName,
        };
    }

    private MeFile BuildPersonWithParent(string personName, Guid parentId)
    {
        return new MeFile
        {
            UniqueIdentifier = PersonId,
            PersonName = personName,
            ParentsId = new List<Guid> { parentId },
            Parents = new List<string> { "Anna Nowak" },
        };
    }

    private void SetupEmptyScan()
    {
        _processor.Setup(x => x.ScanMeFiles(RootPath)).Returns(Array.Empty<string>());
    }

    private void SetupScanWithRelated(MeFile relatedMeFile)
    {
        _processor.Setup(x => x.ScanMeFiles(RootPath)).Returns(new[] { RelatedMeJson });
        _processor.Setup(x => x.ReadMeFile(RelatedMeJson)).Returns(relatedMeFile);
    }

    #endregion
}
