using System;
using System.Linq;
using TreeManager.Common.TestUtilities;
using TreeManager.UiTesting.Graph;

namespace TreeManager.UiTesting.L0.Graph;

public sealed class ExpectedTreeGraphTests
{
    private const string PersonFolder = "Jan Kowalski";
    private const string RelatedFolder = "Anna Nowak";

    private readonly Guid _personUid = Guid.NewGuid();
    private readonly Guid _relatedUid = Guid.NewGuid();
    private readonly ExpectedTreeGraph _sut = new ExpectedTreeGraph();

    public ExpectedTreeGraphTests()
    {
        _sut.AddPerson(PersonFolder, _personUid);
        _sut.AddPerson(RelatedFolder, _relatedUid);
    }

    #region AddPerson

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void AddPerson_ReturnsExistingNode_WhenUidAlreadyPresent()
    {
        //Arrange
        var first = _sut.AddPerson("Other Folder", _personUid);

        //Act
        var second = _sut.AddPerson("Different Folder", _personUid);

        //Assert
        Assert.Same(first, second);
    }

    #endregion

    #region AddRelationship

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void AddRelationship_DerivesParentAndReciprocalChildReferences_WhenKindIsParent()
    {
        //Act
        _sut.AddRelationship(_personUid, _relatedUid, RelationshipKind.Parent);

        //Assert
        var person = GetNode(_personUid);
        var related = GetNode(_relatedUid);
        Assert.Contains(_relatedUid, person.ParentIds);
        Assert.Contains(_personUid, related.ChildIds);
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void AddRelationship_DerivesParentAndReciprocalChildLinkEdges_WhenKindIsParent()
    {
        //Act
        _sut.AddRelationship(_personUid, _relatedUid, RelationshipKind.Parent);

        //Assert
        var person = GetNode(_personUid);
        var related = GetNode(_relatedUid);
        Assert.Contains(new LinkEdge(RelationshipSubfolders.Parents, RelatedFolder), person.LinkEdges);
        Assert.Contains(new LinkEdge(RelationshipSubfolders.Children, PersonFolder), related.LinkEdges);
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void AddRelationship_DerivesChildAndReciprocalParentLinkEdges_WhenKindIsChild()
    {
        //Act
        _sut.AddRelationship(_personUid, _relatedUid, RelationshipKind.Child);

        //Assert
        var person = GetNode(_personUid);
        var related = GetNode(_relatedUid);
        Assert.Contains(_relatedUid, person.ChildIds);
        Assert.Contains(_personUid, related.ParentIds);
        Assert.Contains(new LinkEdge(RelationshipSubfolders.Children, RelatedFolder), person.LinkEdges);
        Assert.Contains(new LinkEdge(RelationshipSubfolders.Parents, PersonFolder), related.LinkEdges);
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void AddRelationship_DerivesSymmetricSpouseLinkEdges_WhenKindIsSpouse()
    {
        //Act
        _sut.AddRelationship(_personUid, _relatedUid, RelationshipKind.Spouse);

        //Assert
        var person = GetNode(_personUid);
        var related = GetNode(_relatedUid);
        Assert.Contains(_relatedUid, person.SpouseIds);
        Assert.Contains(_personUid, related.SpouseIds);
        Assert.Contains(new LinkEdge(RelationshipSubfolders.Spouses, RelatedFolder), person.LinkEdges);
        Assert.Contains(new LinkEdge(RelationshipSubfolders.Spouses, PersonFolder), related.LinkEdges);
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void AddRelationship_DerivesSymmetricSiblingLinkEdges_WhenKindIsSibling()
    {
        //Act
        _sut.AddRelationship(_personUid, _relatedUid, RelationshipKind.Sibling);

        //Assert
        var person = GetNode(_personUid);
        var related = GetNode(_relatedUid);
        Assert.Contains(_relatedUid, person.SiblingIds);
        Assert.Contains(_personUid, related.SiblingIds);
        Assert.Contains(new LinkEdge(RelationshipSubfolders.Siblings, RelatedFolder), person.LinkEdges);
        Assert.Contains(new LinkEdge(RelationshipSubfolders.Siblings, PersonFolder), related.LinkEdges);
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void AddRelationship_Throws_WhenPersonNotAdded()
    {
        //Arrange
        var unknownUid = Guid.NewGuid();

        //Act + Assert
        Assert.Throws<InvalidOperationException>(() => _sut.AddRelationship(unknownUid, _relatedUid, RelationshipKind.Spouse));
    }

    #endregion

    private PersonNode GetNode(Guid uid)
    {
        return _sut.Nodes.Single(n => n.UniqueIdentifier == uid);
    }
}
