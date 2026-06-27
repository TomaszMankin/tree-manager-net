using TreeManager.App.Views;

namespace TreeManager.App.Services;

public sealed class PromoteConfirmService : IPromoteConfirmService
{
    public bool Confirm(string summary, string title, string header)
    {
        var dialog = new PromoteConfirmDialog(summary, title, header);
        return dialog.ShowDialog() == true;
    }
}
