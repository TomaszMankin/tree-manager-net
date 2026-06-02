using TreeManager.App.Views;

namespace TreeManager.App.Services;

public sealed class DirtyGuardService : IDirtyGuardService
{
    public bool ConfirmDiscard()
    {
        var dialog = new UnsavedChangesDialog();
        return dialog.ShowDialog() == true;
    }
}
