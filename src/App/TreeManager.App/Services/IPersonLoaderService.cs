using TreeManager.App.ViewModels;
using TreeManager.Core.Domain;

namespace TreeManager.App.Services;

public interface IPersonLoaderService
{
    MeFile Load(
        string meFilePath,
        string rootPath,
        PersonViewModel personVm,
        DatesTabViewModel datesVm,
        FamilyTabViewModel familyVm);
}
