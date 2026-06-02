using System.Collections.Generic;
using TreeManager.Core.Domain;

namespace TreeManager.App.Services;

public interface IPersonPickerService
{
    PersonSummary PickPerson(IReadOnlyList<PersonSummary> people);
}
