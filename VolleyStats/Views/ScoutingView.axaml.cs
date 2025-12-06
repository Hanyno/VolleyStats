using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using VolleyStats.Domain;
using VolleyStats.Enums;
using VolleyStats.Services;
using VolleyStats.Views;

namespace VolleyStats;

public partial class ScoutingView : UserControl
{
    private readonly IOfficialStatsService _officialStatsService;
    private readonly Match _match;

    private bool _setActive;
    private int _homeScore;
    private int _awayScore;

    private int _homeSets;
    private int _awaySets;

    private MatchSet? _currentSet;
    private readonly List<MatchEvent> _currentRallyEvents = new();

    private readonly ObservableCollection<string> _rallies = new();

    public ScoutingView(Match match, IOfficialStatsService officialStatsService)
    {
        _officialStatsService = officialStatsService;
        InitializeComponent();
        _match = match;

        _officialStatsService.LoadMatchStatistics(_match);

        HomeTeamNameTextBlock.Text = _match.HomeTeam?.Name ?? "Home team";
        AwayTeamNameTextBlock.Text = _match.AwayTeam?.Name ?? "Away team";

        MatchInfoTextBlock.Text =
            $"{_match.StartTime:dd.MM.yyyy HH:mm}  -  {HomeTeamNameTextBlock.Text} vs {AwayTeamNameTextBlock.Text}";

        ServingTeamComboBox.ItemsSource = new[] { TeamSide.Home, TeamSide.Away };
        RallyWinnerComboBox.ItemsSource = new[] { TeamSide.Home, TeamSide.Away };

        ActionTypeComboBox.ItemsSource = Enum.GetValues(typeof(BasicSkill));
        ActionRatingComboBox.ItemsSource = Enum.GetValues(typeof(EvaluationSymbol));

        ActionTypeComboBox.SelectedItem = BasicSkill.Serve;
        ActionRatingComboBox.SelectedItem = EvaluationSymbol.Unknown;

        RalliesListBox.ItemsSource = _rallies;

        foreach (var set in _match.Sets)
        {
            if (set.HomeScore > set.AwayScore)
                _homeSets++;
            else if (set.AwayScore > set.HomeScore)
                _awaySets++;
        }

        if (_match.Sets.Count > 0)
        {
            var lastSet = _match.Sets[^1];

            _homeScore = lastSet.HomeScore;
            _awayScore = lastSet.AwayScore;

            _rallies.Clear();

            foreach (var rally in lastSet.Rallies)
            {
                var winner = rally.HomeScoreAfter > rally.AwayScoreAfter
                    ? _match.HomeTeam.Name
                    : _match.AwayTeam.Name;

                _rallies.Add(
                    $"Výměna #{rally.SequenceNumber}: " +
                    $"{rally.HomeScoreBefore}-{rally.AwayScoreBefore} → " +
                    $"{rally.HomeScoreAfter}-{rally.AwayScoreAfter}, bod – {winner}");
            }
        }

        UpdateScoreDisplay();
        SetInputsEnabled(false);
    }

    private void UpdateScoreDisplay()
    {
        HomeTeamScoreTextBlock.Text = _homeScore.ToString();
        AwayTeamScoreTextBlock.Text = _awayScore.ToString();
    }

    private void SetInputsEnabled(bool enabled)
    {
        ServingTeamComboBox.IsEnabled = enabled;
        ActionTypeComboBox.IsEnabled = enabled;
        ActionRatingComboBox.IsEnabled = enabled;
        AddActionButton.IsEnabled = enabled;
        RallyWinnerComboBox.IsEnabled = enabled;
        EndRallyButton.IsEnabled = enabled;

        if (!enabled)
        {
            PlayerComboBox.IsEnabled = false;
            PlayerComboBox.ItemsSource = null;
            PlayerComboBox.SelectedIndex = -1;
        }
        else
        {
            UpdatePlayerComboBox();
        }
    }

    private void HomeButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (VisualRoot is MainWindow mainWindow)
        {
            SaveMatch(finished: false);
            mainWindow.ShowHome();
        }
    }

    private void StartSetButton_OnClick(object? sender, RoutedEventArgs e)
    {
        _setActive = true;
        SetInputsEnabled(true);
        StartSetButton.IsEnabled = false;

        _homeScore = 0;
        _awayScore = 0;
        UpdateScoreDisplay();
        _rallies.Clear();
        _currentRallyEvents.Clear();

        var setNumber = _match.Sets.Count + 1;

        _currentSet = new MatchSet
        {
            Match = _match,
            Number = setNumber,
            HomeScore = 0,
            AwayScore = 0
        };

        _match.Sets.Add(_currentSet);
    }

    // ----------------------------------------------
    // PŘIDÁNÍ AKCE DO AKTUÁLNÍ VÝMĚNY
    // ----------------------------------------------
    private void AddActionButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (!_setActive || _currentSet == null)
            return;

        if (ServingTeamComboBox.SelectedItem is not TeamSide servingSide)
            return;

        if (PlayerComboBox.SelectedItem is not Player selectedPlayer)
            return;

        var playerNumber = selectedPlayer.JerseyNumber;

        var skill = ActionTypeComboBox.SelectedItem is BasicSkill s ? s : BasicSkill.Unknown;
        var eval = ActionRatingComboBox.SelectedItem is EvaluationSymbol ev ? ev : EvaluationSymbol.Unknown;

        _rallies.Add($"Hráč {playerNumber} – {skill} ({eval}), podávající: {GetTeamName(servingSide)}");

        var evnt = new MatchEvent
        {
            Set = _currentSet,
            OrderInRally = _currentRallyEvents.Count + 1,
            Side = servingSide,
            Skill = skill,
            Eval = eval,
            RawCode = $"{skill} {(char)eval} #{playerNumber}"
        };

        _currentRallyEvents.Add(evnt);
    }

    // ----------------------------------------------
    // UKONČENÍ VÝMĚNY
    // ----------------------------------------------
    private async void EndRallyButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (!_setActive || _currentSet == null)
            return;

        if (RallyWinnerComboBox.SelectedItem is not TeamSide winnerSide)
            return;
        if (ServingTeamComboBox.SelectedItem is not TeamSide servingSide)
            return;

        var homeBefore = _homeScore;
        var awayBefore = _awayScore;

        if (winnerSide == TeamSide.Home)
            _homeScore++;
        else
            _awayScore++;

        var rally = new Rally
        {
            Set = _currentSet,
            SequenceNumber = _currentSet.Rallies.Count + 1,
            ServingSide = servingSide,
            HomeScoreBefore = homeBefore,
            AwayScoreBefore = awayBefore,
            HomeScoreAfter = _homeScore,
            AwayScoreAfter = _awayScore
        };

        foreach (var ev in _currentRallyEvents)
        {
            ev.Rally = rally;
            ev.Set = _currentSet;
            rally.Events.Add(ev);
        }

        _currentRallyEvents.Clear();
        _currentSet.Rallies.Add(rally);

        _currentSet.HomeScore = _homeScore;
        _currentSet.AwayScore = _awayScore;

        _rallies.Add($"Výměna #{_rallies.Count + 1}: bod – {GetTeamName(winnerSide)}");

        UpdateScoreDisplay();

        PlayerComboBox.SelectedIndex = -1;
        ActionTypeComboBox.SelectedIndex = -1;
        ActionRatingComboBox.SelectedIndex = -1;
        RallyWinnerComboBox.SelectedIndex = -1;

        await CheckSetEndAsync();
    }

    /// <summary>
    /// Kontrola podmínky pro ukončení setu:
    /// - jeden tým má alespoň 25 bodů
    /// - a vede o 2 a více bodů
    /// </summary>
    private async Task CheckSetEndAsync()
    {
        if (!_setActive)
            return;

        int leadingScore = Math.Max(_homeScore, _awayScore);
        int trailingScore = Math.Min(_homeScore, _awayScore);

        if (leadingScore >= 25 && leadingScore - trailingScore >= 2)
        {
            bool confirm = await ShowEndSetDialogAsync();
            if (!confirm)
                return;

            bool homeWonSet = _homeScore > _awayScore;

            if (homeWonSet)
                _homeSets++;
            else
                _awaySets++;

            bool matchFinished = _homeSets == 3 || _awaySets == 3;

            SaveMatch(finished: matchFinished);
            EndCurrentSet();

            if (matchFinished)
                StartSetButton.IsEnabled = false;
        }
    }

    private void EndCurrentSet()
    {
        _setActive = false;
        SetInputsEnabled(false);
        StartSetButton.IsEnabled = true;

        _homeScore = 0;
        _awayScore = 0;
        UpdateScoreDisplay();
        _rallies.Clear();
        _currentRallyEvents.Clear();
        _currentSet = null;
    }

    private void SaveMatch(bool finished)
    {
        _match.IsFinished = finished;
        _officialStatsService.SaveMatchStatistics(_match);

    }

    private async Task<bool> ShowEndSetDialogAsync()
    {
        if (VisualRoot is not Window owner)
            return false;

        var tcs = new TaskCompletionSource<bool>();
        var dlg = new Window
        {
            Title = "End set?",
            Width = 300,
            Height = 130,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false
        };

        var yesButton = new Button { Content = "Yes", Width = 80, Margin = new Thickness(5) };
        var noButton = new Button { Content = "No", Width = 80, Margin = new Thickness(5) };

        yesButton.Click += (_, _) =>
        {
            tcs.TrySetResult(true);
            dlg.Close();
        };

        noButton.Click += (_, _) =>
        {
            tcs.TrySetResult(false);
            dlg.Close();
        };

        dlg.Content = new StackPanel
        {
            Margin = new Thickness(10),
            Children =
            {
                new TextBlock {
                    Text = "End set?",
                    Margin = new Thickness(0,0,0,10),
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center
                },
                new StackPanel
                {
                    Orientation = Avalonia.Layout.Orientation.Horizontal,
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                    Children = { yesButton, noButton }
                }
            }
        };

        await dlg.ShowDialog(owner);
        return await tcs.Task;
    }

    // -----------------------------------------------------
    // helper
    // -----------------------------------------------------
    private Team GetTeamBySide(TeamSide side)
        => side == TeamSide.Home ? _match.HomeTeam : _match.AwayTeam;

    private string GetTeamName(TeamSide side)
        => side == TeamSide.Home ? HomeTeamNameTextBlock.Text : AwayTeamNameTextBlock.Text;

    private void ServingTeamComboBox_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (!_setActive)
            return;

        UpdatePlayerComboBox();
    }
    private void UpdatePlayerComboBox()
    {
        if (ServingTeamComboBox.SelectedItem is not TeamSide side)
        {
            PlayerComboBox.ItemsSource = null;
            PlayerComboBox.IsEnabled = false;
            PlayerComboBox.SelectedIndex = -1;
            return;
        }

        var team = GetTeamBySide(side);
        if (team?.Players == null || team.Players.Count == 0)
        {
            PlayerComboBox.ItemsSource = null;
            PlayerComboBox.IsEnabled = false;
            PlayerComboBox.SelectedIndex = -1;
            return;
        }

        PlayerComboBox.ItemsSource = team.Players;
        PlayerComboBox.IsEnabled = true;
        PlayerComboBox.SelectedIndex = -1;
    }
}
