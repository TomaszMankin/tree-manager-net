using System.Collections.Generic;

namespace TreeManager.App.Services;

public interface IValidationReportService
{
    void Show(IReadOnlyList<string> messages);
}
