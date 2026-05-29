using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using TreeManager.App.Services;
using TreeManager.App.Startup;
using TreeManager.App.ViewModels;
using TreeManager.Core.Abstractions.IO;
using TreeManager.Core.Abstractions.Persistence;
using TreeManager.Core.Abstractions.Settings;
using TreeManager.Infrastructure.IO;
using TreeManager.Infrastructure.Persistence;
using TreeManager.Infrastructure.Settings;

namespace TreeManager.App;

public partial class App : Application
{
    private ServiceProvider _services;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _services = BuildServiceProvider();

        var bootstrapper = _services.GetRequiredService<StartupBootstrapper>();
        var result = bootstrapper.Resolve();
        if (result.ShouldShutdown)
        {
            Shutdown();
            return;
        }

        _services.GetRequiredService<MainWindow>().Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _services?.Dispose();
        base.OnExit(e);
    }

    private static ServiceProvider BuildServiceProvider()
    {
        var services = new ServiceCollection();

        services.AddSingleton<IFileSystemFacade, FileSystemFacade>();
        services.AddSingleton<IRootPointerStore, RootPointerStore>();
        services.AddSingleton<IMeFileProcessor, MeFileProcessor>();
        services.AddSingleton<IPersonDirectoryService, PersonDirectoryService>();
        services.AddSingleton<IRootPickerService, RootPickerService>();
        services.AddSingleton<StartupBootstrapper>();

        services.AddTransient<OptionalDatePickerViewModel>();
        services.AddTransient<DatesTabViewModel>();
        services.AddTransient<FamilyTabViewModel>();
        services.AddTransient<PersonViewModel>();
        services.AddTransient<MainViewModel>();
        services.AddTransient<MainWindow>();

        return services.BuildServiceProvider();
    }
}
