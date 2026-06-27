using TreeManager.Infrastructure.IO;
using TreeManager.Infrastructure.Shell;
using TreeManager.UiTesting.Graph;
using TreeManager.UiTesting.Reading;
using TreeManager.UiTesting.Verification;

namespace TreeManager.App.L3.Harness;

/// <summary>
/// Walks the session's temp root with the real filesystem and shortcut implementations
/// and asserts the on-disk graph matches the supplied expectation, printing the full
/// discrepancy report on failure.
/// </summary>
public static class DiskVerification
{
    public static void VerifyOnDisk(AppSession session, ExpectedTreeGraph expected)
    {
        var fs = new FileSystemFacade();
        var shortcutCreator = new ShellLinkShortcutCreator(Serilog.Log.Logger);

        var reader = new DiskTreeGraphReader(fs, shortcutCreator);
        var actual = reader.Read(session.RootPath);

        var result = new TreeGraphVerifier().Verify(expected, actual);

        Assert.True(result.IsMatch, result.Describe());
    }
}
