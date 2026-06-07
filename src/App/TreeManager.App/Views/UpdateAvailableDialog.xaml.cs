using System.Windows;

namespace TreeManager.App.Views;

public partial class UpdateAvailableDialog : Window
{
    public UpdateAvailableDialog(string targetVersion)
    {
        InitializeComponent();
        VersionText.Text = "Nowa wersja: " + targetVersion;
    }

    private void YesButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
    }

    private void NoButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}
