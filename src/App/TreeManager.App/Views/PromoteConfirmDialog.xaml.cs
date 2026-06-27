using System.Windows;

namespace TreeManager.App.Views;

public partial class PromoteConfirmDialog : Window
{
    public PromoteConfirmDialog(string summary, string title, string header)
    {
        InitializeComponent();
        Title = title;
        HeaderBlock.Text = header;
        SummaryBlock.Text = summary;
    }

    private void TakButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
    }

    private void NieButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}
