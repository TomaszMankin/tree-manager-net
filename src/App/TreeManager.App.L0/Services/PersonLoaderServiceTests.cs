using System;
using System.Collections.Generic;
using Moq;
using TreeManager.App.Services;
using TreeManager.App.ViewModels;
using TreeManager.Common.TestUtilities;
using TreeManager.Core.Abstractions.Persistence;
using TreeManager.Core.Domain;

namespace TreeManager.App.L0.Services;

public class PersonLoaderServiceTests
{
    private const string FakeMeFilePath = @"C:\root\Lista osób\Jan Kowalski\me.json";
    private const string FakeRootPath = @"C:\root";

    private readonly Mock<IMeFileProcessor> _mockProcessor;
    private readonly Mock<IPersonDirectoryService> _mockDirectoryService;
    private readonly PersonViewModel _personVm;
    private readonly DatesTabViewModel _datesVm;
    private readonly FamilyTabViewModel _familyVm;
    private readonly PersonLoaderService _sut;

    public PersonLoaderServiceTests()
    {
        _mockProcessor = new Mock<IMeFileProcessor>();
        _mockDirectoryService = new Mock<IPersonDirectoryService>();
        _personVm = new PersonViewModel();
        _datesVm = new DatesTabViewModel();
        _familyVm = new FamilyTabViewModel();

        _mockDirectoryService
            .Setup(x => x.GetAll(FakeRootPath))
            .Returns(new List<PersonSummary>());

        _sut = new PersonLoaderService(_mockProcessor.Object, _mockDirectoryService.Object);
    }

    #region Load

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void Load_PopulatesPersonVmFirstName_WhenMeFileHasFirstName()
    {
        //Arrange
        var meFile = new MeFile { FirstName = "Jan" };
        _mockProcessor.Setup(x => x.ReadMeFile(FakeMeFilePath)).Returns(meFile);

        //Act
        _sut.Load(FakeMeFilePath, FakeRootPath, _personVm, _datesVm, _familyVm);

        //Assert
        Assert.Equal("Jan", _personVm.FirstName);
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void Load_PopulatesPersonVmAllFields_WhenMeFileIsFullyPopulated()
    {
        //Arrange
        var id = Guid.NewGuid();
        var meFile = new MeFile
        {
            UniqueIdentifier = id,
            FirstName = "Jan",
            LastName = "Kowalski",
            PersonName = "Jan Kowalski",
            Sex = Sex.Male,
        };
        _mockProcessor.Setup(x => x.ReadMeFile(FakeMeFilePath)).Returns(meFile);

        //Act
        _sut.Load(FakeMeFilePath, FakeRootPath, _personVm, _datesVm, _familyVm);

        //Assert
        Assert.Equal(id, _personVm.UniqueIdentifier);
        Assert.Equal("Jan", _personVm.FirstName);
        Assert.Equal("Kowalski", _personVm.LastName);
        Assert.Equal(Sex.Male, _personVm.Sex);
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void Load_PopulatesDatesVmBirthDate_WhenMeFileHasBirthDate()
    {
        //Arrange
        var meFile = new MeFile { DatesOfBirth = "15|06|1980" };
        _mockProcessor.Setup(x => x.ReadMeFile(FakeMeFilePath)).Returns(meFile);

        //Act
        _sut.Load(FakeMeFilePath, FakeRootPath, _personVm, _datesVm, _familyVm);

        //Assert
        Assert.Equal("15", _datesVm.BirthDate.Day);
        Assert.Equal("6", _datesVm.BirthDate.Month);
        Assert.Equal("1980", _datesVm.BirthDate.Year);
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void Load_PopulatesFamilyVmParentsSelected_WhenMeFileHasParents()
    {
        //Arrange
        var parentId = Guid.NewGuid();
        var meFile = new MeFile
        {
            ParentsId = new List<Guid> { parentId },
            Parents = new List<string> { "Anna Nowak" },
        };
        _mockProcessor.Setup(x => x.ReadMeFile(FakeMeFilePath)).Returns(meFile);
        _mockDirectoryService
            .Setup(x => x.GetAll(FakeRootPath))
            .Returns(new List<PersonSummary> { new PersonSummary(parentId, "Anna Nowak") });

        //Act
        _sut.Load(FakeMeFilePath, FakeRootPath, _personVm, _datesVm, _familyVm);

        //Assert
        Assert.Single(_familyVm.Parents.Selected);
        Assert.Equal(parentId, _familyVm.Parents.Selected[0].UniqueIdentifier);
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void Load_SetsFamilyLoadedPersonId_WhenMeFileLoaded()
    {
        //Arrange
        var id = Guid.NewGuid();
        var meFile = new MeFile { UniqueIdentifier = id };
        _mockProcessor.Setup(x => x.ReadMeFile(FakeMeFilePath)).Returns(meFile);

        //Act
        _sut.Load(FakeMeFilePath, FakeRootPath, _personVm, _datesVm, _familyVm);

        //Assert
        Assert.Equal(id, _familyVm.LoadedPersonId);
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void Load_ReturnsLoadedMeFile_WhenCalled()
    {
        //Arrange
        var meFile = new MeFile { FirstName = "Jan" };
        _mockProcessor.Setup(x => x.ReadMeFile(FakeMeFilePath)).Returns(meFile);

        //Act
        var result = _sut.Load(FakeMeFilePath, FakeRootPath, _personVm, _datesVm, _familyVm);

        //Assert
        Assert.Same(meFile, result);
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void Load_CallsDirectoryServiceGetAll_WhenCalled()
    {
        //Arrange
        var meFile = new MeFile();
        _mockProcessor.Setup(x => x.ReadMeFile(FakeMeFilePath)).Returns(meFile);

        //Act
        _sut.Load(FakeMeFilePath, FakeRootPath, _personVm, _datesVm, _familyVm);

        //Assert
        _mockDirectoryService.Verify(x => x.GetAll(FakeRootPath), Times.Once());
    }

    #endregion
}
