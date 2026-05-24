using TreeManager.Common.TestUtilities;
using Xunit;

namespace TreeManager.Core.L0;

public class SmokeTests
{
    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void Smoke_BuildsAndRuns_True()
    {
        Assert.True(true);
    }
}
