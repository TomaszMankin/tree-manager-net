using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using TreeManager.App.Services;
using TreeManager.App.Startup;
using TreeManager.App.ViewModels;
using TreeManager.App.Views;
using TreeManager.Core.Abstractions.IO;
using TreeManager.Core.Abstractions.Notifications;
using TreeManager.Core.Abstractions.Persistence;
using TreeManager.Core.Abstractions.Services;
using TreeManager.Core.Abstractions.Settings;
using TreeManager.Core.Abstractions.Shell;
using TreeManager.Core.Abstractions.Validation;
using TreeManager.Core.Services;
using TreeManager.Core.Services.Validation;
using TreeManager.Infrastructure.IO;
using TreeManager.Infrastructure.Logging;
using TreeManager.Infrastructure.Notifications;
using TreeManager.Infrastructure.Persistence;
using TreeManager.Infrastructure.Settings;
using TreeManager.Infrastructure.Shell;

namespace TreeManager.App;

public partial class App : Application
{
    private ServiceProvider _services;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        DispatcherUnhandledException += OnDispatcherUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

        _services = BuildServiceProvider();

        var bootstrapper = _services.GetRequiredService<StartupBootstrapper>();
        var result = bootstrapper.Resolve();
        if (result.ShouldShutdown)
        {
            Shutdown();
            return;
        }

        _services.GetRequiredService<LoggingBootstrapper>().Configure(result.RootPath);
        _services.GetRequiredService<IQueueRetryService>().Start();

        _services.GetRequiredService<MainWindow>().Show();
        _ = _services.GetRequiredService<UpdateCoordinator>().RunAsync();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _services?.GetService<IQueueRetryService>()?.Stop();
        Log.CloseAndFlush();
        _services?.Dispose();
        base.OnExit(e);
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        e.Handled = true;
        ReportCrash(e.Exception, "DispatcherUnhandledException");
    }

    private void OnUnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
    {
        e.SetObserved();
        ReportCrash(e.Exception, "UnobservedTaskException");
    }

    private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        var ex = e.ExceptionObject as Exception ?? new Exception(e.ExceptionObject?.ToString() ?? "Unknown error");

        if (_services != null)
        {
            ReportCrash(ex, "AppDomainUnhandledException");
            return;
        }

        var fallbackPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "TreeManager",
            "crash-fallback.log");

        try
        {
            var fs = new FileSystemFacade();
            fs.CreateDirectory(Path.GetDirectoryName(fallbackPath));
            fs.AppendAllText(fallbackPath, $"{DateTime.Now:u} [CRASH] {ex}{Environment.NewLine}");
        }
        catch (Exception writeEx)
        {
            Log.Logger.Error(writeEx, "App: fallback crash log write failed");
        }

        if (Application.Current?.Dispatcher.CheckAccess() == true)
        {
            ShowFallbackDialog();
        }
    }

    private void ReportCrash(Exception ex, string source)
    {
        if (_services == null)
        {
            ShowFallbackDialog();
            return;
        }

        _services.GetRequiredService<CrashReporter>().Report(ex, source);
    }

    private static void ShowFallbackDialog()
    {
        try
        {
            var dialog = new ErrorDialog();
            dialog.ShowDialog();
        }
        catch
        {
            // best-effort; do not recurse
        }
    }

    private static ServiceProvider BuildServiceProvider()
    {
        var services = new ServiceCollection();

        services.AddSingleton<ILogger>(sp => Log.Logger);
        services.AddSingleton<IFileSystemFacade, FileSystemFacade>();
        services.AddSingleton<IRootPointerStore, RootPointerStore>();
        services.AddSingleton<IMeFileProcessor, MeFileProcessor>();
        services.AddSingleton<IPersonRepository, PersonRepository>();
        services.AddSingleton<IPersonDirectoryService, PersonDirectoryService>();
        services.AddSingleton<IRootPickerService, RootPickerService>();
        services.AddSingleton<IPersonLoaderService, PersonLoaderService>();
        services.AddSingleton<IPersonPickerService, PersonPickerService>();
        services.AddSingleton<IDirtyTracker, DirtyTracker>();
        services.AddSingleton<IDirtyGuardService, DirtyGuardService>();
        services.AddSingleton<IDraftRepository, DraftRepository>();
        services.AddSingleton<IDraftPromoter, DraftPromoter>();
        services.AddSingleton<IPromoteConfirmService, PromoteConfirmService>();
        services.AddSingleton<IFolderRevealService, FolderRevealService>();
        services.AddSingleton<PersonEditDependencies>();
        services.AddSingleton<IShortcutCreator, ShellLinkShortcutCreator>();
        services.AddSingleton<IFolderTreeGenerator, FolderTreeGenerator>();
        services.AddSingleton<IFolderTreeSettingsStore, FolderTreeSettingsStore>();
        services.AddSingleton<ILineageFolderGenerator, LineageFolderGenerator>();
        services.AddSingleton<FolderTreeCommandDependencies>();
        services.AddSingleton<ITreeConsistencyValidator, TreeConsistencyValidator>();
        services.AddSingleton<IValidationMessageFormatter, ValidationMessageFormatter>();
        services.AddSingleton<IValidationReportService, ValidationReportService>();
        services.AddSingleton<ValidationCommandDependencies>();
        services.AddSingleton<StartupBootstrapper>();
        services.AddSingleton<LoggingBootstrapper>();
        services.AddSingleton<ICrashDialogService, CrashDialogService>();

        services.AddSingleton<IEmailSettingsStore>(sp =>
            new EmailSettingsStore(
                Path.Combine(AppContext.BaseDirectory, "appsettings.user.json"),
                sp.GetRequiredService<IFileSystemFacade>(),
                sp.GetRequiredService<ILogger>()));

        services.AddSingleton<IEmailEscalator, GmailEscalator>();

        services.AddSingleton<IOfflineQueue>(sp =>
            new FileOfflineQueue(
                sp.GetRequiredService<IRootPointerStore>().Read(),
                sp.GetRequiredService<IFileSystemFacade>(),
                sp.GetRequiredService<ILogger>()));

        services.AddSingleton<IQueueRetryService, QueueRetryService>();
        services.AddSingleton<CrashReporter>();
        services.AddSingleton<IUpdateService, VelopackUpdateService>();
        services.AddSingleton<IUpdatePromptService, UpdatePromptService>();
        services.AddSingleton<UpdateCoordinator>();

        services.AddTransient<OptionalDatePickerViewModel>();
        services.AddTransient<DatesTabViewModel>();
        services.AddTransient<FamilyTabViewModel>();
        services.AddTransient<PersonViewModel>();
        services.AddTransient<NotesTabViewModel>();
        services.AddTransient<MainViewModel>();
        services.AddTransient<MainWindow>();

        return services.BuildServiceProvider();
    }
}
