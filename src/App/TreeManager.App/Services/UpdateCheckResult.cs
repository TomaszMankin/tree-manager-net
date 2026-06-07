namespace TreeManager.App.Services;

public sealed class UpdateCheckResult
{
    public static readonly UpdateCheckResult None = new(false, null);

    public bool HasUpdate { get; }
    public string TargetVersion { get; }

    private UpdateCheckResult(bool hasUpdate, string targetVersion)
    {
        HasUpdate = hasUpdate;
        TargetVersion = targetVersion;
    }

    public static UpdateCheckResult Available(string targetVersion) =>
        new(true, targetVersion);
}
