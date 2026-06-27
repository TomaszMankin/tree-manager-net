using System;
using System.IO;
using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Conditions;
using FlaUI.Core.Definitions;
using FlaUI.UIA3;
using TreeManager.Infrastructure.Settings;

namespace TreeManager.App.L3.Harness;

/// <summary>
/// Launches the built TreeManager.App against a fresh disposable temp root and exposes
/// the FlaUI automation surface. The temp root is wired through the standard root pointer
/// so the app boots into it. Disposal closes the app and deletes the temp root.
/// </summary>
public sealed class AppSession : IDisposable
{
    private const string AppExeName = "TreeManager.App.exe";
    private const string PeopleListFolder = "Lista osób";
    private static readonly TimeSpan WindowWaitTimeout = TimeSpan.FromSeconds(30);

    private readonly Application _application;
    private readonly UIA3Automation _automation;
    private readonly string _previousPointerValue;

    public AppSession()
    {
        RootPath = Path.Combine(Path.GetTempPath(), "TM_L3_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(RootPath, PeopleListFolder));

        _previousPointerValue = SwapRootPointer(RootPath);

        _automation = new UIA3Automation();
        _application = Application.Launch(ResolveAppExePath());
    }

    public string RootPath { get; }

    public Window MainWindow => _application.GetMainWindow(_automation, WindowWaitTimeout);

    /// <summary>Finds a descendant of the main window by its AutomationId.</summary>
    public AutomationElement FindById(string automationId)
    {
        return MainWindow.FindFirstDescendant(cf => cf.ByAutomationId(automationId));
    }

    public void Dispose()
    {
        try
        {
            if (_application != null)
            {
                if (!_application.HasExited)
                {
                    _application.Close();
                }
                _application.Dispose();
            }
        }
        finally
        {
            _automation?.Dispose();
            RestoreRootPointer(_previousPointerValue);
            DeleteTempRoot();
        }
    }

    private static string ResolveAppExePath()
    {
        var assemblyDir = Path.GetDirectoryName(typeof(AppSession).Assembly.Location);
        var exePath = Path.Combine(assemblyDir, AppExeName);
        if (!File.Exists(exePath))
        {
            throw new FileNotFoundException("TreeManager.App executable not found beside the test assembly", exePath);
        }
        return exePath;
    }

    private static string SwapRootPointer(string newRoot)
    {
        var pointerPath = RootPointerStore.ResolveDefaultPointerPath();
        var existing = File.Exists(pointerPath) ? File.ReadAllText(pointerPath) : null;

        Directory.CreateDirectory(Path.GetDirectoryName(pointerPath));
        File.WriteAllText(pointerPath, newRoot);

        return existing;
    }

    private static void RestoreRootPointer(string previousValue)
    {
        var pointerPath = RootPointerStore.ResolveDefaultPointerPath();
        if (previousValue == null)
        {
            if (File.Exists(pointerPath))
            {
                File.Delete(pointerPath);
            }
            return;
        }

        File.WriteAllText(pointerPath, previousValue);
    }

    private void DeleteTempRoot()
    {
        if (Directory.Exists(RootPath))
        {
            Directory.Delete(RootPath, recursive: true);
        }
    }
}
