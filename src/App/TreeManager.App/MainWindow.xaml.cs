using System.Windows;
using TreeManager.App.ViewModels;

namespace TreeManager.App;

public partial class MainWindow : Window
{
    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
