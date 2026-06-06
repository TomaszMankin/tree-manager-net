using System;
using Serilog;

namespace TreeManager.App.Services;

public sealed class CrashReporter
{
    private readonly ILogger _log;
    private readonly ICrashDialogService _dialog;

    public CrashReporter(ILogger log, ICrashDialogService dialog)
    {
        _log = log;
        _dialog = dialog;
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
    }
}
