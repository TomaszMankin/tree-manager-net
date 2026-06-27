using TreeManager.App.L3.Harness;
using TreeManager.Common.TestUtilities;

namespace TreeManager.App.L3;

public sealed class SmokeTests
{
    #region AppSession

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L3)]
    public void AppSession_LaunchesAndShowsMainWindow_Always()
    {
        //Arrange
        using var session = new AppSession();

        //Act
        var mainWindow = session.FindById("Window_Main");

        //Assert
        Assert.NotNull(mainWindow);
    }

    #endregion
}
