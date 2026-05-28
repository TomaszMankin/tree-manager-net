using Moq;
using TreeManager.Common.TestUtilities;
using TreeManager.Core.Abstractions.Persistence;
using TreeManager.Core.Domain;
using TreeManager.Infrastructure.Persistence;

namespace TreeManager.Infrastructure.L0.Persistence;

public class PersonDirectoryServiceTests
{
    private readonly Mock<IMeFileProcessor> _mockProcessor;
    private readonly PersonDirectoryService _sut;

    public PersonDirectoryServiceTests()
    {
        _mockProcessor = new Mock<IMeFileProcessor>();
        _sut = new PersonDirectoryService(_mockProcessor.Object);
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void GetAll_ReturnsOneSummaryPerMeFile_WhenScanProducesMatches()
    {
        //Arrange
        const string rootPath = @"C:\fake\root";
        const string meFilePath = @"C:\fake\root\Lista osób\Alice\me.json";
        var id = Guid.NewGuid();
        var meFile = new MeFile { UniqueIdentifier = id, PersonName = "Alice Smith" };
        _mockProcessor.Setup(p => p.ScanMeFiles(rootPath)).Returns(new[] { meFilePath });
        _mockProcessor.Setup(p => p.ReadMeFile(meFilePath)).Returns(meFile);

        //Act
        var result = _sut.GetAll(rootPath);

        //Assert
        Assert.Single(result);
        Assert.Equal(id, result[0].UniqueIdentifier);
        Assert.Equal("Alice Smith", result[0].DisplayName);
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void GetAll_MapsPersonNameToDisplayName_WhenMeFileHasPersonName()
    {
        //Arrange
        const string rootPath = @"C:\fake\root";
        const string path1 = @"C:\fake\root\Lista osób\Jan\me.json";
        const string path2 = @"C:\fake\root\Lista osób\Anna\me.json";
        var meFile1 = new MeFile { UniqueIdentifier = Guid.NewGuid(), PersonName = "Jan Kowalski" };
        var meFile2 = new MeFile { UniqueIdentifier = Guid.NewGuid(), PersonName = "Anna Nowak" };
        _mockProcessor.Setup(p => p.ScanMeFiles(rootPath)).Returns(new[] { path1, path2 });
        _mockProcessor.Setup(p => p.ReadMeFile(path1)).Returns(meFile1);
        _mockProcessor.Setup(p => p.ReadMeFile(path2)).Returns(meFile2);

        //Act
        var result = _sut.GetAll(rootPath);

        //Assert
        Assert.Equal(2, result.Count);
        Assert.Contains(result, s => s.DisplayName == "Jan Kowalski");
        Assert.Contains(result, s => s.DisplayName == "Anna Nowak");
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void GetAll_ThrowsArgumentException_WhenRootPathIsEmpty()
    {
        Assert.Throws<ArgumentException>(() => _sut.GetAll(string.Empty));
    }
}
