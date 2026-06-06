using System;
using System.Threading.Tasks;
using Moq;
using Serilog;
using TreeManager.Common.TestUtilities;
using TreeManager.Core.Abstractions.Settings;
using TreeManager.Core.Domain.Notifications;
using TreeManager.Infrastructure.Notifications;

namespace TreeManager.Infrastructure.L0.Notifications;

public sealed class GmailEscalatorTests
{
    private readonly Mock<IEmailSettingsStore> _settingsMock = new();
    private readonly Mock<ILogger> _logMock = new();

    private static EmailSettings NotConfiguredSettings() => new();

    private static EmailSettings ConfiguredSettings() => new()
    {
        Host = "smtp.gmail.com",
        Port = 587,
        UseSsl = true,
        FromAddress = "sender@gmail.com",
        AppPassword = "xxxx xxxx xxxx xxxx",
        ToAddress = "receiver@gmail.com"
    };

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public async Task SendAsync_DoesNothing_WhenSettingsNotConfigured()
    {
        //Arrange
        _settingsMock.Setup(s => s.Get()).Returns(NotConfiguredSettings());
        var sut = new GmailEscalator(_settingsMock.Object, _logMock.Object);

        //Act
        await sut.SendAsync("subject", "body");

        //Assert — no exception; not-configured logged as warning
        _logMock.Verify(
            l => l.Warning(It.Is<string>(s => s.Contains("not configured"))),
            Times.Once);
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public async Task SendAsync_ThrowsToCaller_WhenTransportFails()
    {
        //Arrange
        _settingsMock.Setup(s => s.Get()).Returns(ConfiguredSettings());

        var seam = new FailingSendSeam(_settingsMock.Object, _logMock.Object);

        //Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            seam.SendAsync("subject", "body"));
    }

    private sealed class FailingSendSeam : GmailEscalator
    {
        public FailingSendSeam(IEmailSettingsStore settings, ILogger log)
            : base(settings, log) { }

        internal override Task SendCoreAsync(string host, int port, bool useSsl, string fromAddress, string toAddress, string appPassword, string subject, string body)
            => throw new InvalidOperationException("Simulated transport failure");
    }
}
