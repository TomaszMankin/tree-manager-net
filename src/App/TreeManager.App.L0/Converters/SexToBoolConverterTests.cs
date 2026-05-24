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
        //Act
        var result = _sut.Convert(Sex.Male, typeof(bool), Sex.Male, CultureInfo.InvariantCulture);

        //Assert
        Assert.Equal(true, result);
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void Convert_ReturnsFalse_WhenSexDiffersFromParameter()
    {
        //Arrange
        //Act
        var result = _sut.Convert(Sex.Female, typeof(bool), Sex.Male, CultureInfo.InvariantCulture);

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
        //Act
        var result = _sut.ConvertBack(true, typeof(Sex), Sex.Male, CultureInfo.InvariantCulture);

        //Assert
        Assert.Equal(Sex.Male, result);
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void ConvertBack_ReturnsBindingDoNothing_WhenIsCheckedFalse()
    {
        //Arrange
        //Act
        var result = _sut.ConvertBack(false, typeof(Sex), Sex.Male, CultureInfo.InvariantCulture);

        //Assert
        Assert.Equal(Binding.DoNothing, result);
    }

    #endregion
}
