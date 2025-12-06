using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using VolleyStats.Services;
using VolleyStats.Views;

namespace VolleyStats;

public partial class HomeView : UserControl
{
    private readonly ITeamsService _teamsService;
    private readonly IOfficialStatsService _officialStatsService;

    public HomeView(ITeamsService teamsService, IOfficialStatsService officialStatsService)
    {
        _teamsService = teamsService;
        _officialStatsService = officialStatsService;
        InitializeComponent();
    }

    private Window? GetParentWindow()
    {
        return VisualRoot as Window;
    }

    private void TeamsButton_OnClick(object? sender, RoutedEventArgs e)
    {
        var owner = GetParentWindow();
        if (owner is null)
            return;

        var win = new TeamsWindow(_teamsService, _officialStatsService);
        win.ShowDialog(owner);
    }

    private async void ScoutMatchButton_OnClick(object? sender, RoutedEventArgs e)
    {
        var owner = GetParentWindow();
        if (owner is null)
            return;

        var dialog = new MatchChoiceDialog();

        var result = await dialog.ShowDialog<bool?>(owner);

        if (result == true)
        {
            var matchesWindow = new MatchesWindow(_officialStatsService);
            matchesWindow.Show(owner);
        }
        else if (result == false)
        {
            var creationWindow = new CreationMatchWindow(_officialStatsService);
            creationWindow.Show(owner);
        }
    }

    private void AnalysisButton_OnClick(object? sender, RoutedEventArgs e)
    {
        // TODO
    }
}
