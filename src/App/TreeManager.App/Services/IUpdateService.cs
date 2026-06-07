using System.Threading.Tasks;

namespace TreeManager.App.Services;

public interface IUpdateService
{
    Task<UpdateCheckResult> CheckAsync();
    Task DownloadAndApplyAsync();
}
