using System;
using System.Linq;
using TreeManager.App.ViewModels;
using TreeManager.Core.Domain;

namespace TreeManager.App.Mappers;

public static class FamilyTabViewModelMapper
{
    public static MeFile ToMeFile(this FamilyTabViewModel vm, MeFile existing = null)
    {
        ArgumentNullException.ThrowIfNull(vm);

        var baseFile = existing ?? new MeFile();

        return baseFile with
        {
            ParentsId = vm.Parents.Selected.Select(p => p.UniqueIdentifier).ToList(),
            Parents = vm.Parents.Selected.Select(p => p.DisplayName).ToList(),
            ChildrenId = vm.Children.Selected.Select(p => p.UniqueIdentifier).ToList(),
            Children = vm.Children.Selected.Select(p => p.DisplayName).ToList(),
            SpouseId = vm.Spouses.Selected.Select(p => p.UniqueIdentifier).ToList(),
            Spouse = vm.Spouses.Selected.Select(p => p.DisplayName).ToList(),
            SiblingsId = vm.Siblings.Selected.Select(p => p.UniqueIdentifier).ToList(),
            Siblings = vm.Siblings.Selected.Select(p => p.DisplayName).ToList(),
        };
    }
}
