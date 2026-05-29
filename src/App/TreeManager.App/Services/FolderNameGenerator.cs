using System;
using System.Text;
using TreeManager.App.ViewModels;

namespace TreeManager.App.Services;

public static class FolderNameGenerator
{
    private const string Unknown = "(nieznane)";

    public static string ToFolderName(this PersonViewModel vm)
    {
        ArgumentNullException.ThrowIfNull(vm);

        var sb = new StringBuilder();

        sb.Append(string.IsNullOrEmpty(vm.FirstName) ? Unknown : vm.FirstName);

        if (!string.IsNullOrEmpty(vm.OtherFirstNames))
        {
            sb.Append(' ');
            sb.Append(vm.OtherFirstNames);
        }

        sb.Append(' ');
        sb.Append(string.IsNullOrEmpty(vm.LastName) ? Unknown : vm.LastName);

        if (!string.IsNullOrEmpty(vm.OtherLastNames))
        {
            sb.Append(';');
            sb.Append(vm.OtherLastNames);
        }

        if (vm.HasMaidenName)
        {
            sb.Append(" zd. ");
            sb.Append(vm.MaidenName);

            if (!string.IsNullOrEmpty(vm.OtherMaidenNames))
            {
                sb.Append(';');
                sb.Append(vm.OtherMaidenNames);
            }
        }

        return sb.ToString();
    }
}
