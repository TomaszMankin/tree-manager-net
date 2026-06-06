using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using TreeManager.Common.TestUtilities;
using TreeManager.Core.Domain;
using TreeManager.Core.Services.Validation;
using TreeManager.Infrastructure.IO;
using TreeManager.Infrastructure.Persistence;

namespace TreeManager.Infrastructure.L1.Validation;

public class TreeConsistencyValidatorIntegrationTests : IDisposable
{
    private const string ListaOsobFolder = "Lista osób";

    private readonly string _tempRoot;
    private readonly MeFileProcessor _processor;
    private readonly TreeConsistencyValidator _sut;

    public TreeConsistencyValidatorIntegrationTests()
    {
        _tempRoot = Path.Combine(
            Path.GetTempPath(),
            "TreeManagerL1Validator_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempRoot);

        var fs = new FileSystemFacade();
        _processor = new MeFileProcessor(fs);
        _sut = new TreeConsistencyValidator();
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempRoot))
        {
            Directory.Delete(_tempRoot, recursive: true);
        }
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L1)]
    public void Validate_ReturnsNoIssues_WhenCleanFixtureTree()
    {
        //Arrange — three-person consistent tree: two parents + one child
        var fatherId = Guid.NewGuid();
        var motherId = Guid.NewGuid();
        var childId = Guid.NewGuid();

        WriteMeFile(fatherId, "Władysław", "Kowalski", Sex.Male,
            spouseIds: [motherId], childrenIds: [childId]);
        WriteMeFile(motherId, "Zofia", "Kowalska", Sex.Female,
            spouseIds: [fatherId], childrenIds: [childId]);
        WriteMeFile(childId, "Tomasz", "Kowalski", Sex.Male,
            parentIds: [fatherId, motherId]);

        // Scan via real MeFileProcessor
        var people = new Dictionary<Guid, MeFile>();
        foreach (var path in _processor.ScanMeFiles(_tempRoot))
        {
            var meFile = _processor.ReadMeFile(path);
            if (meFile.UniqueIdentifier != Guid.Empty)
            {
                people[meFile.UniqueIdentifier] = meFile;
            }
        }

        //Act
        var issues = _sut.Validate(people);

        //Assert
        Assert.Empty(issues);
    }

    #region Helpers

    private void WriteMeFile(
        Guid id,
        string firstName,
        string lastName,
        Sex sex,
        List<Guid> parentIds = null,
        List<Guid> childrenIds = null,
        List<Guid> spouseIds = null)
    {
        var folderName = $"{firstName} {lastName}";
        var personFolder = Path.Combine(_tempRoot, ListaOsobFolder, folderName);
        Directory.CreateDirectory(personFolder);

        var meFile = new MeFile
        {
            UniqueIdentifier = id,
            PersonName = folderName,
            Location = personFolder,
            FirstName = firstName,
            LastName = lastName,
            Sex = sex,
            ParentsId = parentIds ?? [],
            ChildrenId = childrenIds ?? [],
            SpouseId = spouseIds ?? [],
        };

        var json = JsonSerializer.Serialize(meFile, MeFile.DefaultOptions);
        File.WriteAllText(
            Path.Combine(personFolder, "me.json"),
            json,
            System.Text.Encoding.UTF8);
    }

    #endregion
}
