using System.Globalization;
using System.Windows;
using TreeManager.App.Converters;
using TreeManager.App.ViewModels;
using TreeManager.Common.TestUtilities;

namespace TreeManager.App.L0.Converters;

public class ModeToBackgroundBrushConverterTests
{
    private readonly ModeToBackgroundBrushConverter _sut = new();

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void Convert_ReturnsUnsetValue_WhenValueIsNotAppMode()
    {
        //Act
        var result = _sut.Convert("not-an-enum", typeof(object), null, CultureInfo.InvariantCulture);

        //Assert
        Assert.Equal(DependencyProperty.UnsetValue, result);
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void ConvertBack_Throws_WhenInvoked()
    {
        Assert.Throws<NotSupportedException>(
            () => _sut.ConvertBack(null, typeof(AppMode), null, CultureInfo.InvariantCulture));
    }
}
