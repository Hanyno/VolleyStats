using Avalonia.Controls;
using Avalonia.Interactivity;

namespace VolleyStats.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void TeamsButton_OnClick(object? sender, RoutedEventArgs e)
        {
            var win = new TeamsWindow();
            win.ShowDialog(this);
        }
    }
}