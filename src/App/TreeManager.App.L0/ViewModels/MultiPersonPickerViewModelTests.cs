using TreeManager.App.ViewModels;
using TreeManager.Common.TestUtilities;
using TreeManager.Core.Domain;

namespace TreeManager.App.L0.ViewModels;

public class MultiPersonPickerViewModelTests
{
    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void SearchText_FiltersCandidates_WhenSubstringMatches()
    {
        //Arrange
        var picker = new MultiPersonPickerViewModel();
        var alice = new PersonSummary(Guid.NewGuid(), "Alice Smith");
        var bob = new PersonSummary(Guid.NewGuid(), "Bob Jones");
        picker.LoadPeople(new[] { alice, bob });

        //Act
        picker.SearchText = "ali";

        //Assert
        Assert.Contains(alice, picker.Candidates);
        Assert.DoesNotContain(bob, picker.Candidates);
    }
}
