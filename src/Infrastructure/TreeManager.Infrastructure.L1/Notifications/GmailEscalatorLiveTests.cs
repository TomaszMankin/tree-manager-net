using System;
using System.Threading.Tasks;
using Serilog;
using TreeManager.Core.Abstractions.Settings;
using TreeManager.Core.Domain.Notifications;
using TreeManager.Infrastructure.Notifications;

namespace TreeManager.Infrastructure.L1.Notifications;

[Trait("Tier", "LiveSmtp")]
public sealed class GmailEscalatorLiveTests
{
    [Fact]
    public async Task SendAsync_DeliversEmail_WhenCredentialsAreConfigured()
    {
        var username  = Environment.GetEnvironmentVariable("SMTP_USERNAME");
        var password  = Environment.GetEnvironmentVariable("SMTP_APP_PASSWORD");
        var recipient = Environment.GetEnvironmentVariable("SMTP_RECIPIENT");

        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password) || string.IsNullOrEmpty(recipient))
        {
            return;
        }

        var settings = new EmailSettings
        {
            Host        = "smtp.gmail.com",
            Port        = 587,
            UseSsl      = true,
            FromAddress = username,
            AppPassword = password,
            ToAddress   = recipient
        };

        var escalator = new GmailEscalator(new StaticEmailSettingsStore(settings), Log.Logger);

        await escalator.SendAsync("Tree Manager — live test", "Live SMTP test from CI. If you see this, escalation delivery works.");
    }

    private sealed class StaticEmailSettingsStore : IEmailSettingsStore
    {
        private readonly EmailSettings _settings;

        public StaticEmailSettingsStore(EmailSettings settings) => _settings = settings;

        public EmailSettings Get() => _settings;
    }
}
