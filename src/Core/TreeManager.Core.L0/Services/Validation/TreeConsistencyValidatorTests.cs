using System;
using System.Collections.Generic;
using System.Linq;
using TreeManager.Common.TestUtilities;
using TreeManager.Core.Domain;
using TreeManager.Core.Validation;
using TreeManager.Core.Services.Validation;

namespace TreeManager.Core.L0.Services.Validation;

public class TreeConsistencyValidatorTests
{
    private readonly TreeConsistencyValidator _sut;

    public TreeConsistencyValidatorTests()
    {
        _sut = new TreeConsistencyValidator();
    }

    #region Validate — StaleReference

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void Validate_DetectsStaleReference_WhenParentGuidNotInMap()
    {
        //Arrange
        var personId = Guid.NewGuid();
        var ghostId = Guid.NewGuid();
        var person = PersonFixtureFactory.Build(personId, "Jan", "Kowalski", Sex.Male,
            parentIds: [ghostId]);
        var people = PersonFixtureFactory.BuildMap(person);

        //Act
        var issues = _sut.Validate(people);

        //Assert
        var stale = issues.Where(i => i.Kind == ValidationIssueKind.StaleReference).ToList();
        Assert.Single(stale);
        Assert.Contains(personId, stale[0].Subjects);
        Assert.Contains(ghostId, stale[0].Subjects);
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void Validate_DetectsStaleReference_WhenChildGuidNotInMap()
    {
        //Arrange
        var personId = Guid.NewGuid();
        var ghostId = Guid.NewGuid();
        var person = PersonFixtureFactory.Build(personId, "Anna", "Nowak", Sex.Female,
            childrenIds: [ghostId]);
        var people = PersonFixtureFactory.BuildMap(person);

        //Act
        var issues = _sut.Validate(people);

        //Assert
        var stale = issues.Where(i => i.Kind == ValidationIssueKind.StaleReference).ToList();
        Assert.Single(stale);
        Assert.Contains(personId, stale[0].Subjects);
        Assert.Contains(ghostId, stale[0].Subjects);
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void Validate_DetectsStaleReference_WhenSpouseGuidNotInMap()
    {
        //Arrange
        var personId = Guid.NewGuid();
        var ghostId = Guid.NewGuid();
        var person = PersonFixtureFactory.Build(personId, "Piotr", "Wiśniewski", Sex.Male,
            spouseIds: [ghostId]);
        var people = PersonFixtureFactory.BuildMap(person);

        //Act
        var issues = _sut.Validate(people);

        //Assert
        var stale = issues.Where(i => i.Kind == ValidationIssueKind.StaleReference).ToList();
        Assert.Single(stale);
        Assert.Contains(personId, stale[0].Subjects);
        Assert.Contains(ghostId, stale[0].Subjects);
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void Validate_DoesNotReportOneSidedRelationship_WhenTargetMissingFromMap()
    {
        //Arrange — stale reference should not also generate one-sided
        var personId = Guid.NewGuid();
        var ghostId = Guid.NewGuid();
        var person = PersonFixtureFactory.Build(personId, "Jan", "Nowicki", Sex.Male,
            spouseIds: [ghostId]);
        var people = PersonFixtureFactory.BuildMap(person);

        //Act
        var issues = _sut.Validate(people);

        //Assert — stale present, one-sided absent
        Assert.Contains(issues, i => i.Kind == ValidationIssueKind.StaleReference);
        Assert.DoesNotContain(issues, i => i.Kind == ValidationIssueKind.OneSidedRelationship);
    }

    #endregion

    #region Validate — Cycle

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void Validate_DetectsCycle_WhenSimpleTwoNodeCycleAToBToA()
    {
        //Arrange
        var idA = Guid.NewGuid();
        var idB = Guid.NewGuid();
        var personA = PersonFixtureFactory.Build(idA, "Jan", "Kowalski", Sex.Male,
            parentIds: [idB], childrenIds: [idB]);
        var personB = PersonFixtureFactory.Build(idB, "Anna", "Kowalska", Sex.Female,
            parentIds: [idA], childrenIds: [idA]);
        var people = PersonFixtureFactory.BuildMap(personA, personB);

        //Act
        var issues = _sut.Validate(people);

        //Assert — exactly one cycle, not just "any"
        Assert.Equal(1, issues.Count(i => i.Kind == ValidationIssueKind.Cycle));
        var cycle = issues.First(i => i.Kind == ValidationIssueKind.Cycle);
        Assert.Contains(idA, cycle.Subjects);
        Assert.Contains(idB, cycle.Subjects);
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void Validate_DoesNotReportCycle_WhenDiamondGraph()
    {
        //Arrange — A→B→D, A→C→D
        var idA = Guid.NewGuid();
        var idB = Guid.NewGuid();
        var idC = Guid.NewGuid();
        var idD = Guid.NewGuid();

        var personA = PersonFixtureFactory.Build(idA, "Adam", "Kowalski", Sex.Male,
            childrenIds: [idB, idC]);
        var personB = PersonFixtureFactory.Build(idB, "Bartek", "Kowalski", Sex.Male,
            parentIds: [idA], childrenIds: [idD]);
        var personC = PersonFixtureFactory.Build(idC, "Celina", "Kowalska", Sex.Female,
            parentIds: [idA], childrenIds: [idD]);
        var personD = PersonFixtureFactory.Build(idD, "Dorota", "Kowalska", Sex.Female,
            parentIds: [idB, idC]);
        var people = PersonFixtureFactory.BuildMap(personA, personB, personC, personD);

        //Act
        var issues = _sut.Validate(people);

        //Assert
        Assert.DoesNotContain(issues, i => i.Kind == ValidationIssueKind.Cycle);
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void Validate_DoesNotReportCycle_WhenRemarriageGraph()
    {
        //Arrange — M and F are spouses, both parents of C
        var idM = Guid.NewGuid();
        var idF = Guid.NewGuid();
        var idC = Guid.NewGuid();

        var personM = PersonFixtureFactory.Build(idM, "Marek", "Nowak", Sex.Male,
            spouseIds: [idF], childrenIds: [idC]);
        var personF = PersonFixtureFactory.Build(idF, "Felicja", "Nowak", Sex.Female,
            spouseIds: [idM], childrenIds: [idC]);
        var personC = PersonFixtureFactory.Build(idC, "Cezary", "Nowak", Sex.Male,
            parentIds: [idM, idF]);
        var people = PersonFixtureFactory.BuildMap(personM, personF, personC);

        //Act
        var issues = _sut.Validate(people);

        //Assert
        Assert.DoesNotContain(issues, i => i.Kind == ValidationIssueKind.Cycle);
    }

    #endregion

    #region Validate — OneSidedRelationship

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void Validate_DetectsOneSidedRelationship_WhenAClaimsSpouseButBSilent()
    {
        //Arrange
        var idA = Guid.NewGuid();
        var idB = Guid.NewGuid();
        var personA = PersonFixtureFactory.Build(idA, "Jan", "Wiśniewski", Sex.Male,
            spouseIds: [idB]);
        var personB = PersonFixtureFactory.Build(idB, "Maria", "Wiśniewska", Sex.Female);
        var people = PersonFixtureFactory.BuildMap(personA, personB);

        //Act
        var issues = _sut.Validate(people);

        //Assert
        var oneSided = issues.Where(i => i.Kind == ValidationIssueKind.OneSidedRelationship).ToList();
        Assert.Single(oneSided);
        Assert.Equal(idA, oneSided[0].Subjects[0]);
        Assert.Equal(idB, oneSided[0].Subjects[1]);
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void Validate_DetectsOneSidedRelationship_WhenAClaimsParentButParentDoesNotListChildBack()
    {
        //Arrange
        var idChild = Guid.NewGuid();
        var idParent = Guid.NewGuid();
        var child = PersonFixtureFactory.Build(idChild, "Tomek", "Nowicki", Sex.Male,
            parentIds: [idParent]);
        var parent = PersonFixtureFactory.Build(idParent, "Stefan", "Nowicki", Sex.Male);
        var people = PersonFixtureFactory.BuildMap(child, parent);

        //Act
        var issues = _sut.Validate(people);

        //Assert
        var oneSided = issues.Where(i => i.Kind == ValidationIssueKind.OneSidedRelationship).ToList();
        Assert.Single(oneSided);
        Assert.Equal(idChild, oneSided[0].Subjects[0]);
        Assert.Equal(idParent, oneSided[0].Subjects[1]);
    }

    #endregion

    #region Validate — Orphan

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void Validate_DetectsOrphan_WhenPersonHasZeroLinks()
    {
        //Arrange
        var personId = Guid.NewGuid();
        var person = PersonFixtureFactory.Build(personId, "Samotna", "Nowak", Sex.Female);
        var people = PersonFixtureFactory.BuildMap(person);

        //Act
        var issues = _sut.Validate(people);

        //Assert
        var orphans = issues.Where(i => i.Kind == ValidationIssueKind.Orphan).ToList();
        Assert.Single(orphans);
        Assert.Equal(personId, orphans[0].Subjects[0]);
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void DetectOrphans_DoesNotReportOrphan_WhenPersonHasOnlySibling()
    {
        //Arrange — B-005: person with only a sibling relation must NOT be flagged orphan
        var idA = Guid.NewGuid();
        var idB = Guid.NewGuid();
        var personA = PersonFixtureFactory.Build(idA, "Adam", "Braterski", Sex.Male,
            siblingIds: [idB]);
        var personB = PersonFixtureFactory.Build(idB, "Basia", "Braterska", Sex.Female,
            siblingIds: [idA]);
        var people = PersonFixtureFactory.BuildMap(personA, personB);

        //Act
        var issues = _sut.Validate(people);

        //Assert
        Assert.DoesNotContain(issues, i => i.Kind == ValidationIssueKind.Orphan);
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void Validate_ReturnsNoIssues_WhenSiblingPairFullyBidirectional()
    {
        //Arrange
        var idA = Guid.NewGuid();
        var idB = Guid.NewGuid();
        var personA = PersonFixtureFactory.Build(idA, "Adam", "Siódmy", Sex.Male,
            siblingIds: [idB]);
        var personB = PersonFixtureFactory.Build(idB, "Basia", "Siódma", Sex.Female,
            siblingIds: [idA]);
        var people = PersonFixtureFactory.BuildMap(personA, personB);

        //Act
        var issues = _sut.Validate(people);

        //Assert — no issues for a proper bidirectional sibling pair
        Assert.Empty(issues);
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void DetectOneSidedRelationships_ReportsChildDirection_WhenChildMissingParentBackref()
    {
        //Arrange — A lists B as child, but B does not list A as parent
        var idA = Guid.NewGuid();
        var idB = Guid.NewGuid();
        var personA = PersonFixtureFactory.Build(idA, "Anna", "Matka", Sex.Female,
            childrenIds: [idB]);
        var personB = PersonFixtureFactory.Build(idB, "Bartek", "Syn", Sex.Male);
        var people = PersonFixtureFactory.BuildMap(personA, personB);

        //Act
        var issues = _sut.Validate(people);

        //Assert
        var oneSided = issues.Where(i => i.Kind == ValidationIssueKind.OneSidedRelationship).ToList();
        Assert.Single(oneSided);
        Assert.Equal(idA, oneSided[0].Subjects[0]);
        Assert.Equal(idB, oneSided[0].Subjects[1]);
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void Validate_ReturnsNoIssues_WhenParentChildFullyBidirectional()
    {
        //Arrange — parent lists child, child lists parent
        var idA = Guid.NewGuid();
        var idB = Guid.NewGuid();
        var personA = PersonFixtureFactory.Build(idA, "Anna", "Rodzic", Sex.Female,
            childrenIds: [idB]);
        var personB = PersonFixtureFactory.Build(idB, "Bartek", "Dziecko", Sex.Male,
            parentIds: [idA]);
        var people = PersonFixtureFactory.BuildMap(personA, personB);

        //Act
        var issues = _sut.Validate(people);

        //Assert
        Assert.Empty(issues);
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void Validate_DoesNotReportOrphan_WhenPersonHasAtLeastOneLink()
    {
        //Arrange
        var idA = Guid.NewGuid();
        var idB = Guid.NewGuid();
        var personA = PersonFixtureFactory.Build(idA, "Jan", "Kowalski", Sex.Male,
            spouseIds: [idB]);
        var personB = PersonFixtureFactory.Build(idB, "Anna", "Kowalska", Sex.Female,
            spouseIds: [idA]);
        var people = PersonFixtureFactory.BuildMap(personA, personB);

        //Act
        var issues = _sut.Validate(people);

        //Assert
        Assert.DoesNotContain(issues, i => i.Kind == ValidationIssueKind.Orphan);
    }

    #endregion

    #region Validate — Clean tree

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void Validate_ReturnsEmpty_WhenAllRelationshipsConsistent()
    {
        //Arrange — A and B are spouses with child C; all links bidirectional
        var idA = Guid.NewGuid();
        var idB = Guid.NewGuid();
        var idC = Guid.NewGuid();

        var personA = PersonFixtureFactory.Build(idA, "Adam", "Nowak", Sex.Male,
            spouseIds: [idB], childrenIds: [idC]);
        var personB = PersonFixtureFactory.Build(idB, "Bożena", "Nowak", Sex.Female,
            spouseIds: [idA], childrenIds: [idC]);
        var personC = PersonFixtureFactory.Build(idC, "Celina", "Nowak", Sex.Female,
            parentIds: [idA, idB]);
        var people = PersonFixtureFactory.BuildMap(personA, personB, personC);

        //Act
        var issues = _sut.Validate(people);

        //Assert
        Assert.Empty(issues);
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void Validate_DoesNotThrow_WhenDataIsCorrupt()
    {
        //Arrange — simple two-node cycle
        var idA = Guid.NewGuid();
        var idB = Guid.NewGuid();
        var personA = PersonFixtureFactory.Build(idA, "Jan", "Kowalski", Sex.Male,
            childrenIds: [idB]);
        var personB = PersonFixtureFactory.Build(idB, "Anna", "Kowalska", Sex.Female,
            childrenIds: [idA]);
        var people = PersonFixtureFactory.BuildMap(personA, personB);

        //Act + Assert — must not throw
        var exception = Record.Exception(() => _sut.Validate(people));
        Assert.Null(exception);
    }

    #endregion
}
