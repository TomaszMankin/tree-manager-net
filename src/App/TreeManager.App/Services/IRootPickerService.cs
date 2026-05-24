namespace TreeManager.App.Services;

public interface IRootPickerService
{
    /// <summary>Shows a folder picker dialog. Returns the selected folder path, or empty string when user cancels.</summary>
    string PickRoot();
}
