using System.Diagnostics;

namespace TreeManager.App.Services;

public sealed class FolderRevealService : IFolderRevealService
{
    public void Reveal(string folderPath)
    {
        Process.Start("explorer.exe", folderPath);
    }
}
