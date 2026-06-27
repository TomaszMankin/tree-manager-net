using System;
using System.Threading.Tasks;
using Serilog;
using TreeManager.Core.Abstractions.Notifications;
using TreeManager.Core.Abstractions.Settings;
using TreeManager.Core.Domain.Notifications;

namespace TreeManager.App.Services;

public sealed class CrashReporter : ICrashReporter
{
    private const string ManualReportTitle = "Raport o błędzie";
    private const string ManualReportSentMessage = "Raport został wysłany.";
    private const string ManualReportNotConfiguredMessage = "Konfiguracja e-mail nie jest skonfigurowana. Raport nie został wysłany.";

    private readonly ILogger _log;
    private readonly ICrashDialogService _dialog;
    private readonly IEmailEscalator _escalator;
    private readonly IOfflineQueue _queue;
    private readonly IEmailSettingsStore _settings;
    private readonly IInfoDialogService _info;

    public CrashReporter(
        ILogger log,
        ICrashDialogService dialog,
        IEmailEscalator escalator,
        IOfflineQueue queue,
        IEmailSettingsStore settings,
        IInfoDialogService info)
    {
        _log = log;
        _dialog = dialog;
        _escalator = escalator;
        _queue = queue;
        _settings = settings;
        _info = info;
    }

    public void Report(Exception ex, string source)
    {
        _log.Error(ex, "Unhandled exception from {Source}", source);

        try
        {
            _dialog.ShowCrash();
        }
        catch
        {
            // guard against recursive crash if the dialog itself fails
        }

        _ = Task.Run(() => EscalateAsync(ex, source));
    }

    public void ReportManual(string note)
    {
        _log.Information("Manual report submitted: {Note}", note);

        var config = _settings.Get();
        if (!config.IsConfigured)
        {
            _info.Show(ManualReportTitle, ManualReportNotConfiguredMessage);
            return;
        }

        _ = Task.Run(() => SendManualAsync(note));
        _info.Show(ManualReportTitle, ManualReportSentMessage);
    }

    internal async Task EscalateAsync(Exception ex, string source)
    {
        var subject = $"TreeManager crash: {source}";
        var body = ex.ToString();

        try
        {
            await _escalator.SendAsync(subject, body);
        }
        catch
        {
            try
            {
                _queue.Enqueue(new QueuedMessage
                {
                    Id = Guid.NewGuid().ToString(),
                    SubjectText = subject,
                    BodyText = body,
                    CreatedUtc = DateTime.UtcNow
                });
            }
            catch (Exception qex)
            {
                _log.Error(qex, "CrashReporter: escalation queue write failed");
            }
        }
    }

    private async Task SendManualAsync(string note)
    {
        var subject = "TreeManager raport ręczny";
        var body = note;

        try
        {
            await _escalator.SendAsync(subject, body);
        }
        catch
        {
            try
            {
                _queue.Enqueue(new QueuedMessage
                {
                    Id = Guid.NewGuid().ToString(),
                    SubjectText = subject,
                    BodyText = body,
                    CreatedUtc = DateTime.UtcNow
                });
            }
            catch (Exception qex)
            {
                _log.Error(qex, "CrashReporter: manual report queue write failed");
            }
        }
    }
}
