using System;
using System.Globalization;
using TreeManager.App.Converters;
using TreeManager.App.ViewModels;
using TreeManager.Common.TestUtilities;

namespace TreeManager.App.L0.Converters;

public class ModeToLabelConverterTests
{
    private readonly ModeToLabelConverter _sut = new();

    #region Convert

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void Convert_ReturnsNowaOsoba_WhenModeIsAdd()
    {
        //Act
        var result = _sut.Convert(AppMode.Add, typeof(string), null, CultureInfo.InvariantCulture);

        //Assert
        Assert.Equal("Nowa osoba", result);
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void Convert_ReturnsEdycjaOsoby_WhenModeIsEditTree()
    {
        //Act
        var result = _sut.Convert(AppMode.EditTree, typeof(string), null, CultureInfo.InvariantCulture);

        //Assert
        Assert.Equal("Edycja osoby", result);
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void Convert_ReturnsEdycjaSzkicu_WhenModeIsEditDraft()
    {
        //Act
        var result = _sut.Convert(AppMode.EditDraft, typeof(string), null, CultureInfo.InvariantCulture);

        //Assert
        Assert.Equal("Edycja szkicu", result);
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void Convert_ReturnsEmpty_WhenValueIsNotAppMode()
    {
        //Act
        var result = _sut.Convert("invalid", typeof(string), null, CultureInfo.InvariantCulture);

        //Assert
        Assert.Equal(string.Empty, result);
    }

    #endregion

    #region ConvertBack

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void ConvertBack_Throws_Always()
    {
        Assert.Throws<NotSupportedException>(() =>
            _sut.ConvertBack("Nowa osoba", typeof(AppMode), null, CultureInfo.InvariantCulture));
    }

    #endregion
}
