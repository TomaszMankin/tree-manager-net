using System;
using System.Collections.Generic;
using System.Linq;
using TreeManager.Common.TestUtilities;
using TreeManager.UiTesting.Graph;
using TreeManager.UiTesting.Verification;

namespace TreeManager.UiTesting.L0.Verification;

public sealed class TreeGraphVerifierTests
{
    private const string PersonFolder = "Jan Kowalski";
    private const string SpouseFolder = "Anna Nowak";

    private readonly Guid _personUid = Guid.NewGuid();
    private readonly Guid _spouseUid = Guid.NewGuid();
    private readonly TreeGraphVerifier _sut = new TreeGraphVerifier();

    #region Verify

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void Verify_ReportsMatch_WhenActualMirrorsExpected()
    {
        //Arrange
        var expected = BuildSpouseExpected();
        var actual = BuildActualFrom(expected);

        //Act
        var result = _sut.Verify(expected, actual);

        //Assert
        Assert.True(result.IsMatch, result.Describe());
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void Verify_ReportsMissingLink_WhenActualOmitsExpectedEdge()
    {
        //Arrange
        var expected = BuildSpouseExpected();
        var actual = new ActualTreeGraph();
        var personNode = new PersonNode(PersonFolder, _personUid);
        personNode.SpouseIds.Add(_spouseUid);
        actual.Add(personNode);
        var spouseNode = new PersonNode(SpouseFolder, _spouseUid);
        spouseNode.SpouseIds.Add(_personUid);
        spouseNode.LinkEdges.Add(new LinkEdge(RelationshipSubfolders.Spouses, PersonFolder));
        actual.Add(spouseNode);

        //Act
        var result = _sut.Verify(expected, actual);

        //Assert
        Assert.False(result.IsMatch);
        Assert.Contains(result.MissingLinks, l =>
            l.PersonFolderName == PersonFolder
            && l.Subfolder == RelationshipSubfolders.Spouses
            && l.TargetFolderName == SpouseFolder);
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void Verify_ReportsExtraLink_WhenActualHasOrphanEdge()
    {
        //Arrange
        var expected = BuildSpouseExpected();
        var actual = BuildActualFrom(expected);
        var personNode = actual.Nodes.Single(n => n.UniqueIdentifier == _personUid);
        personNode.LinkEdges.Add(new LinkEdge(RelationshipSubfolders.Children, "Orphan Folder"));

        //Act
        var result = _sut.Verify(expected, actual);

        //Assert
        Assert.False(result.IsMatch);
        Assert.Contains(result.ExtraLinks, l =>
            l.PersonFolderName == PersonFolder
            && l.Subfolder == RelationshipSubfolders.Children
            && l.TargetFolderName == "Orphan Folder");
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void Verify_ReportsBothMissingAndExtra_WhenLinkTargetIsWrong()
    {
        //Arrange
        var expected = BuildSpouseExpected();
        var actual = BuildActualFrom(expected);
        var personNode = actual.Nodes.Single(n => n.UniqueIdentifier == _personUid);
        personNode.LinkEdges.Clear();
        personNode.LinkEdges.Add(new LinkEdge(RelationshipSubfolders.Spouses, "Wrong Target"));

        //Act
        var result = _sut.Verify(expected, actual);

        //Assert
        Assert.False(result.IsMatch);
        Assert.Contains(result.MissingLinks, l => l.TargetFolderName == SpouseFolder);
        Assert.Contains(result.ExtraLinks, l => l.TargetFolderName == "Wrong Target");
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void Verify_ReportsMissingRelationship_WhenActualOmitsReferenceArrayEntry()
    {
        //Arrange
        var expected = BuildSpouseExpected();
        var actual = BuildActualFrom(expected);
        var personNode = actual.Nodes.Single(n => n.UniqueIdentifier == _personUid);
        personNode.SpouseIds.Clear();

        //Act
        var result = _sut.Verify(expected, actual);

        //Assert
        Assert.False(result.IsMatch);
        Assert.Contains(result.MissingRelationships, r => r.PersonUid == _personUid && r.RelatedUid == _spouseUid);
    }

    #endregion

    private ExpectedTreeGraph BuildSpouseExpected()
    {
        var graph = new ExpectedTreeGraph();
        graph.AddPerson(PersonFolder, _personUid);
        graph.AddPerson(SpouseFolder, _spouseUid);
        graph.AddRelationship(_personUid, _spouseUid, RelationshipKind.Spouse);
        return graph;
    }

    private static ActualTreeGraph BuildActualFrom(ExpectedTreeGraph expected)
    {
        var actual = new ActualTreeGraph();
        foreach (var node in expected.Nodes)
        {
            var copy = new PersonNode(node.FolderName, node.UniqueIdentifier);
            CopyInto(node.ParentIds, copy.ParentIds);
            CopyInto(node.ChildIds, copy.ChildIds);
            CopyInto(node.SpouseIds, copy.SpouseIds);
            CopyInto(node.SiblingIds, copy.SiblingIds);
            foreach (var edge in node.LinkEdges)
            {
                copy.LinkEdges.Add(edge);
            }
            actual.Add(copy);
        }
        return actual;
    }

    private static void CopyInto(HashSet<Guid> source, HashSet<Guid> target)
    {
        foreach (var id in source)
        {
            target.Add(id);
        }
    }
}
