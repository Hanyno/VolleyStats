using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
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

        private async void BrowseFfmpegButton_OnClick(object? sender, RoutedEventArgs e)
        {
            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel == null || ViewModel == null) return;

            var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Select FFmpeg executable",
                AllowMultiple = false,
                FileTypeFilter = new[]
                {
                    new FilePickerFileType("Executable") { Patterns = new[] { "ffmpeg.exe", "ffmpeg" } },
                    new FilePickerFileType("All files") { Patterns = new[] { "*" } }
                }
            });

            var file = files.FirstOrDefault();
            if (file != null)
                ViewModel.FfmpegPath = file.Path.LocalPath;
        }
    }
}
