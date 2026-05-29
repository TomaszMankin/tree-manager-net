using System;
using System.Collections.Generic;
using System.Linq;
using TreeManager.App.Mappers;
using TreeManager.App.ViewModels;
using TreeManager.Common.TestUtilities;
using TreeManager.Core.Domain;

namespace TreeManager.App.L0.Mappers;

public class FamilyTabViewModelMapperTests
{
    private static PersonSummary MakeSummary(string name) =>
        new PersonSummary(Guid.NewGuid(), name);

    #region ToMeFile

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void ToMeFile_MapsSelectedParents_WhenParentsSelected()
    {
        //Arrange
        var parent = MakeSummary("Parent One");
        var vm = new FamilyTabViewModel();
        vm.Parents.AddCommand.Execute(parent);

        //Act
        var result = vm.ToMeFile();

        //Assert
        Assert.Contains(parent.UniqueIdentifier, result.ParentsId);
        Assert.Contains(parent.DisplayName, result.Parents);
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void ToMeFile_MapsSelectedChildren_WhenChildrenSelected()
    {
        //Arrange
        var child = MakeSummary("Child One");
        var vm = new FamilyTabViewModel();
        vm.Children.AddCommand.Execute(child);

        //Act
        var result = vm.ToMeFile();

        //Assert
        Assert.Contains(child.UniqueIdentifier, result.ChildrenId);
        Assert.Contains(child.DisplayName, result.Children);
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void ToMeFile_MapsSelectedSpouses_WhenSpousesSelected()
    {
        //Arrange
        var spouse = MakeSummary("Spouse One");
        var vm = new FamilyTabViewModel();
        vm.Spouses.AddCommand.Execute(spouse);

        //Act
        var result = vm.ToMeFile();

        //Assert
        Assert.Contains(spouse.UniqueIdentifier, result.SpouseId);
        Assert.Contains(spouse.DisplayName, result.Spouse);
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void ToMeFile_MapsSelectedSiblings_WhenSiblingsSelected()
    {
        //Arrange
        var sibling = MakeSummary("Sibling One");
        var vm = new FamilyTabViewModel();
        vm.Siblings.AddCommand.Execute(sibling);

        //Act
        var result = vm.ToMeFile();

        //Assert
        Assert.Contains(sibling.UniqueIdentifier, result.SiblingsId);
        Assert.Contains(sibling.DisplayName, result.Siblings);
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void ToMeFile_PreservesNonRelationshipFields_WhenExistingProvided()
    {
        //Arrange
        var existing = new MeFile
        {
            PersonName = "Jan Testowy",
            Notes = "some notes",
            FirstName = "Jan",
        };
        var vm = new FamilyTabViewModel();

        //Act
        var result = vm.ToMeFile(existing);

        //Assert
        Assert.Equal("Jan Testowy", result.PersonName);
        Assert.Equal("some notes", result.Notes);
        Assert.Equal("Jan", result.FirstName);
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void ToMeFile_ProducesEmptyLists_WhenNoSelectionsAndNoExisting()
    {
        //Arrange
        var vm = new FamilyTabViewModel();

        //Act
        var result = vm.ToMeFile();

        //Assert
        Assert.Empty(result.ParentsId);
        Assert.Empty(result.Parents);
        Assert.Empty(result.ChildrenId);
        Assert.Empty(result.Children);
        Assert.Empty(result.SpouseId);
        Assert.Empty(result.Spouse);
        Assert.Empty(result.SiblingsId);
        Assert.Empty(result.Siblings);
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void ToMeFile_ThrowsArgumentNullException_WhenViewModelIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => ((FamilyTabViewModel)null).ToMeFile());
    }

    #endregion
}
