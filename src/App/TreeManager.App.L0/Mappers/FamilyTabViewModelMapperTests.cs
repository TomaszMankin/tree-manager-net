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

    #region ToFamilyTabViewModel

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void ToFamilyTabViewModel_PopulatesParentsSelected_WhenMeFileHasParents()
    {
        //Arrange
        var parentId = Guid.NewGuid();
        var meFile = new MeFile
        {
            ParentsId = new List<Guid> { parentId },
            Parents = new List<string> { "Parent One" },
        };

        //Act
        var result = meFile.ToFamilyTabViewModel();

        //Assert
        Assert.Single(result.Parents.Selected);
        Assert.Equal(parentId, result.Parents.Selected[0].UniqueIdentifier);
        Assert.Equal("Parent One", result.Parents.Selected[0].DisplayName);
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void ToFamilyTabViewModel_PopulatesChildrenSelected_WhenMeFileHasChildren()
    {
        //Arrange
        var childId = Guid.NewGuid();
        var meFile = new MeFile
        {
            ChildrenId = new List<Guid> { childId },
            Children = new List<string> { "Child One" },
        };

        //Act
        var result = meFile.ToFamilyTabViewModel();

        //Assert
        Assert.Single(result.Children.Selected);
        Assert.Equal(childId, result.Children.Selected[0].UniqueIdentifier);
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void ToFamilyTabViewModel_PopulatesSpousesSelected_WhenMeFileHasSpouses()
    {
        //Arrange
        var spouseId = Guid.NewGuid();
        var meFile = new MeFile
        {
            SpouseId = new List<Guid> { spouseId },
            Spouse = new List<string> { "Spouse One" },
        };

        //Act
        var result = meFile.ToFamilyTabViewModel();

        //Assert
        Assert.Single(result.Spouses.Selected);
        Assert.Equal(spouseId, result.Spouses.Selected[0].UniqueIdentifier);
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void ToFamilyTabViewModel_PopulatesSiblingsSelected_WhenMeFileHasSiblings()
    {
        //Arrange
        var siblingId = Guid.NewGuid();
        var meFile = new MeFile
        {
            SiblingsId = new List<Guid> { siblingId },
            Siblings = new List<string> { "Sibling One" },
        };

        //Act
        var result = meFile.ToFamilyTabViewModel();

        //Assert
        Assert.Single(result.Siblings.Selected);
        Assert.Equal(siblingId, result.Siblings.Selected[0].UniqueIdentifier);
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void ToFamilyTabViewModel_SetsLoadedPersonId_WhenMeFileHasUniqueIdentifier()
    {
        //Arrange
        var personId = Guid.NewGuid();
        var meFile = new MeFile { UniqueIdentifier = personId };

        //Act
        var result = meFile.ToFamilyTabViewModel();

        //Assert
        Assert.Equal(personId, result.LoadedPersonId);
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void ToFamilyTabViewModel_ProducesEmptySelections_WhenMeFileHasNoRelationships()
    {
        //Arrange
        var meFile = new MeFile();

        //Act
        var result = meFile.ToFamilyTabViewModel();

        //Assert
        Assert.Empty(result.Parents.Selected);
        Assert.Empty(result.Children.Selected);
        Assert.Empty(result.Spouses.Selected);
        Assert.Empty(result.Siblings.Selected);
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void ToFamilyTabViewModel_Throws_WhenSourceIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => ((MeFile)null).ToFamilyTabViewModel());
    }

    #endregion
}
