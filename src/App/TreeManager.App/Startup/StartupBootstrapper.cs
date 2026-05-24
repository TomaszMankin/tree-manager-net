using TreeManager.App.Services;
using TreeManager.Core.Abstractions.IO;
using TreeManager.Core.Abstractions.Settings;

namespace TreeManager.App.Startup;

public sealed class StartupBootstrapper
{
    private readonly IRootPointerStore _store;
    private readonly IRootPickerService _picker;
    private readonly IFileSystemFacade _fs;

    public StartupBootstrapper(IRootPointerStore store, IRootPickerService picker, IFileSystemFacade fs)
    {
        _store = store;
        _picker = picker;
        _fs = fs;
    }

    public BootstrapResult Resolve()
    {
        var existing = _store.Read();
        if (!string.IsNullOrEmpty(existing) && _fs.DirectoryExists(existing))
        {
            return BootstrapResult.UseExisting(existing);
        }

        var picked = _picker.PickRoot();
        if (string.IsNullOrEmpty(picked))
        {
            return BootstrapResult.Cancelled();
        }

        _store.Write(picked);
        return BootstrapResult.UseExisting(picked);
    }
}
