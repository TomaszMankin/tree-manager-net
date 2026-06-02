using System;
using System.Collections.Generic;
using TreeManager.App.ViewModels;
using TreeManager.Common.TestUtilities;
using TreeManager.Core.Domain;

namespace TreeManager.App.L0.ViewModels;

public class PersonPickerViewModelTests
{
    private readonly PersonPickerViewModel _sut;

    public PersonPickerViewModelTests()
    {
        _sut = new PersonPickerViewModel();
    }

    #region FilteredPeople

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void FilteredPeople_ReturnsAll_WhenSearchTextIsEmpty()
    {
        //Arrange
        var people = new List<PersonSummary>
        {
            new(Guid.NewGuid(), "Jan Kowalski"),
            new(Guid.NewGuid(), "Anna Nowak"),
        };
        _sut.LoadPeople(people);

        //Act
        var result = _sut.FilteredPeople;

        //Assert
        Assert.Equal(2, result.Count);
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void FilteredPeople_FiltersOnDisplayName_WhenSearchTextProvided()
    {
        //Arrange
        var people = new List<PersonSummary>
        {
            new(Guid.NewGuid(), "Jan Kowalski"),
            new(Guid.NewGuid(), "Anna Nowak"),
        };
        _sut.LoadPeople(people);
        _sut.SearchText = "Jan";

        //Act
        var result = _sut.FilteredPeople;

        //Assert
        Assert.Single(result);
        Assert.Equal("Jan Kowalski", result[0].DisplayName);
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void FilteredPeople_IsCaseInsensitive_WhenSearchTextMixedCase()
    {
        //Arrange
        var people = new List<PersonSummary>
        {
            new(Guid.NewGuid(), "Jan Kowalski"),
            new(Guid.NewGuid(), "Anna Nowak"),
        };
        _sut.LoadPeople(people);
        _sut.SearchText = "jan";

        //Act
        var result = _sut.FilteredPeople;

        //Assert
        Assert.Single(result);
        Assert.Equal("Jan Kowalski", result[0].DisplayName);
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void FilteredPeople_ReturnsEmpty_WhenNoMatch()
    {
        //Arrange
        var people = new List<PersonSummary>
        {
            new(Guid.NewGuid(), "Jan Kowalski"),
        };
        _sut.LoadPeople(people);
        _sut.SearchText = "Xyz";

        //Act
        var result = _sut.FilteredPeople;

        //Assert
        Assert.Empty(result);
    }

    #endregion
}
