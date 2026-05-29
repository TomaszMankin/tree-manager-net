using System;
using System.Collections.Generic;
using TreeManager.Common.TestUtilities;
using TreeManager.Core.Domain;
using TreeManager.Core.Domain.Relationships;
using TreeManager.Core.Services;

namespace TreeManager.Core.L0.Services;

public class RelationshipSyncServiceTests
{
    private static readonly Guid SourceId = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001");
    private const string SourceName = "Jan Kowalski";

    #region ApplyBidirectionalSync

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void ApplyBidirectionalSync_AddsSourceToChildrenList_WhenSourceIsChildOfTarget()
    {
        //Arrange
        var target = new MeFile();

        //Act
        var result = RelationshipSyncService.ApplyBidirectionalSync(target, SourceId, SourceName, RelationshipRole.IsChildOf);

        //Assert
        Assert.Contains(SourceId, result.ChildrenId);
        Assert.Contains(SourceName, result.Children);
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void ApplyBidirectionalSync_AddsSourceToParentsList_WhenSourceIsParentOfTarget()
    {
        //Arrange
        var target = new MeFile();

        //Act
        var result = RelationshipSyncService.ApplyBidirectionalSync(target, SourceId, SourceName, RelationshipRole.IsParentOf);

        //Assert
        Assert.Contains(SourceId, result.ParentsId);
        Assert.Contains(SourceName, result.Parents);
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void ApplyBidirectionalSync_AddsSourceToSpouseList_WhenSourceIsSpouseOfTarget()
    {
        //Arrange
        var target = new MeFile();

        //Act
        var result = RelationshipSyncService.ApplyBidirectionalSync(target, SourceId, SourceName, RelationshipRole.IsSpouseOf);

        //Assert
        Assert.Contains(SourceId, result.SpouseId);
        Assert.Contains(SourceName, result.Spouse);
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void ApplyBidirectionalSync_AddsSourceToSiblingsList_WhenSourceIsSiblingOfTarget()
    {
        //Arrange
        var target = new MeFile();

        //Act
        var result = RelationshipSyncService.ApplyBidirectionalSync(target, SourceId, SourceName, RelationshipRole.IsSiblingOf);

        //Assert
        Assert.Contains(SourceId, result.SiblingsId);
        Assert.Contains(SourceName, result.Siblings);
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void ApplyBidirectionalSync_IsIdempotent_WhenAppliedTwice()
    {
        //Arrange
        var target = new MeFile();

        //Act
        var once = RelationshipSyncService.ApplyBidirectionalSync(target, SourceId, SourceName, RelationshipRole.IsSpouseOf);
        var twice = RelationshipSyncService.ApplyBidirectionalSync(once, SourceId, SourceName, RelationshipRole.IsSpouseOf);

        //Assert
        Assert.Single(twice.SpouseId);
        Assert.Single(twice.Spouse);
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void ApplyBidirectionalSync_DoesNotOverwrite_WhenTargetAlreadyHasSourceInDifferentList()
    {
        //Arrange
        var target = new MeFile
        {
            ParentsId = new List<Guid> { SourceId },
            Parents = new List<string> { SourceName },
        };

        //Act
        var result = RelationshipSyncService.ApplyBidirectionalSync(target, SourceId, SourceName, RelationshipRole.IsSpouseOf);

        //Assert
        Assert.Same(target, result);
        Assert.Empty(result.SpouseId);
        Assert.Contains(SourceId, result.ParentsId);
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void ApplyBidirectionalSync_AppendsBothUuidAndName_WhenSyncApplied()
    {
        //Arrange
        var existingId = Guid.Parse("bbbbbbbb-0000-0000-0000-000000000002");
        var target = new MeFile
        {
            ChildrenId = new List<Guid> { existingId },
            Children = new List<string> { "Existing Person" },
        };

        //Act
        var result = RelationshipSyncService.ApplyBidirectionalSync(target, SourceId, SourceName, RelationshipRole.IsChildOf);

        //Assert
        Assert.Equal(2, result.ChildrenId.Count);
        Assert.Equal(2, result.Children.Count);
        Assert.Contains(SourceId, result.ChildrenId);
        Assert.Contains(SourceName, result.Children);
        Assert.Contains(existingId, result.ChildrenId);
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void ApplyBidirectionalSync_ReturnsTargetUnchanged_WhenAlreadyCorrect()
    {
        //Arrange
        var target = new MeFile
        {
            SpouseId = new List<Guid> { SourceId },
            Spouse = new List<string> { SourceName },
        };

        //Act
        var result = RelationshipSyncService.ApplyBidirectionalSync(target, SourceId, SourceName, RelationshipRole.IsSpouseOf);

        //Assert
        Assert.Same(target, result);
    }

    #endregion
}
