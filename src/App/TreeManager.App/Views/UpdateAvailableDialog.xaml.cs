using System.Windows;

namespace TreeManager.App.Views;

public partial class UpdateAvailableDialog : Window
{
    public UpdateAvailableDialog(string targetVersion)
    {
        InitializeComponent();
        VersionText.Text = "Nowa wersja: " + targetVersion;
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
