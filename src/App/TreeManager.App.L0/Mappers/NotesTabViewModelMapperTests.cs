using System;
using TreeManager.App.Mappers;
using TreeManager.App.ViewModels;
using TreeManager.Common.TestUtilities;
using TreeManager.Core.Domain;

namespace TreeManager.App.L0.Mappers;

public class NotesTabViewModelMapperTests
{
    #region ToNotesTabViewModel

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void ToNotesTabViewModel_SetsNotes_WhenMeFileHasNotes()
    {
        //Arrange
        var meFile = new MeFile { Notes = "Ważna notatka o osobie" };

        //Act
        var vm = meFile.ToNotesTabViewModel();

        //Assert
        Assert.Equal("Ważna notatka o osobie", vm.Notes);
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void ToNotesTabViewModel_SetsEmptyNotes_WhenMeFileNotesEmpty()
    {
        //Arrange
        var meFile = new MeFile { Notes = string.Empty };

        //Act
        var vm = meFile.ToNotesTabViewModel();

        //Assert
        Assert.Equal(string.Empty, vm.Notes);
    }

    #endregion

    #region ToMeFile

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void ToMeFile_SetsNotes_WhenViewModelHasNotes()
    {
        //Arrange
        var vm = new NotesTabViewModel { Notes = "Notatka testowa" };

        //Act
        var result = vm.ToMeFile();

        //Assert
        Assert.Equal("Notatka testowa", result.Notes);
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void ToMeFile_PreservesOtherFields_WhenMappingNotes()
    {
        //Arrange
        var existing = new MeFile
        {
            FirstName = "Jan",
            LastName = "Kowalski",
            Notes = "stara notatka",
            DatesOfBirth = "1|1|1980",
        };
        var vm = new NotesTabViewModel { Notes = "nowa notatka" };

        //Act
        var result = vm.ToMeFile(existing);

        //Assert
        Assert.Equal("nowa notatka", result.Notes);
        Assert.Equal("Jan", result.FirstName);
        Assert.Equal("Kowalski", result.LastName);
        Assert.Equal("1|1|1980", result.DatesOfBirth);
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void ToMeFile_ThrowsArgumentNull_WhenViewModelNull()
    {
        //Arrange
        NotesTabViewModel vm = null;

        //Act + Assert
        Assert.Throws<ArgumentNullException>(() => vm.ToMeFile());
    }

    #endregion
}
