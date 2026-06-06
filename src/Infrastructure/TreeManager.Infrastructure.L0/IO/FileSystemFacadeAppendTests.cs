using System;
using System.IO;
using System.Text;
using TreeManager.Common.TestUtilities;
using TreeManager.Infrastructure.IO;

namespace TreeManager.Infrastructure.L0.IO;

public sealed class FileSystemFacadeAppendTests : IDisposable
{
    private readonly string _tempDir;
    private readonly FileSystemFacade _sut;

    public FileSystemFacadeAppendTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDir);
        _sut = new FileSystemFacade();
    }

    public void Dispose()
    {
        try { Directory.Delete(_tempDir, recursive: true); } catch { }
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void AppendAllText_AppendsContent_WhenFileAlreadyExists()
    {
        //Arrange
        var filePath = Path.Combine(_tempDir, "append-test.log");
        File.WriteAllText(filePath, "first line\n", Encoding.UTF8);

        //Act
        _sut.AppendAllText(filePath, "second line\n");

        //Assert
        var content = File.ReadAllText(filePath);
        Assert.Contains("first line", content);
        Assert.Contains("second line", content);
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void AppendAllText_CreatesFileWithContent_WhenFileMissing()
    {
        //Arrange
        var filePath = Path.Combine(_tempDir, "new-file.log");

        //Act
        _sut.AppendAllText(filePath, "hello");

        //Assert
        Assert.True(File.Exists(filePath));
        Assert.Equal("hello", File.ReadAllText(filePath, Encoding.UTF8));
    }
}
