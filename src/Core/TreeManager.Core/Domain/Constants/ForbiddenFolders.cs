namespace TreeManager.Core.Domain.Constants;

public static class ForbiddenFolders
{
    public const string PeopleListFolderName = "Lista osób";

    public static readonly IReadOnlySet<string> Default = new HashSet<string>(StringComparer.Ordinal)
    {
        "Pozostałe nieuporządkowane",
        "Rutowscy - dane ogólne",
        "Do ustalenia",
        "Wspólne",
    };
}
