namespace TreeManager.App.Startup;

public readonly record struct BootstrapResult(bool ShouldShutdown, string RootPath)
{
    public static BootstrapResult UseExisting(string path) => new(false, path);
    public static BootstrapResult Cancelled() => new(true, string.Empty);
}
