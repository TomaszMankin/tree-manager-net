using System;
using TreeManager.Common.TestUtilities;
using TreeManager.Core.Domain;
using TreeManager.Core.Domain.Relationships;

namespace TreeManager.Core.L0.Domain;

public class PersonExtensionsTests
{
    private static readonly Guid TestId = Guid.Parse("11111111-0000-0000-0000-000000000001");

    #region AsSpouse

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void AsSpouse_ReturnsSpouseRelationshipWithMatchingId_Always()
    {
        //Arrange + Act
        var result = TestId.AsSpouse();

        //Assert
        Assert.Equal(TestId, result.SpouseId);
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void AsSpouse_DefaultsToSpouseTypeAndIsCurrent_Always()
    {
        //Arrange + Act
        var result = TestId.AsSpouse();

        //Assert
        Assert.Equal(SpousalRelationshipType.Spouse, result.Type);
        Assert.True(result.IsCurrent);
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void AsSpouse_RespectsExplicitTypeAndCurrentFlag_Always()
    {
        //Arrange + Act
        var result = TestId.AsSpouse(SpousalRelationshipType.ExSpouse, isCurrent: false);

        //Assert
        Assert.Equal(SpousalRelationshipType.ExSpouse, result.Type);
        Assert.False(result.IsCurrent);
    }

    #endregion

    #region AsParent

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void AsParent_ReturnsParentalRelationshipWithMatchingId_Always()
    {
        //Arrange + Act
        var result = TestId.AsParent();

        //Assert
        Assert.Equal(TestId, result.ParentId);
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void AsParent_DefaultsToParentType_Always()
    {
        //Arrange + Act
        var result = TestId.AsParent();

        //Assert
        Assert.Equal(ParentalRelationshipType.Parent, result.Type);
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void AsParent_RespectsExplicitType_WhenStepParent()
    {
        //Arrange + Act
        var result = TestId.AsParent(ParentalRelationshipType.StepParent);

        //Assert
        Assert.Equal(ParentalRelationshipType.StepParent, result.Type);
    }

    #endregion

    #region AsChild

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void AsChild_ReturnsChildRelationshipWithMatchingId_Always()
    {
        //Arrange + Act
        var result = TestId.AsChild();

        //Assert
        Assert.Equal(TestId, result.ChildId);
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void AsChild_DefaultsToChildType_Always()
    {
        //Arrange + Act
        var result = TestId.AsChild();

        //Assert
        Assert.Equal(ChildRelationshipType.Child, result.Type);
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void AsChild_RespectsExplicitType_WhenAdoptiveChild()
    {
        //Arrange + Act
        var result = TestId.AsChild(ChildRelationshipType.AdoptiveChild);

        //Assert
        Assert.Equal(ChildRelationshipType.AdoptiveChild, result.Type);
    }

    #endregion

    #region AsSibling

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void AsSibling_ReturnsSiblingRelationshipWithMatchingId_Always()
    {
        //Arrange + Act
        var result = TestId.AsSibling();

        //Assert
        Assert.Equal(TestId, result.SiblingId);
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void AsSibling_DefaultsToSiblingType_Always()
    {
        //Arrange + Act
        var result = TestId.AsSibling();

        //Assert
        Assert.Equal(SiblingRelationshipType.Sibling, result.Type);
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void AsSibling_RespectsExplicitType_WhenHalfSibling()
    {
        //Arrange + Act
        var result = TestId.AsSibling(SiblingRelationshipType.HalfSibling);

        //Assert
        Assert.Equal(SiblingRelationshipType.HalfSibling, result.Type);
    }

    #endregion
}
