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
    public void FirstName_HasError_WhenEmpty()
    {
        //Arrange
        _sut.FirstName = string.Empty;

        //Act
        _sut.ValidateAll();

        //Assert
        Assert.True(_sut.HasErrors);
        Assert.NotEmpty(_sut.GetErrors(nameof(PersonViewModel.FirstName)).Cast<ValidationResult>());
    }

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
    public void LastName_HasError_WhenEmpty()
    {
        //Arrange
        _sut.LastName = string.Empty;

        //Act
        _sut.ValidateAll();

        //Assert
        Assert.True(_sut.HasErrors);
        Assert.NotEmpty(_sut.GetErrors(nameof(PersonViewModel.LastName)).Cast<ValidationResult>());
    }

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
        //Arrange — Sex is Unknown by default

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

    #region Construction

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void Constructor_InitializesDefaults_WhenInstantiated()
    {
        //Act + Assert (AAA omitted per code standards — pure assertion block)
        Assert.Equal(string.Empty, _sut.FirstName);
        Assert.Equal(string.Empty, _sut.LastName);
        Assert.Equal(Sex.Unknown, _sut.Sex);
        Assert.False(_sut.HasMaidenName);
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void HasErrors_IsFalse_WhenJustConstructed()
    {
        //Act + Assert (one-liner; AAA omitted per code standards)
        Assert.False(_sut.HasErrors);
    }

    #endregion

    #region ValidateAll

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void ValidateAll_ReportsAllErrors_WhenAllRequiredFieldsEmpty()
    {
        //Arrange — fresh VM; FirstName="", LastName="", Sex=Unknown

        //Act
        _sut.ValidateAll();

        //Assert
        Assert.True(_sut.HasErrors);
        Assert.NotEmpty(_sut.GetErrors(nameof(PersonViewModel.FirstName)).Cast<ValidationResult>());
        Assert.NotEmpty(_sut.GetErrors(nameof(PersonViewModel.LastName)).Cast<ValidationResult>());
        Assert.NotEmpty(_sut.GetErrors(nameof(PersonViewModel.Sex)).Cast<ValidationResult>());
    }

    #endregion
}
