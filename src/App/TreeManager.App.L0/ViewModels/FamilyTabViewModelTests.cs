using System.Collections.Generic;
using TreeManager.App.Mappers;
using TreeManager.App.ViewModels;
using TreeManager.Common.TestUtilities;
using TreeManager.Core.Domain;

namespace TreeManager.App.L0.ViewModels;

public class FamilyTabViewModelTests
{
    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void Add_RemovesPersonFromOtherCandidates_WhenAddedToParents()
    {
        //Arrange
        var vm = new FamilyTabViewModel();
        var alice = MakePerson("Alice");
        vm.LoadPeople(new[] { alice });

        //Act
        vm.Parents.AddCommand.Execute(alice);

        //Assert
        Assert.DoesNotContain(alice, vm.Children.Candidates);
        Assert.DoesNotContain(alice, vm.Spouses.Candidates);
        Assert.DoesNotContain(alice, vm.Siblings.Candidates);
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void Remove_RestoresPersonToOtherCandidates_WhenRemovedFromParents()
    {
        //Arrange
        var vm = new FamilyTabViewModel();
        var alice = MakePerson("Alice");
        vm.LoadPeople(new[] { alice });
        vm.Parents.AddCommand.Execute(alice);

        //Act
        vm.Parents.RemoveCommand.Execute(alice);

        //Assert
        Assert.Contains(alice, vm.Children.Candidates);
        Assert.Contains(alice, vm.Spouses.Candidates);
        Assert.Contains(alice, vm.Siblings.Candidates);
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void LoadedPersonId_ExcludesPersonFromAllPickers_WhenAssigned()
    {
        //Arrange
        var vm = new FamilyTabViewModel();
        var selfId = Guid.NewGuid();
        var self = new PersonSummary(selfId, "Self");
        var other = MakePerson("Other");
        vm.LoadPeople(new[] { self, other });

        //Act
        vm.LoadedPersonId = selfId;

        //Assert
        Assert.DoesNotContain(self, vm.Parents.Candidates);
        Assert.DoesNotContain(self, vm.Children.Candidates);
        Assert.DoesNotContain(self, vm.Spouses.Candidates);
        Assert.DoesNotContain(self, vm.Siblings.Candidates);
    }

    #region Reset

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void Reset_ClearsAllSelectionsAndRepopulates_WhenCalledWithNewPerson()
    {
        //Arrange
        var vm = new FamilyTabViewModel();
        var alice = MakePerson("Alice");
        vm.Parents.Selected.Add(alice);

        var parentId = Guid.NewGuid();
        var bob = new PersonSummary(parentId, "Bob");
        var meFile = new MeFile
        {
            ParentsId = new List<Guid> { parentId },
            Parents = new List<string> { "Bob" },
        };
        var allPeople = new List<PersonSummary> { alice, bob };

        //Act
        vm.Reset(meFile, allPeople);

        //Assert — Alice cleared; Bob from MeFile is now in Parents.Selected
        Assert.DoesNotContain(alice, vm.Parents.Selected);
        Assert.Single(vm.Parents.Selected);
        Assert.Equal(parentId, vm.Parents.Selected[0].UniqueIdentifier);
        // Bob in Candidates of other pickers is excluded due to cross-exclusion (W-001 mitigation)
        Assert.DoesNotContain(bob, vm.Children.Candidates);
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void Reset_SetsLoadedPersonId_WhenCalled()
    {
        //Arrange
        var vm = new FamilyTabViewModel();
        var personId = Guid.NewGuid();
        var meFile = new MeFile { UniqueIdentifier = personId };

        //Act
        vm.Reset(meFile, new List<PersonSummary>());

        //Assert
        Assert.Equal(personId, vm.LoadedPersonId);
    }

    #endregion

    private static PersonSummary MakePerson(string name) =>
        new(Guid.NewGuid(), name);
}
