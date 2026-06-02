using TreeManager.App.ViewModels;
using TreeManager.Common.TestUtilities;
using TreeManager.Core.Domain;

namespace TreeManager.App.L0.ViewModels;

public class NotesTabViewModelTests
{
    private readonly NotesTabViewModel _sut;

    public NotesTabViewModelTests()
    {
        _sut = new NotesTabViewModel();
    }

    #region Reset

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void Reset_SetsNotes_WhenMeFileHasNotes()
    {
        //Arrange
        var meFile = new MeFile { Notes = "Notatka z pliku" };

        //Act
        _sut.Reset(meFile);

        //Assert
        Assert.Equal("Notatka z pliku", _sut.Notes);
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void Reset_ClearsNotes_WhenMeFileNotesEmpty()
    {
        //Arrange
        _sut.Notes = "poprzednia notatka";
        var meFile = new MeFile { Notes = string.Empty };

        //Act
        _sut.Reset(meFile);

        //Assert
        Assert.Equal(string.Empty, _sut.Notes);
    }

    #endregion
}
