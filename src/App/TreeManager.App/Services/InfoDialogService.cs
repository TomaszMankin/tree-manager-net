using TreeManager.App.Views;

namespace TreeManager.App.Services;

public sealed class InfoDialogService : IInfoDialogService
{
    public void Show(string title, string message)
    {
        var dialog = new InfoDialog(title, message);
        dialog.ShowDialog();
    }
}
