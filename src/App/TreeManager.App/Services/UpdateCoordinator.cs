using System;
using System.Threading.Tasks;
using System.Windows;
using Serilog;

namespace TreeManager.App.Services;

public sealed class UpdateCoordinator
{
    private readonly IUpdateService _updateService;
    private readonly IUpdatePromptService _prompt;
    private readonly ILogger _log;

    public UpdateCoordinator(IUpdateService updateService, IUpdatePromptService prompt, ILogger log)
    {
        _updateService = updateService;
        _prompt = prompt;
        _log = log;
    }

    public async Task RunAsync()
    {
        try
        {
            // 1. Check for available update
            var result = await _updateService.CheckAsync();
            if (!result.HasUpdate)
            {
                return;
            }

            // 2. Ask the user — marshalled to UI thread when a Dispatcher is available
            var confirmed = await InvokePromptAsync(result.TargetVersion);
            if (!confirmed)
            {
                return;
            }

            // 3. Download and restart into new version
            await _updateService.DownloadAndApplyAsync();
        }
        catch (Exception ex)
        {
            _log.Error(ex, "Update coordination failed");
        }
    }

    private Task<bool> InvokePromptAsync(string targetVersion)
    {
        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher != null)
        {
            return dispatcher.InvokeAsync(() => _prompt.ConfirmUpdate(targetVersion)).Task;
        }

        return Task.FromResult(_prompt.ConfirmUpdate(targetVersion));
    }
}
