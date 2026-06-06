using System.Collections.Generic;
using TreeManager.App.Views;

namespace TreeManager.App.Services;

public sealed class ValidationReportService : IValidationReportService
{
    public void Show(IReadOnlyList<string> messages)
    {
        var dialog = new ValidationReportDialog(messages);
        dialog.ShowDialog();
    }
}
