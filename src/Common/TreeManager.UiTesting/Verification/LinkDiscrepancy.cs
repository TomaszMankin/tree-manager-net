namespace TreeManager.UiTesting.Verification;

/// <summary>A single .lnk edge present on one side of the compare but not the other.</summary>
public sealed class LinkDiscrepancy
{
    public LinkDiscrepancy(string personFolderName, string subfolder, string targetFolderName)
    {
        PersonFolderName = personFolderName;
        Subfolder = subfolder;
        TargetFolderName = targetFolderName;
    }

    public string PersonFolderName { get; }

    public string Subfolder { get; }

    public string TargetFolderName { get; }

    public override string ToString()
    {
        return PersonFolderName + "/" + Subfolder + "/" + TargetFolderName + ".lnk";
    }
}
