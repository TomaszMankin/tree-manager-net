using System;
using TreeManager.App.Mappers;
using TreeManager.App.ViewModels;
using TreeManager.Common.TestUtilities;
using TreeManager.Core.Domain;

namespace TreeManager.App.L0.ViewModels;

public class PersonViewModelTests
{
    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void Sex_DefaultsToUnknown_WhenConstructed()
    {
        Assert.Equal(Sex.Unknown, new PersonViewModel().Sex);
    }

    #region Reset

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void Reset_SetsAllFields_WhenMeFileProvided()
    {
        //Arrange
        var vm = new PersonViewModel();
        var id = Guid.NewGuid();
        var meFile = new MeFile
        {
            UniqueIdentifier = id,
            PersonName = "Jan Kowalski",
            Location = @"C:\root\Lista osób\Jan Kowalski",
            FirstName = "Jan",
            OtherFirstNames = "Janusz",
            LastName = "Kowalski",
            OtherLastNames = "Kowski",
            MaidenName = "Nowak",
            OtherMaidenNames = "Nowakowa",
            HasMaidenName = true,
            Sex = Sex.Male,
        };

        //Act
        vm.Reset(meFile);

        //Assert
        Assert.Equal(id, vm.UniqueIdentifier);
        Assert.Equal("Jan Kowalski", vm.PersonName);
        Assert.Equal(@"C:\root\Lista osób\Jan Kowalski", vm.Location);
        Assert.Equal("Jan", vm.FirstName);
        Assert.Equal("Janusz", vm.OtherFirstNames);
        Assert.Equal("Kowalski", vm.LastName);
        Assert.Equal("Kowski", vm.OtherLastNames);
        Assert.Equal("Nowak", vm.MaidenName);
        Assert.Equal("Nowakowa", vm.OtherMaidenNames);
        Assert.True(vm.HasMaidenName);
        Assert.Equal(Sex.Male, vm.Sex);
    }

    #endregion
}
