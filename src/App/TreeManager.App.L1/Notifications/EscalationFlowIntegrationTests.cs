using System;
using System.Threading.Tasks;
using Moq;
using Serilog;
using TreeManager.App.Services;
using TreeManager.Core.Abstractions.Notifications;

namespace TreeManager.App.L1.Notifications;

public sealed class EscalationFlowIntegrationTests
{
    [Fact]
    public async Task CrashReport_InvokesEscalator_WhenCrashReported()
    {
        //Arrange
        var logMock = new Mock<ILogger>();
        var dialogMock = new Mock<ICrashDialogService>();
        var escalatorMock = new Mock<IEmailEscalator>();
        var queueMock = new Mock<IOfflineQueue>();

        escalatorMock.Setup(e => e.SendAsync(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        var sut = new CrashReporter(logMock.Object, dialogMock.Object, escalatorMock.Object, queueMock.Object);
        var exception = new InvalidOperationException("escalation-flow-test");

        //Act
        await sut.EscalateAsync(exception, "IntegrationTestSource");

        //Assert
        escalatorMock.Verify(
            e => e.SendAsync(
                It.Is<string>(s => s.Contains("IntegrationTestSource")),
                It.Is<string>(b => b.Contains("escalation-flow-test"))),
            Times.Once);
    }
}
