namespace TreeManager.App.Services;

public interface IUserJournalService
{
    void LogAction(string action, string personLabel = null);
}
