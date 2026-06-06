using TreeManager.App.Views;

namespace TreeManager.App.Services;

public sealed class CrashDialogService : ICrashDialogService
{
    public void ShowCrash()
    {
        var dialog = new ErrorDialog();
        dialog.ShowDialog();
    }
}
