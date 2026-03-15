using Avalonia.Controls;
using Avalonia.Interactivity;

namespace VolleyStats.Views
{
    public partial class EndSetConfirmWindow : Window
    {
        public EndSetConfirmWindow()
        {
            InitializeComponent();
        }

        public EndSetConfirmWindow(int setNumber, string homeTeam, string awayTeam, int homeScore, int awayScore)
        {
            InitializeComponent();
            TitleText.Text  = $"Set {setNumber}  —  {homeTeam} vs {awayTeam}";
            ScoreText.Text  = $"{homeScore} : {awayScore}";
        }

        private void ContinueButton_OnClick(object? sender, RoutedEventArgs e) => Close(false);
        private void EndSetButton_OnClick(object? sender, RoutedEventArgs e)   => Close(true);
    }
}
