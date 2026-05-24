using Moq;
using TreeManager.App.Services;
using TreeManager.App.Startup;
using TreeManager.Common.TestUtilities;
using TreeManager.Core.Abstractions.IO;
using TreeManager.Core.Abstractions.Settings;

namespace TreeManager.App.L0.Startup;

public class StartupBootstrapperTests
{
    private const string ValidRoot = @"C:\fake\family-tree";
    private const string PickedRoot = @"C:\fake\new-tree";

    private readonly Mock<IRootPointerStore> _store = new();
    private readonly Mock<IRootPickerService> _picker = new();
    private readonly Mock<IFileSystemFacade> _fs = new();
    private readonly StartupBootstrapper _sut;

    public StartupBootstrapperTests()
    {
        _sut = new StartupBootstrapper(_store.Object, _picker.Object, _fs.Object);
    }

    #region Resolve

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void Resolve_ReturnsExisting_WhenPointerHasValidPath()
    {
        //Arrange
        _store.Setup(x => x.Read()).Returns(ValidRoot);
        _fs.Setup(x => x.DirectoryExists(ValidRoot)).Returns(true);

        //Act
        var result = _sut.Resolve();

        //Assert
        Assert.False(result.ShouldShutdown);
        Assert.Equal(ValidRoot, result.RootPath);
        _picker.Verify(x => x.PickRoot(), Times.Never);
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void Resolve_PromptsPicker_WhenPointerEmpty()
    {
        //Arrange
        _store.Setup(x => x.Read()).Returns(string.Empty);
        _picker.Setup(x => x.PickRoot()).Returns(PickedRoot);

        //Act
        var result = _sut.Resolve();

        //Assert
        Assert.False(result.ShouldShutdown);
        Assert.Equal(PickedRoot, result.RootPath);
        _store.Verify(x => x.Write(PickedRoot), Times.Once);
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void Resolve_PromptsPicker_WhenPointerPathDoesNotExist()
    {
        //Arrange
        _store.Setup(x => x.Read()).Returns(ValidRoot);
        _fs.Setup(x => x.DirectoryExists(ValidRoot)).Returns(false);
        _picker.Setup(x => x.PickRoot()).Returns(PickedRoot);

        //Act
        var result = _sut.Resolve();

        //Assert
        Assert.False(result.ShouldShutdown);
        Assert.Equal(PickedRoot, result.RootPath);
        _store.Verify(x => x.Write(PickedRoot), Times.Once);
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void Resolve_PersistsPickedPath_WhenUserSelectsValidFolder()
    {
        //Arrange
        _store.Setup(x => x.Read()).Returns(string.Empty);
        _picker.Setup(x => x.PickRoot()).Returns(PickedRoot);

        //Act
        _sut.Resolve();

        //Assert
        _store.Verify(x => x.Write(PickedRoot), Times.Once);
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void Resolve_ReturnsShutdown_WhenUserCancelsPicker()
    {
        //Arrange
        _store.Setup(x => x.Read()).Returns(string.Empty);
        _picker.Setup(x => x.PickRoot()).Returns(string.Empty);

        //Act
        var result = _sut.Resolve();

        //Assert
        Assert.True(result.ShouldShutdown);
        Assert.Equal(string.Empty, result.RootPath);
        _store.Verify(x => x.Write(It.IsAny<string>()), Times.Never);
    }

    #endregion
}
