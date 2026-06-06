using System;
using System.Threading.Tasks;
using Moq;
using Serilog;
using TreeManager.App.Services;
using TreeManager.Common.TestUtilities;
using TreeManager.Core.Abstractions.Notifications;
using TreeManager.Core.Abstractions.Settings;
using TreeManager.Core.Domain.Notifications;
using TreeManager.Infrastructure.Notifications;

namespace TreeManager.App.L1.Notifications;

public sealed class EscalationFlowIntegrationTests
{
    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L1)]
    public async Task EscalateAsync_ReachesSendCore_WhenCrashReported()
    {
        //Arrange
        var settingsMock = new Mock<IEmailSettingsStore>();
        settingsMock.Setup(s => s.Get()).Returns(new EmailSettings
        {
            Host = "smtp.gmail.com",
            Port = 587,
            UseSsl = true,
            FromAddress = "sender@example.com",
            AppPassword = "xxxx xxxx xxxx xxxx",
            ToAddress = "receiver@example.com"
        });

        var escalator = new CapturingEscalator(settingsMock.Object, Log.Logger);
        var sut = new CrashReporter(
            Log.Logger,
            new NoOpCrashDialogService(),
            escalator,
            new Mock<IOfflineQueue>().Object);

        var exception = new InvalidOperationException("escalation-flow-test");

        //Act
        await sut.EscalateAsync(exception, "IntegrationTestSource");

        //Assert
        Assert.NotNull(escalator.CapturedSubject);
        Assert.Contains("IntegrationTestSource", escalator.CapturedSubject);
        Assert.Contains("escalation-flow-test", escalator.CapturedBody);
    }

    private sealed class CapturingEscalator : GmailEscalator
    {
        public string CapturedSubject { get; private set; }
        public string CapturedBody { get; private set; }

        public CapturingEscalator(IEmailSettingsStore settings, ILogger log)
            : base(settings, log) { }

        internal override Task SendCoreAsync(
            string host, int port, bool useSsl,
            string fromAddress, string toAddress, string appPassword,
            string subject, string body)
        {
            CapturedSubject = subject;
            CapturedBody = body;
            return Task.CompletedTask;
        }
    }

    private sealed class NoOpCrashDialogService : ICrashDialogService
    {
        public void ShowCrash() { }
    }
}
