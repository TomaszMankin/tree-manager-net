using System;
using System.Collections.Generic;
using TreeManager.App.Mappers;
using TreeManager.App.Services;
using TreeManager.App.ViewModels;
using TreeManager.Common.TestUtilities;
using TreeManager.Core.Domain;

namespace TreeManager.App.L0.Services;

public class DirtyTrackerTests
{
    private readonly DirtyTracker _sut;

    public DirtyTrackerTests()
    {
        _sut = new DirtyTracker();
    }

    #region IsDirty

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void IsDirty_ReturnsFalse_WhenCurrentMatchesSnapshot()
    {
        //Arrange
        var id = Guid.NewGuid();
        var snapshot = new MeFile { UniqueIdentifier = id, FirstName = "Jan", LastName = "Kowalski" };
        var current  = new MeFile { UniqueIdentifier = id, FirstName = "Jan", LastName = "Kowalski" };

        //Act
        var result = _sut.IsDirty(snapshot, current);

        //Assert
        Assert.False(result);
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void IsDirty_ReturnsTrue_WhenCurrentDiffersFromSnapshot()
    {
        //Arrange
        var id = Guid.NewGuid();
        var snapshot = new MeFile { UniqueIdentifier = id, FirstName = "Jan" };
        var current  = new MeFile { UniqueIdentifier = id, FirstName = "Piotr" };

        //Act
        var result = _sut.IsDirty(snapshot, current);

        //Assert
        Assert.True(result);
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void IsDirty_ReturnsFalse_WhenSnapshotNullAndCurrentIsPristine()
    {
        //Arrange — current assembled from default/empty VMs through the mapper chain
        var personVm = new PersonViewModel();
        var datesVm  = new DatesTabViewModel();
        var familyVm = new FamilyTabViewModel();
        var notesVm  = new NotesTabViewModel();

        var current = BuildFromVms(personVm, datesVm, familyVm, notesVm);

        //Act
        var result = _sut.IsDirty(null, current);

        //Assert
        Assert.False(result);
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void IsDirty_ReturnsTrue_WhenSnapshotNullAndAnyFieldEntered()
    {
        //Arrange
        var personVm = new PersonViewModel { FirstName = "Maria" };
        var datesVm  = new DatesTabViewModel();
        var familyVm = new FamilyTabViewModel();
        var notesVm  = new NotesTabViewModel();

        var current = BuildFromVms(personVm, datesVm, familyVm, notesVm);

        //Act
        var result = _sut.IsDirty(null, current);

        //Assert
        Assert.True(result);
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void IsDirty_ReturnsFalse_WhenOnlyLocationDiffers()
    {
        //Arrange
        var id = Guid.NewGuid();
        var snapshot = new MeFile { UniqueIdentifier = id, FirstName = "Jan", Location = @"C:\root\Lista osób\Jan" };
        var current  = new MeFile { UniqueIdentifier = id, FirstName = "Jan", Location = string.Empty };

        //Act
        var result = _sut.IsDirty(snapshot, current);

        //Assert
        Assert.False(result);
    }

    #endregion

    private static MeFile BuildFromVms(
        PersonViewModel personVm,
        DatesTabViewModel datesVm,
        FamilyTabViewModel familyVm,
        NotesTabViewModel notesVm)
    {
        var meFile = personVm.ToMeFile();
        meFile = datesVm.ToMeFile(meFile);
        meFile = familyVm.ToMeFile(meFile);
        meFile = notesVm.ToMeFile(meFile);
        return meFile;
    }
}
