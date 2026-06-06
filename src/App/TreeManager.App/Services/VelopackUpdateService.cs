using System;
using System.Threading.Tasks;
using Serilog;
using Velopack;
using Velopack.Sources;

namespace TreeManager.App.Services;

public sealed class VelopackUpdateService : IUpdateService
{
    private readonly ILogger _log;
    private UpdateInfo _pendingUpdate;

    public VelopackUpdateService(ILogger log)
    {
        _log = log;
    }

    public async Task<UpdateCheckResult> CheckAsync()
    {
        if (!IsManagedInstall())
        {
            return UpdateCheckResult.None;
        }

        return await CheckFeedAsync();
    }

    public async Task DownloadAndApplyAsync()
    {
        if (_pendingUpdate == null)
        {
            return;
        }

        try
        {
            var mgr = BuildManager();
            await mgr.DownloadUpdatesAsync(_pendingUpdate);
            mgr.ApplyUpdatesAndRestart(_pendingUpdate.TargetFullRelease);
        }
        catch (Exception ex)
        {
            _log.Error(ex, "Update download and apply failed");
        }
    }

    private bool IsManagedInstall()
    {
        try
        {
            return BuildManager().IsInstalled;
        }
        catch (Exception ex)
        {
            _log.Error(ex, "Update install-state check failed");
            return false;
        }
    }

    private async Task<UpdateCheckResult> CheckFeedAsync()
    {
        try
        {
            var mgr = BuildManager();
            var info = await mgr.CheckForUpdatesAsync();
            if (info == null)
            {
                return UpdateCheckResult.None;
            }

            _pendingUpdate = info;
            return UpdateCheckResult.Available(info.TargetFullRelease.Version.ToString());
        }
        catch (Exception ex)
        {
            _log.Error(ex, "Update check failed");
            return UpdateCheckResult.None;
        }
    }

    private static UpdateManager BuildManager() =>
        new(new GithubSource("https://github.com/TomaszMankin/tree-manager-net", null, false));
}
