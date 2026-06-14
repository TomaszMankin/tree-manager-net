using System.Collections.Generic;
using System.Windows;

namespace TreeManager.App.Views;

public partial class ValidationReportDialog : Window
{
    public ValidationReportDialog(IReadOnlyList<string> messages)
    {
        InitializeComponent();

        if (messages.Count == 0)
        {
            NoProblemsText.Visibility = Visibility.Visible;
        }
        else
        {
            IssuesTextBox.Text = string.Join("\n", messages);
            IssuesTextBox.Visibility = Visibility.Visible;
        }
    }
}
