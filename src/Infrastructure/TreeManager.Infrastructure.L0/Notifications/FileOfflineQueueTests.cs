using System;
using System.Collections.Generic;
using System.IO;
using Moq;
using Serilog;
using TreeManager.Common.TestUtilities;
using TreeManager.Core.Abstractions.IO;
using TreeManager.Core.Domain.Notifications;
using TreeManager.Infrastructure.Notifications;

namespace TreeManager.Infrastructure.L0.Notifications;

public sealed class FileOfflineQueueTests
{
    private const string FakeRoot = @"C:\fake\root";
    private const string QueueDir = @"C:\fake\root\.TreeManager\offline_queue";

    private readonly Mock<IFileSystemFacade> _fsMock = new();
    private readonly Mock<ILogger> _logMock = new();

    private FileOfflineQueue BuildSut(string root = FakeRoot) =>
        new(root, _fsMock.Object, _logMock.Object);

    private static QueuedMessage MakeMessage(string id = null) => new()
    {
        Id = id ?? Guid.NewGuid().ToString(),
        SubjectText = "TreeManager crash: Test",
        BodyText = "Exception details",
        CreatedUtc = DateTime.UtcNow
    };

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void Enqueue_WritesJsonFileUnderOfflineQueue_WhenCalled()
    {
        //Arrange
        var message = MakeMessage("abc-123");
        string writtenPath = null;
        string writtenContent = null;
        _fsMock.Setup(f => f.WriteAllText(It.IsAny<string>(), It.IsAny<string>()))
            .Callback<string, string>((p, c) => { writtenPath = p; writtenContent = c; });

        //Act
        BuildSut().Enqueue(message);

        //Assert
        _fsMock.Verify(f => f.CreateDirectory(QueueDir), Times.Once);
        Assert.NotNull(writtenPath);
        Assert.Contains("abc-123", writtenPath);
        Assert.NotNull(writtenContent);
        Assert.Contains("abc-123", writtenContent);
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void Enqueue_CreatesOfflineQueueDirectory_WhenMissing()
    {
        //Arrange
        var message = MakeMessage();

        //Act
        BuildSut().Enqueue(message);

        //Assert
        _fsMock.Verify(f => f.CreateDirectory(QueueDir), Times.Once);
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void List_ReturnsAllQueuedMessages_WhenQueueHasEntries()
    {
        //Arrange
        var id1 = Guid.NewGuid().ToString();
        var id2 = Guid.NewGuid().ToString();
        var json1 = $@"{{""id"":""{id1}"",""subjectText"":""Sub1"",""bodyText"":""Body1"",""createdUtc"":""2026-01-01T00:00:00Z""}}";
        var json2 = $@"{{""id"":""{id2}"",""subjectText"":""Sub2"",""bodyText"":""Body2"",""createdUtc"":""2026-01-02T00:00:00Z""}}";

        _fsMock.Setup(f => f.DirectoryExists(QueueDir)).Returns(true);
        _fsMock.Setup(f => f.EnumerateFiles(QueueDir, "*.json"))
            .Returns(new[] { Path.Combine(QueueDir, $"{id1}.json"), Path.Combine(QueueDir, $"{id2}.json") });
        _fsMock.Setup(f => f.ReadAllText(Path.Combine(QueueDir, $"{id1}.json"))).Returns(json1);
        _fsMock.Setup(f => f.ReadAllText(Path.Combine(QueueDir, $"{id2}.json"))).Returns(json2);

        //Act
        var result = BuildSut().List();

        //Assert
        Assert.Equal(2, result.Count);
        Assert.Contains(result, m => m.Id == id1);
        Assert.Contains(result, m => m.Id == id2);
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void List_ReturnsEmpty_WhenQueueDirectoryMissing()
    {
        //Arrange
        _fsMock.Setup(f => f.DirectoryExists(QueueDir)).Returns(false);

        //Act
        var result = BuildSut().List();

        //Assert
        Assert.Empty(result);
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void Remove_DeletesMatchingFile_WhenIdExists()
    {
        //Arrange
        var id = Guid.NewGuid().ToString();
        var expectedPath = Path.Combine(QueueDir, $"{id}.json");
        _fsMock.Setup(f => f.FileExists(expectedPath)).Returns(true);

        //Act
        BuildSut().Remove(id);

        //Assert
        _fsMock.Verify(f => f.DeleteFile(expectedPath), Times.Once);
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void Enqueue_IsNoOp_WhenRootPathEmpty()
    {
        //Arrange
        var message = MakeMessage();

        //Act
        BuildSut(string.Empty).Enqueue(message);

        //Assert
        _fsMock.Verify(f => f.CreateDirectory(It.IsAny<string>()), Times.Never);
        _fsMock.Verify(f => f.WriteAllText(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }
}
