using System;
using System.Threading.Tasks;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using Serilog;
using TreeManager.Core.Abstractions.Notifications;
using TreeManager.Core.Abstractions.Settings;

namespace TreeManager.Infrastructure.Notifications;

public class GmailEscalator : IEmailEscalator
{
    private readonly IEmailSettingsStore _settings;
    private readonly ILogger _log;

    public GmailEscalator(IEmailSettingsStore settings, ILogger log)
    {
        _settings = settings;
        _log = log;
    }

    public async Task SendAsync(string subject, string body)
    {
        var config = _settings.Get();

        if (!config.IsConfigured)
        {
            _log.Warning("GmailEscalator: email settings not configured; skipping escalation");
            return;
        }

        await SendCoreAsync(config.Host, config.Port, config.UseSsl, config.FromAddress, config.ToAddress, config.AppPassword, subject, body);
    }

    internal virtual async Task SendCoreAsync(string host, int port, bool useSsl, string fromAddress, string toAddress, string appPassword, string subject, string body)
    {
        var msg = new MimeMessage();
        msg.From.Add(MailboxAddress.Parse(fromAddress));
        msg.To.Add(MailboxAddress.Parse(toAddress));
        msg.Subject = subject;
        msg.Body = new TextPart("plain") { Text = body };

        var secureOption = (useSsl && port == 465)
            ? SecureSocketOptions.SslOnConnect
            : SecureSocketOptions.StartTls;

        using var smtp = new SmtpClient();
        await smtp.ConnectAsync(host, port, secureOption);
        await smtp.AuthenticateAsync(fromAddress, appPassword);
        await smtp.SendAsync(msg);
        await smtp.DisconnectAsync(true);
    }
}
