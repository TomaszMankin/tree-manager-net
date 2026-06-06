using System;
using System.Linq;
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

        if (vm.OtherFirstNames.Items.Count > 0)
        {
            sb.Append(' ');
            sb.Append(string.Join(";", vm.OtherFirstNames.Items));
        }

        sb.Append(' ');
        sb.Append(string.IsNullOrEmpty(vm.LastName) ? Unknown : vm.LastName);

        if (vm.OtherLastNames.Items.Count > 0)
        {
            sb.Append(';');
            sb.Append(string.Join(";", vm.OtherLastNames.Items));
        }

        if (vm.HasMaidenName)
        {
            sb.Append(" zd. ");
            sb.Append(vm.MaidenName);

            if (vm.OtherMaidenNames.Items.Count > 0)
            {
                sb.Append(';');
                sb.Append(string.Join(";", vm.OtherMaidenNames.Items));
            }
        }

        return sb.ToString();
    }
}
