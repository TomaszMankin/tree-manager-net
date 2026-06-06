using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Moq;
using Serilog;
using TreeManager.App.Services;
using TreeManager.Core.Abstractions.Notifications;
using TreeManager.Core.Domain.Notifications;

namespace TreeManager.App.L0.Services;

public sealed class QueueRetryServiceTests
{
    private readonly Mock<IEmailEscalator> _escalatorMock = new();
    private readonly Mock<IOfflineQueue> _queueMock = new();
    private readonly Mock<ILogger> _logMock = new();

    [Fact]
    public async Task Drain_RemovesEntry_WhenSendSucceeds()
    {
        //Arrange
        var msg = MakeMessage("id-1");
        _queueMock.Setup(q => q.List()).Returns(new List<QueuedMessage> { msg });
        _escalatorMock.Setup(e => e.SendAsync(msg.SubjectText, msg.BodyText))
            .Returns(Task.CompletedTask);
        var sut = BuildSut();

        //Act
        await sut.DrainAsync();

        //Assert
        _queueMock.Verify(q => q.Remove("id-1"), Times.Once);
    }

    [Fact]
    public async Task Drain_KeepsEntry_WhenSendFails()
    {
        //Arrange
        var msg = MakeMessage("id-2");
        _queueMock.Setup(q => q.List()).Returns(new List<QueuedMessage> { msg });
        _escalatorMock.Setup(e => e.SendAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new Exception("smtp down"));
        var sut = BuildSut();

        //Act
        await sut.DrainAsync();

        //Assert
        _queueMock.Verify(q => q.Remove(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Drain_ContinuesRemainingEntries_WhenOneSendFails()
    {
        //Arrange
        var msg1 = new QueuedMessage
        {
            Id = "fail-id",
            SubjectText = "Subject fail",
            BodyText = "Body fail",
            CreatedUtc = DateTime.UtcNow
        };
        var msg2 = new QueuedMessage
        {
            Id = "ok-id",
            SubjectText = "Subject ok",
            BodyText = "Body ok",
            CreatedUtc = DateTime.UtcNow
        };
        _queueMock.Setup(q => q.List()).Returns(new List<QueuedMessage> { msg1, msg2 });
        _escalatorMock.Setup(e => e.SendAsync("Subject fail", "Body fail"))
            .ThrowsAsync(new Exception("smtp down"));
        _escalatorMock.Setup(e => e.SendAsync("Subject ok", "Body ok"))
            .Returns(Task.CompletedTask);
        var sut = BuildSut();

        //Act
        await sut.DrainAsync();

        //Assert
        _queueMock.Verify(q => q.Remove("fail-id"), Times.Never);
        _queueMock.Verify(q => q.Remove("ok-id"), Times.Once);
    }

    private QueueRetryService BuildSut() =>
        new(_escalatorMock.Object, _queueMock.Object, _logMock.Object);

    private static QueuedMessage MakeMessage(string id = null) => new()
    {
        Id = id ?? Guid.NewGuid().ToString(),
        SubjectText = "TreeManager crash: Test",
        BodyText = "Exception details",
        CreatedUtc = DateTime.UtcNow
    };
}
