using TreeManager.App.ViewModels;
using TreeManager.Common.TestUtilities;
using TreeManager.Core.Domain;

namespace TreeManager.App.L0.ViewModels;

public class PersonViewModelTests
{
    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void Sex_DefaultsToUnknown_WhenConstructed()
    {
        Assert.Equal(Sex.Unknown, new PersonViewModel().Sex);
    }
}
