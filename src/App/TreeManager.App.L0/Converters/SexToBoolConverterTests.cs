using System.Globalization;
using System.Windows.Data;
using TreeManager.App.Converters;
using TreeManager.Common.TestUtilities;
using TreeManager.Core.Domain;

namespace TreeManager.App.L0.Converters;

public class SexToBoolConverterTests
{
    private readonly SexToBoolConverter _sut;

    public SexToBoolConverterTests()
    {
        _sut = new SexToBoolConverter();
    }

    #region Convert

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void Convert_ReturnsTrue_WhenSexMatchesParameter()
    {
        //Arrange
        var sexValue = Sex.Male;
        var converterParam = Sex.Male;

        //Act
        var result = _sut.Convert(sexValue, typeof(bool), converterParam, CultureInfo.InvariantCulture);

        //Assert
        Assert.Equal(true, result);
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void Convert_ReturnsFalse_WhenSexDiffersFromParameter()
    {
        //Arrange
        var sexValue = Sex.Female;
        var converterParam = Sex.Male;

        //Act
        var result = _sut.Convert(sexValue, typeof(bool), converterParam, CultureInfo.InvariantCulture);

        //Assert
        Assert.Equal(false, result);
    }

    #endregion

    #region ConvertBack

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void ConvertBack_ReturnsSex_WhenIsCheckedTrue()
    {
        //Arrange
        var isChecked = true;
        var converterParam = Sex.Male;

        //Act
        var result = _sut.ConvertBack(isChecked, typeof(Sex), converterParam, CultureInfo.InvariantCulture);

        //Assert
        Assert.Equal(Sex.Male, result);
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void ConvertBack_ReturnsBindingDoNothing_WhenIsCheckedFalse()
    {
        //Arrange
        var isChecked = false;
        var converterParam = Sex.Male;

        //Act
        var result = _sut.ConvertBack(isChecked, typeof(Sex), converterParam, CultureInfo.InvariantCulture);

        //Assert
        Assert.Equal(Binding.DoNothing, result);
    }

    #endregion
}
