using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using VolleyStats.Data.Repositories;
using VolleyStats.Models;
using VolleyStats.ViewModels;

namespace VolleyStats.Views
{
    public partial class HomePageView : UserControl
    {
        private readonly TeamsRepository _teamsRepository = new();

        public HomePageView()
        {
            InitializeComponent();
            DataContextChanged += OnDataContextChanged;
        }

        private void OnDataContextChanged(object? sender, System.EventArgs e)
        {
            if (DataContext is HomePageViewModel vm)
            {
                vm.TriggerTeamChoiceDialog = ShowTeamChoiceDialogAsync;
                vm.TriggerNewSeasonDialog = ShowNewSeasonDialogAsync;
            }
        }

        private string FormatTeamDisplay(string name, string? code) =>
            string.IsNullOrEmpty(code) ? name : $"{name} ({code})";

        private HomePageViewModel? ViewModel => DataContext as HomePageViewModel;

        private Window? GetParentWindow()
        {
            return TopLevel.GetTopLevel(this) as Window;
        }

        private async Task<string?> ShowNewSeasonDialogAsync()
        {
            var parentWindow = GetParentWindow();
            if (parentWindow == null) return null;

            var dialog = new NewSeasonWindow();
            return await dialog.ShowDialog<string?>(parentWindow);
        }

        private async Task<string?> ShowTeamChoiceDialogAsync(string display1, string value1, string display2, string value2)
        {
            var parentWindow = GetParentWindow();
            if (parentWindow == null) return null;

            string? result = null;

            var dialog = new Window
            {
                Title = "Choose Team to Analyze",
                SizeToContent = SizeToContent.WidthAndHeight,
                MinWidth = 300,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                CanResize = false,
                Content = new StackPanel
                {
                    Margin = new Thickness(24, 20),
                    Spacing = 16,
                    Children =
                    {
                        new TextBlock
                        {
                            Text = "Which team do you want to analyze?",
                            TextWrapping = TextWrapping.Wrap,
                            FontSize = 13
                        },
                        new StackPanel
                        {
                            Orientation = Orientation.Horizontal,
                            HorizontalAlignment = HorizontalAlignment.Center,
                            Spacing = 12
                        }
                    }
                }
            };

            var btnPanel = (StackPanel)((StackPanel)dialog.Content).Children[1];
            var btn1 = new Button { Content = display1, Padding = new Thickness(18, 8), FontWeight = FontWeight.SemiBold };
            var btn2 = new Button { Content = display2, Padding = new Thickness(18, 8), FontWeight = FontWeight.SemiBold };

            btn1.Click += (_, _) => { result = value1; dialog.Close(); };
            btn2.Click += (_, _) => { result = value2; dialog.Close(); };

            btnPanel.Children.Add(btn1);
            btnPanel.Children.Add(btn2);

            await dialog.ShowDialog(parentWindow);
            return result;
        }

        private async void TeamsButton_OnClick(object? sender, RoutedEventArgs e)
        {
            var parentWindow = GetParentWindow();
            if (parentWindow == null) return;

            var teamsWindow = new TeamsWindow();
            var viewModel = new TeamsViewModel(_teamsRepository, teamsWindow, teamsWindow);

            teamsWindow.SetViewModel(viewModel);

            await viewModel.LoadTeamsCommand.ExecuteAsync(null);
            await teamsWindow.ShowDialog(parentWindow);

            if (ViewModel != null)
                await ViewModel.InitializeAsync();
        }

        private async void SelectTeamButton_OnClick(object? sender, RoutedEventArgs e)
        {
            var parentWindow = GetParentWindow();
            if (parentWindow == null || ViewModel == null) return;

            var picker = new TeamPickerWindow();
            var pickerViewModel = new TeamPickerViewModel(_teamsRepository);
            picker.DataContext = pickerViewModel;
            await pickerViewModel.LoadTeamsAsync();

            var selected = await picker.ShowDialog<Team?>(parentWindow);
            ViewModel.ApplyTeamFilter(selected?.Name, selected?.TeamCode);
        }

        private void ClearTeamFilterButton_OnClick(object? sender, RoutedEventArgs e)
        {
            ViewModel?.ApplyTeamFilter(null);
        }
    }
}
