using Avalonia.Controls;
using Avalonia.Interactivity;

namespace VolleyStats.Views
{
    public partial class MatchChoiceDialog : Window
    {
        public MatchChoiceDialog()
        {
            InitializeComponent();
        }

        private void ChooseMatch_OnClick(object? sender, RoutedEventArgs e)
        {
            Close(true);
        }

        private void CreateMatch_OnClick(object? sender, RoutedEventArgs e)
        {
            Close(false);
        }
    }
}
