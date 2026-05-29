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

    private static PersonSummary MakePerson(string name) =>
        new(Guid.NewGuid(), name);
}
