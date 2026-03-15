using Avalonia.Controls;
using Avalonia.Interactivity;

namespace VolleyStats.Views
{
    public partial class EditCodeWindow : Window
    {
        public EditCodeWindow()
        {
            InitializeComponent();
        }

        public EditCodeWindow(string currentRawCode)
        {
            InitializeComponent();
            RawCodeTextBox.Text = currentRawCode;
        }

        private void CancelButton_OnClick(object? sender, RoutedEventArgs e)
        {
            Close(null);
        }

        private void SaveButton_OnClick(object? sender, RoutedEventArgs e)
        {
            Close(RawCodeTextBox.Text ?? string.Empty);
        }
    }
}
