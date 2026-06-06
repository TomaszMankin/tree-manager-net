using TreeManager.App.ViewModels;
using TreeManager.Common.TestUtilities;

namespace TreeManager.App.L0.ViewModels;

public class StringListViewModelTests
{
    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void Add_AddsItem_WhenInputIsNonEmpty()
    {
        //Arrange
        var vm = new StringListViewModel();
        vm.Input = "Jan";

        //Act
        vm.AddCommand.Execute(null);

        //Assert
        Assert.Contains("Jan", vm.Items);
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void Add_ClearsInput_AfterAdding()
    {
        //Arrange
        var vm = new StringListViewModel();
        vm.Input = "Jan";

        //Act
        vm.AddCommand.Execute(null);

        //Assert
        Assert.Equal(string.Empty, vm.Input);
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void Add_DoesNotAdd_WhenInputIsWhitespace()
    {
        //Arrange
        var vm = new StringListViewModel();
        vm.Input = "   ";

        //Act
        vm.AddCommand.Execute(null);

        //Assert
        Assert.Empty(vm.Items);
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void Remove_RemovesItem_WhenItemExists()
    {
        //Arrange
        var vm = new StringListViewModel();
        vm.Items.Add("Jan");
        vm.Items.Add("Marek");

        //Act
        vm.RemoveCommand.Execute("Jan");

        //Assert
        Assert.DoesNotContain("Jan", vm.Items);
        Assert.Contains("Marek", vm.Items);
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void Serialize_ReturnsNewlineSeparated_WhenMultipleItems()
    {
        //Arrange
        var vm = new StringListViewModel();
        vm.Items.Add("Jan");
        vm.Items.Add("Marek");

        //Act
        var result = vm.Serialize();

        //Assert
        Assert.Equal("Jan\nMarek", result);
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void Load_PopulatesItems_WhenSerializedStringProvided()
    {
        //Arrange
        var vm = new StringListViewModel();

        //Act
        vm.Load("Jan\nMarek");

        //Assert
        Assert.Equal(["Jan", "Marek"], vm.Items);
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void Load_ClearsExistingItems_BeforeLoading()
    {
        //Arrange
        var vm = new StringListViewModel();
        vm.Items.Add("OldValue");

        //Act
        vm.Load("NewValue");

        //Assert
        Assert.Equal(["NewValue"], vm.Items);
        Assert.DoesNotContain("OldValue", vm.Items);
    }
}
