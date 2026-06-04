using System;
using System.Runtime.InteropServices;
using Serilog;
using TreeManager.Core.Abstractions.Shell;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.Com;
using Windows.Win32.UI.Shell;

namespace TreeManager.Infrastructure.Shell;

/// <summary>
/// Creates Windows shortcut (.lnk) files via IShellLinkW + IPersistFile COM interfaces.
/// Uses CsWin32-generated interop (Unicode-safe; no WScript.Shell).
/// ADR-014.
/// </summary>
public sealed class ShellLinkShortcutCreator : IShortcutCreator
{
    // Stable, public Windows constant: CLSID for Shell Link coclass.
    private static readonly Guid ClsidShellLink = new Guid("00021401-0000-0000-C000-000000000046");

    private readonly ILogger _log;

    public ShellLinkShortcutCreator(ILogger log)
    {
        _log = log;
    }

    public void Create(string targetPath, string linkFilePath)
    {
        // Idempotent COM apartment init on calling thread.
        // Tolerate S_FALSE (already initialized) and RPC_E_CHANGED_MODE (WPF STA already set up).
        HRESULT coInitHr = PInvoke.CoInitializeEx(COINIT.COINIT_APARTMENTTHREADED);
        // S_OK = 0: we initialized; S_FALSE = 1: already init; RPC_E_CHANGED_MODE = 0x80010106: STA already
        bool initializedHere = coInitHr.Value == 0;

        try
        {
            var hr = PInvoke.CoCreateInstance<IShellLinkW>(
                ClsidShellLink,
                null,
                CLSCTX.CLSCTX_INPROC_SERVER,
                out var shellLink);

            hr.ThrowOnFailure();

            // SetPath via CsWin32-generated string extension method (Unicode-safe)
            shellLink.SetPath(targetPath);

            // QueryInterface IPersistFile
            var persistFile = (IPersistFile)shellLink;

            // Save via extension method (Unicode-safe)
            persistFile.Save(linkFilePath, fRemember: true);
        }
        catch (Exception ex)
        {
            _log.Error(ex, "ShellLinkShortcutCreator.Create failed for {Target}", targetPath);
            throw;
        }
        finally
        {
            if (initializedHere)
            {
                PInvoke.CoUninitialize();
            }
        }
    }

    /// <summary>
    /// Resolves the target path of an existing .lnk file via IShellLinkW.GetPath.
    /// Used in tests to verify round-trip correctness. Not part of IShortcutCreator.
    /// </summary>
    public static string Resolve(string linkFilePath)
    {
        HRESULT coInitHr = PInvoke.CoInitializeEx(COINIT.COINIT_APARTMENTTHREADED);
        bool initializedHere = coInitHr.Value == 0;

        try
        {
            var hr = PInvoke.CoCreateInstance<IShellLinkW>(
                ClsidShellLink,
                null,
                CLSCTX.CLSCTX_INPROC_SERVER,
                out var shellLink);

            hr.ThrowOnFailure();

            var persistFile = (IPersistFile)shellLink;
            persistFile.Load(linkFilePath, STGM.STGM_READ);

            // Resolve the link (fill in any stale path info)
            shellLink.Resolve(HWND.Null, 1); // SLR_NO_UI = 1

            Span<char> buffer = stackalloc char[260]; // MAX_PATH
            unsafe
            {
                Windows.Win32.Storage.FileSystem.WIN32_FIND_DATAW findData = default;
                shellLink.GetPath(buffer, ref findData, 0);
            }

            return new string(buffer).TrimEnd('\0');
        }
        finally
        {
            if (initializedHere)
            {
                PInvoke.CoUninitialize();
            }
        }
    }
}
