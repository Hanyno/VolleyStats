using Avalonia.Controls;
using Avalonia.Interactivity;
using VolleyStats.ViewModels;
using VolleyStats.Domain;

namespace VolleyStats.Views
{
    public partial class TeamPickerWindow : Window
    {
        public TeamPickerWindow()
        {
            InitializeComponent();
        }

        private void CancelButton_OnClick(object? sender, RoutedEventArgs e)
        {
            Close(null);
        }

        private void ConfirmButton_OnClick(object? sender, RoutedEventArgs e)
        {
            if (DataContext is TeamPickerViewModel vm)
            {
                Close(vm.SelectedTeam as Team);
            }
            else
            {
                Close(null);
            }
        }
    }
}
