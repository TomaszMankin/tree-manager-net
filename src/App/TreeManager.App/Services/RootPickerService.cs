using Microsoft.Win32;

namespace TreeManager.App.Services;

public sealed class RootPickerService : IRootPickerService
{
    public string PickRoot()
    {
        var dialog = new OpenFolderDialog
        {
            Title = "Wybierz katalog drzewa",
            Multiselect = false
        };
        return dialog.ShowDialog() == true ? dialog.FolderName : string.Empty;
    }
}
