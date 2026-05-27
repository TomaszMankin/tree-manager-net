using System;
using AutoBogus;
using TreeManager.App.Mappers;
using TreeManager.Common.TestUtilities;
using TreeManager.Core.Domain;

namespace TreeManager.App.L0.Mappers;

public class MeFileMapperTests
{
    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void ToViewModel_CopiesAllNameFields_WhenSourceIsValid()
    {
        //Arrange
        var source = AutoFaker.Generate<MeFile>();

        //Act
        var result = source.ToViewModel();

        //Assert
        Assert.Equal(source.UniqueIdentifier, result.UniqueIdentifier);
        Assert.Equal(source.PersonName, result.PersonName);
        Assert.Equal(source.Location, result.Location);
        Assert.Equal(source.FirstName, result.FirstName);
        Assert.Equal(source.OtherFirstNames, result.OtherFirstNames);
        Assert.Equal(source.LastName, result.LastName);
        Assert.Equal(source.OtherLastNames, result.OtherLastNames);
        Assert.Equal(source.MaidenName, result.MaidenName);
        Assert.Equal(source.OtherMaidenNames, result.OtherMaidenNames);
        Assert.Equal(source.HasMaidenName, result.HasMaidenName);
        Assert.Equal(source.Sex, result.Sex);
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void ToViewModel_PreservesSexEnum_WhenSourceSexIsFemale()
    {
        //Arrange
        var source = new AutoFaker<MeFile>().RuleFor(x => x.Sex, Sex.Female).Generate();

        //Act
        var result = source.ToViewModel();

        //Assert
        Assert.Equal(Sex.Female, result.Sex);
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void ToViewModel_Throws_WhenSourceIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => ((MeFile)null).ToViewModel());
    }
}
