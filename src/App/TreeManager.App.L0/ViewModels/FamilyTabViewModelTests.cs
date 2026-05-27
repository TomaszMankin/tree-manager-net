using TreeManager.App.ViewModels;
using TreeManager.Common.TestUtilities;
using TreeManager.Core.Domain;

namespace TreeManager.App.L0.ViewModels;

public class FamilyTabViewModelTests
{
    private static PersonSummary MakePerson(string name) =>
        new(Guid.NewGuid(), name);

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void Add_ToParents_RemovesFromChildrenSpousesSiblingsCandidates()
    {
        var vm = new FamilyTabViewModel();
        var alice = MakePerson("Alice");
        vm.LoadPeople(new[] { alice });

        vm.Parents.AddCommand.Execute(alice);

        Assert.DoesNotContain(alice, vm.Children.Candidates);
        Assert.DoesNotContain(alice, vm.Spouses.Candidates);
        Assert.DoesNotContain(alice, vm.Siblings.Candidates);
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void Remove_FromParents_RestoresPersonToOtherCandidates()
    {
        var vm = new FamilyTabViewModel();
        var alice = MakePerson("Alice");
        vm.LoadPeople(new[] { alice });
        vm.Parents.AddCommand.Execute(alice);

        vm.Parents.RemoveCommand.Execute(alice);

        Assert.Contains(alice, vm.Children.Candidates);
        Assert.Contains(alice, vm.Spouses.Candidates);
        Assert.Contains(alice, vm.Siblings.Candidates);
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void SearchText_NarrowsCandidates_BySubstring()
    {
        var picker = new MultiPersonPickerViewModel();
        var alice = new PersonSummary(Guid.NewGuid(), "Alice Smith");
        var bob = new PersonSummary(Guid.NewGuid(), "Bob Jones");
        picker.LoadPeople(new[] { alice, bob });

        picker.SearchText = "ali";

        Assert.Contains(alice, picker.Candidates);
        Assert.DoesNotContain(bob, picker.Candidates);
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void LoadedPersonId_ExcludesLoadedPersonFromAllPickers()
    {
        var vm = new FamilyTabViewModel();
        var selfId = Guid.NewGuid();
        var self = new PersonSummary(selfId, "Self");
        var other = MakePerson("Other");
        vm.LoadPeople(new[] { self, other });

        vm.LoadedPersonId = selfId;

        Assert.DoesNotContain(self, vm.Parents.Candidates);
        Assert.DoesNotContain(self, vm.Children.Candidates);
        Assert.DoesNotContain(self, vm.Spouses.Candidates);
        Assert.DoesNotContain(self, vm.Siblings.Candidates);
    }
}
