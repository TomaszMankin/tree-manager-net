using System.Globalization;
using System.Windows;
using System.Windows.Media;
using TreeManager.App.Converters;
using TreeManager.App.L0.Fixtures;
using TreeManager.App.ViewModels;
using TreeManager.Common.TestUtilities;

namespace TreeManager.App.L0.Converters;

public class ModeToBackgroundBrushConverterTests : IClassFixture<ApplicationResourceFixture>
{
    private const string AddBrushKey = "AddModeBrush";
    private const string EditTreeBrushKey = "EditTreeModeBrush";
    private const string EditDraftBrushKey = "EditDraftModeBrush";

    private readonly ModeToBackgroundBrushConverter _sut;

    public ModeToBackgroundBrushConverterTests(ApplicationResourceFixture _)
    {
        _sut = new ModeToBackgroundBrushConverter();
    }

    #region Convert

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void Convert_ReturnsAddBrush_WhenModeIsAdd()
    {
        //Arrange
        var expected = Application.Current.Resources[AddBrushKey] as SolidColorBrush;

        //Act
        var result = _sut.Convert(AppMode.Add, typeof(Brush), null, CultureInfo.InvariantCulture);

        //Assert
        var brush = Assert.IsType<SolidColorBrush>(result);
        Assert.Equal(expected.Color, brush.Color);
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void Convert_ReturnsEditTreeBrush_WhenModeIsEditTree()
    {
        //Arrange
        var expected = Application.Current.Resources[EditTreeBrushKey] as SolidColorBrush;

        //Act
        var result = _sut.Convert(AppMode.EditTree, typeof(Brush), null, CultureInfo.InvariantCulture);

        //Assert
        var brush = Assert.IsType<SolidColorBrush>(result);
        Assert.Equal(expected.Color, brush.Color);
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void Convert_ReturnsEditDraftBrush_WhenModeIsEditDraft()
    {
        //Arrange
        var expected = Application.Current.Resources[EditDraftBrushKey] as SolidColorBrush;

        //Act
        var result = _sut.Convert(AppMode.EditDraft, typeof(Brush), null, CultureInfo.InvariantCulture);

        //Assert
        var brush = Assert.IsType<SolidColorBrush>(result);
        Assert.Equal(expected.Color, brush.Color);
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void Convert_ReturnsUnsetValue_WhenValueIsNotAppMode()
    {
        //Act
        var result = _sut.Convert("not-an-enum", typeof(Brush), null, CultureInfo.InvariantCulture);

        //Assert
        Assert.Equal(DependencyProperty.UnsetValue, result);
    }

    #endregion

    #region ConvertBack

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void ConvertBack_Throws_WhenInvoked()
    {
        Assert.Throws<NotSupportedException>(
            () => _sut.ConvertBack(null, typeof(AppMode), null, CultureInfo.InvariantCulture));
    }

    #endregion
}
