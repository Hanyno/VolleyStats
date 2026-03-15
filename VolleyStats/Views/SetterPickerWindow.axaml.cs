using Avalonia.Controls;
using Avalonia.Interactivity;
using VolleyStats.ViewModels;

namespace VolleyStats.Views
{
    public partial class SetterPickerWindow : Window
    {
        public SetterPickerWindow()
        {
            InitializeComponent();
        }

        public SetterPickerWindow(SetterPickerViewModel vm)
        {
            InitializeComponent();
            DataContext = vm;
        }

        private void CancelButton_OnClick(object? sender, RoutedEventArgs e) => Close(null);

        private void ConfirmButton_OnClick(object? sender, RoutedEventArgs e)
        {
            if (DataContext is SetterPickerViewModel vm && vm.Selected != null)
                Close(vm.Selected.JerseyNumber);
            else
                Close(null);
        }
    }
}
