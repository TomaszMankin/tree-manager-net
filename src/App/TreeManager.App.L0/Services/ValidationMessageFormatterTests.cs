using System;
using System.Collections.Generic;
using System.Linq;
using TreeManager.App.Services;
using TreeManager.Common.TestUtilities;
using TreeManager.Core.Domain;
using TreeManager.Core.Validation;

namespace TreeManager.App.L0.Services;

public class ValidationMessageFormatterTests
{
    private readonly ValidationMessageFormatter _sut;

    public ValidationMessageFormatterTests()
    {
        _sut = new ValidationMessageFormatter();
    }

    #region Format — Cycle

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void Format_ReturnsCycleString_WhenCycleIssueGiven()
    {
        //Arrange
        var idA = Guid.NewGuid();
        var idB = Guid.NewGuid();
        var personA = PersonFixtureFactory.Build(idA, "Adam", "Kowalski", Sex.Male);
        var personB = PersonFixtureFactory.Build(idB, "Bożena", "Nowak", Sex.Female);
        var people = PersonFixtureFactory.BuildMap(personA, personB);
        var issues = new List<ValidationIssue>
        {
            new() { Kind = ValidationIssueKind.Cycle, Subjects = [idA, idB] },
        };

        //Act
        var messages = _sut.Format(issues, people);

        //Assert
        Assert.Single(messages);
        Assert.Contains("Cykl", messages[0]);
        Assert.Contains("Adam Kowalski", messages[0]);
        Assert.Contains("Bożena Nowak", messages[0]);
    }

    #endregion

    #region Format — OneSidedRelationship

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void Format_ReturnsOneSidedString_WhenOneSidedIssueGiven()
    {
        //Arrange
        var idA = Guid.NewGuid();
        var idB = Guid.NewGuid();
        var personA = PersonFixtureFactory.Build(idA, "Jan", "Wiśniewski", Sex.Male);
        var personB = PersonFixtureFactory.Build(idB, "Maria", "Wiśniewska", Sex.Female);
        var people = PersonFixtureFactory.BuildMap(personA, personB);
        var issues = new List<ValidationIssue>
        {
            new() { Kind = ValidationIssueKind.OneSidedRelationship, Subjects = [idA, idB] },
        };

        //Act
        var messages = _sut.Format(issues, people);

        //Assert
        Assert.Single(messages);
        Assert.Contains("Jednostronna", messages[0]);
        Assert.Contains("Jan Wiśniewski", messages[0]);
        Assert.Contains("Maria Wiśniewska", messages[0]);
    }

    #endregion

    #region Format — Orphan

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void Format_ReturnsOrphanString_WhenOrphanIssueGiven()
    {
        //Arrange
        var personId = Guid.NewGuid();
        var person = PersonFixtureFactory.Build(personId, "Samotna", "Nowicki", Sex.Female);
        var people = PersonFixtureFactory.BuildMap(person);
        var issues = new List<ValidationIssue>
        {
            new() { Kind = ValidationIssueKind.Orphan, Subjects = [personId] },
        };

        //Act
        var messages = _sut.Format(issues, people);

        //Assert
        Assert.Single(messages);
        Assert.Contains("Samotna Nowicki", messages[0]);
        Assert.Contains("bez powiązań", messages[0]);
    }

    #endregion

    #region Format — StaleReference

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void Format_ReturnsStaleReferenceString_WhenStaleReferenceIssueGiven()
    {
        //Arrange
        var referrerId = Guid.NewGuid();
        var ghostId = Guid.NewGuid();
        var referrer = PersonFixtureFactory.Build(referrerId, "Tomasz", "Nowak", Sex.Male);
        var people = PersonFixtureFactory.BuildMap(referrer);
        var issues = new List<ValidationIssue>
        {
            new() { Kind = ValidationIssueKind.StaleReference, Subjects = [referrerId, ghostId] },
        };

        //Act
        var messages = _sut.Format(issues, people);

        //Assert
        Assert.Single(messages);
        Assert.Contains("Tomasz Nowak", messages[0]);
        Assert.Contains("brak folderu", messages[0]);
    }

    #endregion

    #region Format — Empty

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void Format_ReturnsEmpty_WhenNoIssues()
    {
        //Arrange
        var issues = new List<ValidationIssue>();
        var people = new Dictionary<Guid, MeFile>();

        //Act
        var messages = _sut.Format(issues, people);

        //Assert
        Assert.Empty(messages);
    }

    #endregion

    #region Format — unknown name

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void Format_IncludesIdentifierFragment_WhenPersonNameUnknown()
    {
        //Arrange — person has empty first and last name
        var personId = Guid.NewGuid();
        var person = PersonFixtureFactory.Build(personId, "", "", Sex.Male);
        var people = PersonFixtureFactory.BuildMap(person);
        var issues = new List<ValidationIssue>
        {
            new() { Kind = ValidationIssueKind.Orphan, Subjects = [personId] },
        };

        //Act
        var messages = _sut.Format(issues, people);

        //Assert — message must contain first 8 chars of the UUID in brackets
        Assert.Single(messages);
        Assert.Contains($"[{personId.ToString()[..8]}]", messages[0]);
    }

    #endregion
}
