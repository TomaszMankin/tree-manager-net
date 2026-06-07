using TreeManager.App.Views;

namespace TreeManager.App.Services;

public sealed class UpdatePromptService : IUpdatePromptService
{
    public bool ConfirmUpdate(string targetVersion)
    {
        var dialog = new UpdateAvailableDialog(targetVersion);
        return dialog.ShowDialog() == true;
    }
}
