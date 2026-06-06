using System;
using System.Threading.Tasks;
using Serilog;
using TreeManager.Core.Abstractions.Notifications;
using TreeManager.Core.Domain.Notifications;

namespace TreeManager.App.Services;

public sealed class CrashReporter
{
    private readonly ILogger _log;
    private readonly ICrashDialogService _dialog;
    private readonly IEmailEscalator _escalator;
    private readonly IOfflineQueue _queue;

    public CrashReporter(ILogger log, ICrashDialogService dialog, IEmailEscalator escalator, IOfflineQueue queue)
    {
        _log = log;
        _dialog = dialog;
        _escalator = escalator;
        _queue = queue;
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
}
