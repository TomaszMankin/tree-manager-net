using TreeManager.App.Views;

namespace TreeManager.App.Services;

public sealed class PromoteConfirmService : IPromoteConfirmService
{
    public bool Confirm(string summary)
    {
        var dialog = new PromoteConfirmDialog(summary);
        return dialog.ShowDialog() == true;
    }
}
