using Avalonia.Controls;
using Avalonia.Interactivity;
using VolleyStats.ViewModels;

namespace VolleyStats.Views
{
    public partial class SubstitutionWindow : Window
    {
        public SubstitutionWindow()
        {
            InitializeComponent();
        }

        public SubstitutionWindow(SubstitutionViewModel vm)
        {
            InitializeComponent();
            DataContext = vm;
        }

        private void CancelButton_OnClick(object? sender, RoutedEventArgs e)
            => Close(null);

        private void ConfirmButton_OnClick(object? sender, RoutedEventArgs e)
        {
            if (DataContext is SubstitutionViewModel vm
                && vm.SelectedCourt != null
                && vm.SelectedBench != null)
            {
                Close((vm.SelectedCourt.JerseyNumber, vm.SelectedBench.JerseyNumber));
            }
            else
            {
                Close(null);
            }
        }
    }
}
