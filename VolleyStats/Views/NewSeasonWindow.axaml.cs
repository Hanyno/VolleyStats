using System.IO;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace VolleyStats.Views
{
    public partial class NewSeasonWindow : Window
    {
        public NewSeasonWindow()
        {
            InitializeComponent();
        }

        private void CancelButton_OnClick(object? sender, RoutedEventArgs e)
        {
            Close(null);
        }

        private void CreateButton_OnClick(object? sender, RoutedEventArgs e)
        {
            var name = SeasonNameBox.Text?.Trim();

            if (string.IsNullOrWhiteSpace(name))
            {
                ErrorText.Text = "Season name cannot be empty.";
                ErrorText.IsVisible = true;
                return;
            }

            foreach (var ch in Path.GetInvalidFileNameChars())
            {
                if (name.Contains(ch))
                {
                    ErrorText.Text = "Season name contains invalid characters.";
                    ErrorText.IsVisible = true;
                    return;
                }
            }

            Close(name);
        }
    }
}
