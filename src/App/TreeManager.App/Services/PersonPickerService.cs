using System.Collections.Generic;
using TreeManager.App.ViewModels;
using TreeManager.App.Views;
using TreeManager.Core.Domain;

namespace TreeManager.App.Services;

public sealed class PersonPickerService : IPersonPickerService
{
    public PersonSummary PickPerson(IReadOnlyList<PersonSummary> people)
    {
        var vm = new PersonPickerViewModel();
        vm.LoadPeople(people);
        var dialog = new PersonPickerDialog { DataContext = vm };
        var result = dialog.ShowDialog();
        return result == true ? vm.SelectedPerson : null;
    }
}
