using System.Windows;
using TreeManager.App.ViewModels;

namespace TreeManager.App.Views;

public partial class PersonPickerDialog : Window
{
    public PersonPickerDialog()
    {
        InitializeComponent();
    }

    private void SelectButton_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is PersonPickerViewModel vm && vm.SelectedPerson != null)
        {
            DialogResult = true;
        }
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}
