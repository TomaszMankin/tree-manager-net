using System.Windows;

namespace TreeManager.App.Views;

public partial class UnsavedChangesDialog : Window
{
    public UnsavedChangesDialog()
    {
        InitializeComponent();
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
