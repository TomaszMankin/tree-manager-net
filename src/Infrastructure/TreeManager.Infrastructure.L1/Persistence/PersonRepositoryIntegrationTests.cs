using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using TreeManager.Common.TestUtilities;
using TreeManager.Core.Domain;
using TreeManager.Infrastructure.IO;
using TreeManager.Infrastructure.Persistence;

namespace TreeManager.Infrastructure.L1.Persistence;

public class PersonRepositoryIntegrationTests : IDisposable
{
    private readonly string _rootPath;
    private readonly PersonRepository _sut;

    public PersonRepositoryIntegrationTests()
    {
        _rootPath = Path.Combine(Path.GetTempPath(), "TreeManagerL1_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(_rootPath, "Lista osób"));

        var fs = new FileSystemFacade();
        var processor = new MeFileProcessor(fs);
        _sut = new PersonRepository(fs, processor, Serilog.Log.Logger);
    }

    #region Create

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L1)]
    public void Create_WritesTwoMeJsonFiles_WhenPersonAndRelatedExistInTmpDir()
    {
        //Arrange
        var relatedId = Guid.NewGuid();
        var relatedMeFile = new MeFile
        {
            UniqueIdentifier = relatedId,
            PersonName = "Anna Nowak",
        };
        var relatedFolder = Path.Combine(_rootPath, "Lista osób", "Anna Nowak");
        Directory.CreateDirectory(relatedFolder);
        File.WriteAllText(
            Path.Combine(relatedFolder, "me.json"),
            JsonSerializer.Serialize(relatedMeFile, MeFile.DefaultOptions));

        var person = new MeFile
        {
            UniqueIdentifier = Guid.NewGuid(),
            PersonName = "Jan Kowalski",
            ParentsId = new List<Guid> { relatedId },
            Parents = new List<string> { "Anna Nowak" },
        };

        //Act
        _sut.Create(person, _rootPath);

        //Assert
        var personMeJson = Path.Combine(_rootPath, "Lista osób", "Jan Kowalski", "me.json");
        var relatedMeJson = Path.Combine(_rootPath, "Lista osób", "Anna Nowak", "me.json");
        Assert.True(File.Exists(personMeJson));
        var updatedRelated = JsonSerializer.Deserialize<MeFile>(
            File.ReadAllText(relatedMeJson), MeFile.DefaultOptions);
        Assert.Contains(person.UniqueIdentifier, updatedRelated.ChildrenId);
        Assert.Contains("Jan Kowalski", updatedRelated.Children);
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L1)]
    public void Create_DoesNotDuplicateRelationship_WhenSavedTwice()
    {
        //Arrange
        var relatedId = Guid.NewGuid();
        var relatedMeFile = new MeFile
        {
            UniqueIdentifier = relatedId,
            PersonName = "Anna Nowak",
        };
        var relatedFolder = Path.Combine(_rootPath, "Lista osób", "Anna Nowak");
        Directory.CreateDirectory(relatedFolder);
        File.WriteAllText(
            Path.Combine(relatedFolder, "me.json"),
            JsonSerializer.Serialize(relatedMeFile, MeFile.DefaultOptions));

        var person = new MeFile
        {
            UniqueIdentifier = Guid.NewGuid(),
            PersonName = "Jan Kowalski",
            ParentsId = new List<Guid> { relatedId },
            Parents = new List<string> { "Anna Nowak" },
        };

        //Act
        _sut.Create(person, _rootPath);
        _sut.Create(person, _rootPath);

        //Assert
        var relatedMeJson = Path.Combine(_rootPath, "Lista osób", "Anna Nowak", "me.json");
        var updatedRelated = JsonSerializer.Deserialize<MeFile>(
            File.ReadAllText(relatedMeJson), MeFile.DefaultOptions);
        Assert.Single(updatedRelated.ChildrenId);
        Assert.Single(updatedRelated.Children);
    }

    #endregion

    #region Update

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L1)]
    public void Update_WritesEditedMeFile_WhenFieldsChanged()
    {
        //Arrange
        var personId = Guid.NewGuid();
        var original = new MeFile
        {
            UniqueIdentifier = personId,
            PersonName = "Jan Kowalski",
            FirstName = "Jan",
        };
        var personFolder = Path.Combine(_rootPath, "Lista osób", "Jan Kowalski");
        Directory.CreateDirectory(personFolder);
        File.WriteAllText(
            Path.Combine(personFolder, "me.json"),
            JsonSerializer.Serialize(original, MeFile.DefaultOptions));

        var edited = original with { FirstName = "Janusz" };

        //Act
        _sut.Update(edited, original, _rootPath);

        //Assert
        var written = JsonSerializer.Deserialize<MeFile>(
            File.ReadAllText(Path.Combine(personFolder, "me.json")), MeFile.DefaultOptions);
        Assert.Equal("Janusz", written.FirstName);
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L1)]
    public void Update_RemovesRelationshipFromRelated_WhenParentRemoved()
    {
        //Arrange
        var personId = Guid.NewGuid();
        var parentId = Guid.NewGuid();

        var parentFolder = Path.Combine(_rootPath, "Lista osób", "Anna Nowak");
        Directory.CreateDirectory(parentFolder);
        var parentOriginal = new MeFile
        {
            UniqueIdentifier = parentId,
            PersonName = "Anna Nowak",
            ChildrenId = new List<Guid> { personId },
            Children = new List<string> { "Jan Kowalski" },
        };
        File.WriteAllText(
            Path.Combine(parentFolder, "me.json"),
            JsonSerializer.Serialize(parentOriginal, MeFile.DefaultOptions));

        var personFolder = Path.Combine(_rootPath, "Lista osób", "Jan Kowalski");
        Directory.CreateDirectory(personFolder);
        var snapshot = new MeFile
        {
            UniqueIdentifier = personId,
            PersonName = "Jan Kowalski",
            ParentsId = new List<Guid> { parentId },
            Parents = new List<string> { "Anna Nowak" },
        };
        File.WriteAllText(
            Path.Combine(personFolder, "me.json"),
            JsonSerializer.Serialize(snapshot, MeFile.DefaultOptions));

        var edited = snapshot with { ParentsId = [], Parents = [] };

        //Act
        _sut.Update(edited, snapshot, _rootPath);

        //Assert
        var updatedParent = JsonSerializer.Deserialize<MeFile>(
            File.ReadAllText(Path.Combine(parentFolder, "me.json")), MeFile.DefaultOptions);
        Assert.DoesNotContain(personId, updatedParent.ChildrenId);
        Assert.DoesNotContain("Jan Kowalski", updatedParent.Children);
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L1)]
    public void Update_ProducesZeroExtraWrites_WhenNoChanges()
    {
        //Arrange
        var personId = Guid.NewGuid();
        var parentId = Guid.NewGuid();

        var parentFolder = Path.Combine(_rootPath, "Lista osób", "Anna Nowak");
        Directory.CreateDirectory(parentFolder);
        var parentFile = new MeFile
        {
            UniqueIdentifier = parentId,
            PersonName = "Anna Nowak",
            ChildrenId = new List<Guid> { personId },
            Children = new List<string> { "Jan Kowalski" },
        };
        File.WriteAllText(
            Path.Combine(parentFolder, "me.json"),
            JsonSerializer.Serialize(parentFile, MeFile.DefaultOptions));

        var personFolder = Path.Combine(_rootPath, "Lista osób", "Jan Kowalski");
        Directory.CreateDirectory(personFolder);
        var snapshot = new MeFile
        {
            UniqueIdentifier = personId,
            PersonName = "Jan Kowalski",
            ParentsId = new List<Guid> { parentId },
            Parents = new List<string> { "Anna Nowak" },
        };
        File.WriteAllText(
            Path.Combine(personFolder, "me.json"),
            JsonSerializer.Serialize(snapshot, MeFile.DefaultOptions));

        var parentLastWrite = File.GetLastWriteTimeUtc(Path.Combine(parentFolder, "me.json"));

        //Act
        _sut.Update(snapshot, snapshot, _rootPath);

        //Assert — parent file NOT touched (no delta)
        var parentCurrentWrite = File.GetLastWriteTimeUtc(Path.Combine(parentFolder, "me.json"));
        Assert.Equal(parentLastWrite, parentCurrentWrite);
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
