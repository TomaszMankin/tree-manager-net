using System.ComponentModel.DataAnnotations;
using System.Linq;
using TreeManager.App.ViewModels;
using TreeManager.Common.TestUtilities;
using TreeManager.Core.Domain;

namespace TreeManager.App.L0.ViewModels;

public class PersonViewModelTests
{
    private readonly PersonViewModel _sut;

    public PersonViewModelTests()
    {
        _sut = new PersonViewModel();
    }

    #region Validation — FirstName

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void FirstName_HasNoError_WhenPopulated()
    {
        //Arrange
        _sut.FirstName = "Jan";

        //Act
        _sut.ValidateAll();

        //Assert
        Assert.Empty(_sut.GetErrors(nameof(PersonViewModel.FirstName)).Cast<ValidationResult>());
    }

    #endregion

    #region Validation — LastName

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void LastName_HasNoError_WhenPopulated()
    {
        //Arrange
        _sut.LastName = "Kowalski";

        //Act
        _sut.ValidateAll();

        //Assert
        Assert.Empty(_sut.GetErrors(nameof(PersonViewModel.LastName)).Cast<ValidationResult>());
    }

    #endregion

    #region Validation — Sex

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void Sex_HasError_WhenUnknown()
    {
        //Arrange
        _sut.Sex = Sex.Unknown;

        //Act
        _sut.ValidateAll();

        //Assert
        Assert.True(_sut.HasErrors);
        Assert.NotEmpty(_sut.GetErrors(nameof(PersonViewModel.Sex)).Cast<ValidationResult>());
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void Sex_HasNoError_WhenMale()
    {
        //Arrange
        _sut.Sex = Sex.Male;

        //Act
        _sut.ValidateAll();

        //Assert
        Assert.Empty(_sut.GetErrors(nameof(PersonViewModel.Sex)).Cast<ValidationResult>());
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void Sex_HasNoError_WhenFemale()
    {
        //Arrange
        _sut.Sex = Sex.Female;

        //Act
        _sut.ValidateAll();

        //Assert
        Assert.Empty(_sut.GetErrors(nameof(PersonViewModel.Sex)).Cast<ValidationResult>());
    }

    #endregion

    #region ValidateAll

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void ValidateAll_ReportsError_WhenSexIsUnknown()
    {
        //Arrange
        _sut.FirstName = string.Empty;
        _sut.LastName = string.Empty;
        _sut.Sex = Sex.Unknown;

        //Act
        _sut.ValidateAll();

        //Assert
        Assert.True(_sut.HasErrors);
        Assert.Empty(_sut.GetErrors(nameof(PersonViewModel.FirstName)).Cast<ValidationResult>());
        Assert.Empty(_sut.GetErrors(nameof(PersonViewModel.LastName)).Cast<ValidationResult>());
        Assert.NotEmpty(_sut.GetErrors(nameof(PersonViewModel.Sex)).Cast<ValidationResult>());
    }

    #endregion

    #region HasErrors

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void HasErrors_IsFalse_WhenJustConstructed()
    {
        Assert.False(_sut.HasErrors);
    }

    #endregion
}
