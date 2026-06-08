using System.Windows;

namespace TreeManager.App.Views;

public partial class InfoDialog : Window
{
    public InfoDialog(string title, string message)
    {
        InitializeComponent();
        Title = title;
        MessageBlock.Text = message;
    }
}
