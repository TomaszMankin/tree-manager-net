using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using Serilog;
using TreeManager.Core.Abstractions.Notifications;

namespace TreeManager.App.Services;

public sealed class QueueRetryService : IQueueRetryService
{
    private readonly IEmailEscalator _escalator;
    private readonly IOfflineQueue _queue;
    private readonly ILogger _log;

    private DispatcherTimer _timer;
    private int _draining;

    public QueueRetryService(IEmailEscalator escalator, IOfflineQueue queue, ILogger log)
    {
        _escalator = escalator;
        _queue = queue;
        _log = log;
    }

    public void Start()
    {
        _timer = new DispatcherTimer { Interval = TimeSpan.FromMinutes(30) };
        _timer.Tick += (_, _) => _ = Task.Run(DrainAsync);
        _timer.Start();
    }

    public void Stop() => _timer?.Stop();

    internal async Task DrainAsync()
    {
        if (Interlocked.Exchange(ref _draining, 1) == 1)
        {
            return;
        }

        try
        {
            foreach (var message in _queue.List())
            {
                try
                {
                    await _escalator.SendAsync(message.SubjectText, message.BodyText);
                    _queue.Remove(message.Id);
                }
                catch (Exception ex)
                {
                    _log.Warning(ex, "QueueRetryService: send failed for {Id}; keeping entry", message.Id);
                }
            }
        }
        finally
        {
            Interlocked.Exchange(ref _draining, 0);
        }
    }
}
