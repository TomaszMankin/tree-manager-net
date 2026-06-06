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
            IssuesList.ItemsSource = messages;
            IssuesList.Visibility = Visibility.Visible;
        }
    }
}
