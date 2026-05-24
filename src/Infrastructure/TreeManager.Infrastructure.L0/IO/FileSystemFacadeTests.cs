using TreeManager.Common.TestUtilities;
using TreeManager.Infrastructure.IO;

namespace TreeManager.Infrastructure.L0.IO;

/// <summary>
/// L1 tests for <see cref="FileSystemFacade"/> — exercises real filesystem against isolated temp directories.
/// Each test provisions its own temp root and tears it down in try/finally.
/// </summary>
public class FileSystemFacadeTests
{
    private static string NewTempRoot() =>
        Path.Combine(Path.GetTempPath(), $"tmtest-{Guid.NewGuid():N}");

    private static FileSystemFacade CreateSut() => new();

    // -------------------------------------------------------------------------
    // FileExists
    // -------------------------------------------------------------------------

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L1)]
    public void FileExists_True_WhenFileWritten()
    {
        var root = NewTempRoot();
        try
        {
            Directory.CreateDirectory(root);
            var filePath = Path.Combine(root, "test.txt");
            File.WriteAllText(filePath, "hello");

            var sut = CreateSut();
            Assert.True(sut.FileExists(filePath));
        }
        finally { Directory.Delete(root, recursive: true); }
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L1)]
    public void FileExists_False_WhenAbsent()
    {
        var root = NewTempRoot();
        try
        {
            Directory.CreateDirectory(root);
            var sut = CreateSut();
            Assert.False(sut.FileExists(Path.Combine(root, "nonexistent.txt")));
        }
        finally { Directory.Delete(root, recursive: true); }
    }

    // -------------------------------------------------------------------------
    // DirectoryExists
    // -------------------------------------------------------------------------

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L1)]
    public void DirectoryExists_True_WhenCreated()
    {
        var root = NewTempRoot();
        try
        {
            Directory.CreateDirectory(root);
            var sut = CreateSut();
            Assert.True(sut.DirectoryExists(root));
        }
        finally { Directory.Delete(root, recursive: true); }
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L1)]
    public void DirectoryExists_False_WhenAbsent()
    {
        var root = NewTempRoot();
        var sut = CreateSut();
        Assert.False(sut.DirectoryExists(root));
    }

    // -------------------------------------------------------------------------
    // WriteAllText / ReadAllText
    // -------------------------------------------------------------------------

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L1)]
    public void WriteAllText_ThenReadAllText_RoundtripsContent()
    {
        var root = NewTempRoot();
        try
        {
            Directory.CreateDirectory(root);
            var filePath = Path.Combine(root, "data.json");
            var content = "{ \"key\": \"Mężczyzna\" }"; // Polish chars
            var sut = CreateSut();

            sut.WriteAllText(filePath, content);
            var result = sut.ReadAllText(filePath);

            Assert.Equal(content, result);
        }
        finally { Directory.Delete(root, recursive: true); }
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L1)]
    public void WriteAllText_CreatesFileWhenAbsent()
    {
        var root = NewTempRoot();
        try
        {
            Directory.CreateDirectory(root);
            var filePath = Path.Combine(root, "new.txt");
            var sut = CreateSut();

            Assert.False(File.Exists(filePath));
            sut.WriteAllText(filePath, "content");
            Assert.True(File.Exists(filePath));
        }
        finally { Directory.Delete(root, recursive: true); }
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L1)]
    public void WriteAllText_WritesWithoutBom()
    {
        var root = NewTempRoot();
        try
        {
            Directory.CreateDirectory(root);
            var filePath = Path.Combine(root, "nobom.txt");
            var sut = CreateSut();

            sut.WriteAllText(filePath, "hello");

            var bytes = File.ReadAllBytes(filePath);
            // First byte must NOT be BOM (0xEF)
            Assert.NotEqual(0xEF, bytes[0]);
            // First byte is 'h' = 104
            Assert.Equal((byte)'h', bytes[0]);
        }
        finally { Directory.Delete(root, recursive: true); }
    }

    // -------------------------------------------------------------------------
    // EnumerateFiles
    // -------------------------------------------------------------------------

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L1)]
    public void EnumerateFiles_OnPopulatedDir_ReturnsExpected()
    {
        var root = NewTempRoot();
        try
        {
            Directory.CreateDirectory(root);
            File.WriteAllText(Path.Combine(root, "a.txt"), "");
            File.WriteAllText(Path.Combine(root, "b.txt"), "");
            File.WriteAllText(Path.Combine(root, "c.json"), "{}");

            var sut = CreateSut();
            var files = sut.EnumerateFiles(root)
                .Select(Path.GetFileName)
                .Order()
                .ToList();

            Assert.Equal(["a.txt", "b.txt", "c.json"], files);
        }
        finally { Directory.Delete(root, recursive: true); }
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L1)]
    public void EnumerateFiles_WithSearchPattern_FiltersCorrectly()
    {
        var root = NewTempRoot();
        try
        {
            Directory.CreateDirectory(root);
            File.WriteAllText(Path.Combine(root, "a.txt"), "");
            File.WriteAllText(Path.Combine(root, "b.json"), "{}");

            var sut = CreateSut();
            var files = sut.EnumerateFiles(root, "*.json")
                .Select(Path.GetFileName)
                .ToList();

            Assert.Equal(["b.json"], files);
        }
        finally { Directory.Delete(root, recursive: true); }
    }

    // -------------------------------------------------------------------------
    // EnumerateDirectories
    // -------------------------------------------------------------------------

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L1)]
    public void EnumerateDirectories_OnNestedDir_ReturnsImmediateChildren()
    {
        var root = NewTempRoot();
        try
        {
            Directory.CreateDirectory(Path.Combine(root, "sub1"));
            Directory.CreateDirectory(Path.Combine(root, "sub2"));
            // Deeply nested — should NOT appear in immediate-child enumeration
            Directory.CreateDirectory(Path.Combine(root, "sub1", "deep"));

            var sut = CreateSut();
            var dirs = sut.EnumerateDirectories(root)
                .Select(Path.GetFileName)
                .Order()
                .ToList();

            Assert.Equal(["sub1", "sub2"], dirs);
        }
        finally { Directory.Delete(root, recursive: true); }
    }

    // -------------------------------------------------------------------------
    // CreateDirectory
    // -------------------------------------------------------------------------

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L1)]
    public void CreateDirectory_Idempotent()
    {
        var root = NewTempRoot();
        try
        {
            var sut = CreateSut();
            sut.CreateDirectory(root);
            // Second call must not throw
            sut.CreateDirectory(root);
            Assert.True(Directory.Exists(root));
        }
        finally { Directory.Delete(root, recursive: true); }
    }

    // -------------------------------------------------------------------------
    // DeleteFile
    // -------------------------------------------------------------------------

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L1)]
    public void DeleteFile_RemovesExisting()
    {
        var root = NewTempRoot();
        try
        {
            Directory.CreateDirectory(root);
            var filePath = Path.Combine(root, "todelete.txt");
            File.WriteAllText(filePath, "bye");

            var sut = CreateSut();
            sut.DeleteFile(filePath);

            Assert.False(File.Exists(filePath));
        }
        finally { Directory.Delete(root, recursive: true); }
    }

    // -------------------------------------------------------------------------
    // DeleteDirectory
    // -------------------------------------------------------------------------

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L1)]
    public void DeleteDirectory_RecursiveTrue_DeletesNested()
    {
        var root = NewTempRoot();
        var target = Path.Combine(root, "target");
        try
        {
            Directory.CreateDirectory(Path.Combine(target, "nested"));
            File.WriteAllText(Path.Combine(target, "nested", "file.txt"), "x");

            var sut = CreateSut();
            sut.DeleteDirectory(target, recursive: true);

            Assert.False(Directory.Exists(target));
        }
        finally
        {
            if (Directory.Exists(root))
                Directory.Delete(root, recursive: true);
        }
    }
}
