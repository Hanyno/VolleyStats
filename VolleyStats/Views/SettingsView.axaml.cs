using Avalonia.Controls;
using Avalonia.Interactivity;
using VolleyStats.Models;
using VolleyStats.ViewModels;

namespace VolleyStats.Views
{
    public partial class SettingsView : UserControl
    {
        public SettingsView()
        {
            InitializeComponent();
        }

        private SettingsViewModel? ViewModel => DataContext as SettingsViewModel;

        private Window? GetParentWindow() => TopLevel.GetTopLevel(this) as Window;

        private async void AddBindingButton_OnClick(object? sender, RoutedEventArgs e)
        {
            var parentWindow = GetParentWindow();
            if (parentWindow == null || ViewModel == null) return;

            var dialog = new AddKeyBindingWindow();
            var result = await dialog.ShowDialog<KeyboardShortcut?>(parentWindow);
            if (result != null)
                ViewModel.AddBinding(result);
        }
    }
}
