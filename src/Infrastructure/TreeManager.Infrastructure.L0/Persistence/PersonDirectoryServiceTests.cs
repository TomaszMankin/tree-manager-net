using Moq;
using TreeManager.Common.TestUtilities;
using TreeManager.Core.Abstractions.Persistence;
using TreeManager.Core.Domain;
using TreeManager.Infrastructure.Persistence;

namespace TreeManager.Infrastructure.L0.Persistence;

public class PersonDirectoryServiceTests
{
    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void GetAll_ReturnsSummaryPerMeFile()
    {
        var mockProcessor = new Mock<IMeFileProcessor>();
        const string rootPath = @"C:\fake\root";
        const string meFilePath = @"C:\fake\root\Lista osób\Alice\me.json";
        var id = Guid.NewGuid();
        var meFile = new MeFile { UniqueIdentifier = id, PersonName = "Alice Smith" };

        mockProcessor.Setup(p => p.ScanMeFiles(rootPath)).Returns(new[] { meFilePath });
        mockProcessor.Setup(p => p.ReadMeFile(meFilePath)).Returns(meFile);

        var service = new PersonDirectoryService(mockProcessor.Object);
        var result = service.GetAll(rootPath);

        Assert.Single(result);
        Assert.Equal(id, result[0].UniqueIdentifier);
        Assert.Equal("Alice Smith", result[0].DisplayName);
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void GetAll_UsesPersonName_AsDisplayName()
    {
        var mockProcessor = new Mock<IMeFileProcessor>();
        const string rootPath = @"C:\fake\root";
        const string path1 = @"C:\fake\root\Lista osób\Jan\me.json";
        const string path2 = @"C:\fake\root\Lista osób\Anna\me.json";
        var meFile1 = new MeFile { UniqueIdentifier = Guid.NewGuid(), PersonName = "Jan Kowalski" };
        var meFile2 = new MeFile { UniqueIdentifier = Guid.NewGuid(), PersonName = "Anna Nowak" };

        mockProcessor.Setup(p => p.ScanMeFiles(rootPath)).Returns(new[] { path1, path2 });
        mockProcessor.Setup(p => p.ReadMeFile(path1)).Returns(meFile1);
        mockProcessor.Setup(p => p.ReadMeFile(path2)).Returns(meFile2);

        var service = new PersonDirectoryService(mockProcessor.Object);
        var result = service.GetAll(rootPath);

        Assert.Equal(2, result.Count);
        Assert.Contains(result, s => s.DisplayName == "Jan Kowalski");
        Assert.Contains(result, s => s.DisplayName == "Anna Nowak");
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void GetAll_ThrowsArgumentException_WhenRootPathIsEmpty()
    {
        var service = new PersonDirectoryService(new Mock<IMeFileProcessor>().Object);
        Assert.Throws<ArgumentException>(() => service.GetAll(string.Empty));
    }
}
