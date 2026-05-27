using TreeManager.App.Mappers;
using TreeManager.App.ViewModels;
using TreeManager.Common.TestUtilities;
using TreeManager.Core.Domain;

namespace TreeManager.App.L0.Mappers;

public class DatesTabViewModelMapperTests
{
    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void ToDatesTabViewModel_MapsFullBirthDate_WhenDatesOfBirthIsFullDate()
    {
        //Arrange
        var meFile = new MeFile { DatesOfBirth = "12|03|1947" };

        //Act
        var vm = meFile.ToDatesTabViewModel();

        //Assert
        Assert.Equal(12, vm.BirthDate.Day);
        Assert.Equal(3, vm.BirthDate.Month);
        Assert.Equal(1947, vm.BirthDate.Year);
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void ToDatesTabViewModel_MapsPartialBirthDate_WhenDayIsUnknown()
    {
        //Arrange
        var meFile = new MeFile { DatesOfBirth = "XX|03|1947" };

        //Act
        var vm = meFile.ToDatesTabViewModel();

        //Assert
        Assert.Null(vm.BirthDate.Day);
        Assert.Equal(3, vm.BirthDate.Month);
        Assert.Equal(1947, vm.BirthDate.Year);
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void ToDatesTabViewModel_SetsIsDeceased_WhenDatesOfDeathIsNonEmpty()
    {
        //Arrange
        var meFile = new MeFile { DatesOfDeath = "XX|XX|XXXX" };

        //Act
        var vm = meFile.ToDatesTabViewModel();

        //Assert
        Assert.True(vm.IsDeceased);
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void ToDatesTabViewModel_DoesNotSetIsDeceased_WhenDatesOfDeathIsEmpty()
    {
        //Arrange
        var meFile = new MeFile { DatesOfDeath = string.Empty };

        //Act
        var vm = meFile.ToDatesTabViewModel();

        //Assert
        Assert.False(vm.IsDeceased);
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void WithDatesFrom_SerializesFullBirthDate_WhenAllComponentsKnown()
    {
        //Arrange
        var vm = new DatesTabViewModel();
        vm.BirthDate.Day = 12;
        vm.BirthDate.Month = 3;
        vm.BirthDate.Year = 1947;
        var meFile = new MeFile();

        //Act
        var result = meFile.WithDatesFrom(vm);

        //Assert
        Assert.Equal("12|03|1947", result.DatesOfBirth);
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void WithDatesFrom_SerializesEmptyDatesOfDeath_WhenIsDeceasedIsFalse()
    {
        //Arrange
        var vm = new DatesTabViewModel { IsDeceased = false };
        var meFile = new MeFile();

        //Act
        var result = meFile.WithDatesFrom(vm);

        //Assert
        Assert.Equal(string.Empty, result.DatesOfDeath);
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void WithDatesFrom_SerializesDeathDate_WhenIsDeceasedIsTrue()
    {
        //Arrange
        var vm = new DatesTabViewModel();
        vm.IsDeceased = true;
        vm.DeathDate.Day = null;
        vm.DeathDate.Month = null;
        vm.DeathDate.Year = null;
        var meFile = new MeFile();

        //Act
        var result = meFile.WithDatesFrom(vm);

        //Assert
        Assert.Equal("XX|XX|XXXX", result.DatesOfDeath);
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void WithDatesFrom_ThrowsArgumentNullException_WhenBaseFileIsNull()
    {
        //Arrange
        var vm = new DatesTabViewModel();

        //Act
        var ex = Record.Exception(() => ((MeFile)null).WithDatesFrom(vm));

        //Assert
        Assert.IsType<ArgumentNullException>(ex);
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void ToDatesTabViewModel_ThrowsArgumentNullException_WhenMeFileIsNull()
    {
        //Arrange
        //Act
        var ex = Record.Exception(() => ((MeFile)null).ToDatesTabViewModel());

        //Assert
        Assert.IsType<ArgumentNullException>(ex);
    }
}
