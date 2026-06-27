namespace TreeManager.App.Services;

public interface IPromoteConfirmService
{
    bool Confirm(string summary, string title, string header);
}
