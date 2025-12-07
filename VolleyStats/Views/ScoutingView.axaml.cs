using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using VolleyStats.Domain;
using VolleyStats.Enums;
using VolleyStats.Services;
using VolleyStats.Views;

namespace VolleyStats
{
    public partial class ScoutingView : UserControl
    {
        private readonly IOfficialStatsService _officialStatsService;
        private readonly Match _match;
        private readonly Dictionary<int, MatchPlayer> _matchPlayers = new();

        private bool _setActive;
        private int _homeScore;
        private int _awayScore;

        private int _homeSets;
        private int _awaySets;

        private MatchSet? _currentSet;
        private readonly List<MatchEvent> _currentRallyEvents = new();
        private readonly List<MatchEvent?> _rallyLineEvents = new();


        private readonly ObservableCollection<string> _rallies = new();

        public ScoutingView(Match match, IOfficialStatsService officialStatsService)
        {
            _officialStatsService = officialStatsService;
            InitializeComponent();
            _match = match;

            _officialStatsService.LoadMatchStatistics(_match);

            FixPlayersForAllEvents();

            HomeTeamNameTextBlock.Text = _match.HomeTeam?.Name ?? "Home team";
            AwayTeamNameTextBlock.Text = _match.AwayTeam?.Name ?? "Away team";

            MatchInfoTextBlock.Text =
                $"{_match.StartTime:dd.MM.yyyy HH:mm}  -  {HomeTeamNameTextBlock.Text} vs {AwayTeamNameTextBlock.Text}";

            ServingTeamComboBox.ItemsSource = new[] { TeamSide.Home, TeamSide.Away };
            RallyWinnerComboBox.ItemsSource = new[] { TeamSide.Home, TeamSide.Away };

            ActionTypeComboBox.ItemsSource = Enum.GetValues(typeof(BasicSkill));
            ActionRatingComboBox.ItemsSource = new[] { "#", "+", "!", "-", "/", "=" };
            ActionRatingComboBox.SelectedIndex = -1;
            ActionTypeComboBox.SelectedItem = BasicSkill.Serve;

            RalliesListBox.ItemsSource = _rallies;

            foreach (var set in _match.Sets)
            {
                int maxScore = Math.Max(set.HomeScore, set.AwayScore);
                int diff = Math.Abs(set.HomeScore - set.AwayScore);

                if (maxScore >= 25 && diff >= 2)
                {
                    if (set.HomeScore > set.AwayScore)
                        _homeSets++;
                    else
                        _awaySets++;
                }
            }

            InitializeCurrentSetState();

            UpdateScoreDisplay();
            SetInputsEnabled(false);
        }

        private async void RalliesListBox_OnDoubleTapped(object? sender, TappedEventArgs e)
        {
            var index = RalliesListBox.SelectedIndex;
            if (index < 0 || index >= _rallyLineEvents.Count)
                return;

            var ev = _rallyLineEvents[index];
            if (ev == null)
                return;

            if (VisualRoot is not Window owner)
                return;

            var team = GetTeamBySide(ev.Side);

            var result = await ShowEditActionDialogAsync(owner, team, ev);

            if (!result)
                return;

            var jersey = ev.Player?.Player.JerseyNumber ?? 0;
            var evalChar = MapEvaluationSymbolChar(ev.Eval);

            _rallies[index] = $"{jersey}{ev.Skill}{evalChar}";
        }

        private void InitializeCurrentSetState()
        {
            if (_match.Sets.Count == 0)
            {
                StartSetButton.Content = "Start set";
                _currentSet = null;
                _homeScore = 0;
                _awayScore = 0;
                _rallies.Clear();
                _rallyLineEvents.Clear();
                return;
            }

            var unfinished = GetLastUnfinishedSet();

            if (unfinished == null)
            {
                _currentSet = null;
                _homeScore = 0;
                _awayScore = 0;
                _rallies.Clear();
                _rallyLineEvents.Clear();
                StartSetButton.Content = "Start set";
                UpdateSetExtrasDisplay();
            }
            else
            {
                _currentSet = unfinished;
                _homeScore = unfinished.HomeScore;
                _awayScore = unfinished.AwayScore;

                RebuildRalliesFromSet(unfinished);
                StartSetButton.Content = "Resume set";
                UpdateSetExtrasDisplay();
            }
        }



        private void RebuildRalliesFromSet(MatchSet set)
        {
            _rallies.Clear();
            _rallyLineEvents.Clear();

            foreach (var rally in set.Rallies.OrderBy(r => r.SequenceNumber))
            {
                foreach (var ev in rally.Events.OrderBy(e => e.OrderInRally))
                {
                    EnsureEventPlayerFromRawCode(ev);

                    var jersey = ev.Player?.Player.JerseyNumber.ToString() ?? "?";
                    var evalChar = MapEvaluationSymbolChar(ev.Eval);
                    _rallies.Add($"{jersey}{ev.Skill}{evalChar}");
                    _rallyLineEvents.Add(ev);
                }

                var winnerSide = rally.HomeScoreAfter > rally.AwayScoreAfter
                    ? TeamSide.Home
                    : TeamSide.Away;

                _rallies.Add($"Rally #{rally.SequenceNumber}: point – {GetTeamName(winnerSide)}");
                _rallyLineEvents.Add(null);
            }
        }


        private static string MapEvaluationSymbolChar(EvaluationSymbol eval) =>
            eval switch
            {
                EvaluationSymbol.Point => "#",
                EvaluationSymbol.Positive => "+",
                EvaluationSymbol.Good => "!",
                EvaluationSymbol.Poor => "-",
                EvaluationSymbol.Over => "/",
                EvaluationSymbol.Error => "=",
                _ => ""
            };

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
                SaveMatch();
                mainWindow.ShowHome();
            }
        }

        private void StartSetButton_OnClick(object? sender, RoutedEventArgs e)
        {
            _setActive = true;
            SetInputsEnabled(true);
            StartSetButton.IsEnabled = false;

            if (_currentSet == null || IsSetFinished(_currentSet))
            {
                _homeScore = 0;
                _awayScore = 0;
                UpdateScoreDisplay();
                _rallies.Clear();
                _currentRallyEvents.Clear();
                _rallyLineEvents.Clear();
                var setNumber = _match.Sets.Count + 1;

                _currentSet = new MatchSet
                {
                    Match = _match,
                    Number = setNumber,
                    HomeScore = 0,
                    AwayScore = 0,
                    HomeTimeouts = 0,
                    AwayTimeouts = 0,
                    HomeSubstitutions = 0,
                    AwaySubstitutions = 0
                };

                _match.Sets.Add(_currentSet);
                UpdateSetExtrasDisplay();

            }
            else
            {
                _currentRallyEvents.Clear();
            }
        }

        private void AddActionButton_OnClick(object? sender, RoutedEventArgs e)
        {
            if (!_setActive || _currentSet == null)
                return;

            if (ServingTeamComboBox.SelectedItem is not TeamSide teamSide)
                return;

            if (PlayerComboBox.SelectedItem is not Player selectedPlayer)
                return;

            var playerNumber = selectedPlayer.JerseyNumber;

            var skill = ActionTypeComboBox.SelectedItem is BasicSkill s ? s : BasicSkill.Unknown;
            var eval = ActionRatingComboBox.SelectedItem;

            _rallies.Add($"{playerNumber}{skill}{eval}");

            EvaluationSymbol evalSymbol = eval switch
            {
                "#" => EvaluationSymbol.Point,
                "+" => EvaluationSymbol.Positive,
                "!" => EvaluationSymbol.Good,
                "-" => EvaluationSymbol.Poor,
                "/" => EvaluationSymbol.Over,
                "=" => EvaluationSymbol.Error,
                _ => EvaluationSymbol.Error
            };

            string teamSideStr = teamSide == TeamSide.Home ? "*" : "a";

            string skillStr = skill switch
            {
                BasicSkill.Serve => "S",
                BasicSkill.Reception => "R",
                BasicSkill.Set => "E",
                BasicSkill.Attack => "A",
                BasicSkill.Block => "B",
                BasicSkill.Dig => "D",
                BasicSkill.FreeBall => "F",
                _ => "A"
            };

            var matchPlayer = GetOrCreateMatchPlayer(teamSide, selectedPlayer);

            var evnt = new MatchEvent
            {
                Set = _currentSet,
                OrderInRally = _currentRallyEvents.Count + 1,
                Side = teamSide,
                Player = matchPlayer,
                Skill = skill,
                Eval = evalSymbol,
                RawCode = $"{teamSideStr}{playerNumber}{skillStr}{eval}"
            };

            _currentRallyEvents.Add(evnt);
            _rallyLineEvents.Add(evnt);
        }

        private async void EndRallyButton_OnClick(object? sender, RoutedEventArgs e)
        {
            if (!_setActive || _currentSet == null)
                return;

            if (RallyWinnerComboBox.SelectedItem is not TeamSide winnerSide)
                return;
            if (ServingTeamComboBox.SelectedItem is not TeamSide teamSide)
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
                ServingSide = teamSide,
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

            _rallies.Add($"Rally #{_rallies.Count + 1}: point – {GetTeamName(winnerSide)}");
            _rallyLineEvents.Add(null);
            UpdateScoreDisplay();

            PlayerComboBox.SelectedIndex = -1;
            ActionTypeComboBox.SelectedIndex = -1;
            ActionRatingComboBox.SelectedIndex = -1;
            RallyWinnerComboBox.SelectedIndex = -1;

            await CheckSetEndAsync();
        }

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

                bool matchFinished = _homeSets >= 3 || _awaySets >= 3;

                SaveMatch();
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
            _rallyLineEvents.Clear();
            _currentSet = null;
            UpdateSetExtrasDisplay();
        }

        private void SaveMatch()
        {
            _homeSets = 0;
            _awaySets = 0;

            foreach (var set in _match.Sets)
            {
                if (!IsSetFinished(set))
                    continue;

                if (set.HomeScore > set.AwayScore)
                    _homeSets++;
                else if (set.AwayScore > set.HomeScore)
                    _awaySets++;
            }

            _match.IsFinished = _homeSets >= 3 || _awaySets >= 3;

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

        private MatchPlayer GetOrCreateMatchPlayer(TeamSide side, Player player)
        {
            var key = player.Id;

            if (_matchPlayers.TryGetValue(key, out var existing))
                return existing;

            var team = GetTeamBySide(side);

            var mp = new MatchPlayer
            {
                Match = _match,
                Team = team,
                Player = player
            };

            _match.Players.Add(mp);
            _matchPlayers[key] = mp;

            return mp;
        }

        private void EnsureEventPlayerFromRawCode(MatchEvent ev)
        {
            if (ev.Player != null)
                return;

            if (string.IsNullOrWhiteSpace(ev.RawCode))
                return;

            var raw = ev.RawCode;
            if (raw.Length < 3)
                return;

            var sideChar = raw[0];
            var side = sideChar == '*' ? TeamSide.Home : TeamSide.Away;

            int i = 1;
            while (i < raw.Length && char.IsDigit(raw[i]))
                i++;

            if (i == 1)
                return;

            var jerseyStr = raw.Substring(1, i - 1);
            if (!int.TryParse(jerseyStr, out var jerseyNumber))
                return;

            var team = GetTeamBySide(side);
            var player = team.Players.FirstOrDefault(p => p.JerseyNumber == jerseyNumber);
            if (player == null)
                return;

            var mp = GetOrCreateMatchPlayer(side, player);
            ev.Player = mp;
            ev.Side = side;
        }

        private static bool IsSetFinished(MatchSet set)
        {
            int maxScore = Math.Max(set.HomeScore, set.AwayScore);
            int diff = Math.Abs(set.HomeScore - set.AwayScore);
            return maxScore >= 25 && diff >= 2;
        }

        private async Task<bool> ShowEditActionDialogAsync(Window owner, Team team, MatchEvent ev)
        {
            var tcs = new TaskCompletionSource<bool>();

            var dlg = new Window
            {
                Title = "Edit action",
                Width = 350,
                Height = 220,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                CanResize = false
            };

            var playerCombo = new ComboBox
            {
                Width = 220,
                ItemsSource = team.Players
            };
            var currentPlayer = ev.Player?.Player
                                ?? team.Players.FirstOrDefault(p => p.JerseyNumber ==
                                                                     ExtractJerseyFromRaw(ev.RawCode));
            playerCombo.SelectedItem = currentPlayer;

            var skillCombo = new ComboBox
            {
                Width = 220,
                ItemsSource = Enum.GetValues(typeof(BasicSkill))
            };
            skillCombo.SelectedItem = ev.Skill;

            var evalCombo = new ComboBox
            {
                Width = 220,
                ItemsSource = new[] { "#", "+", "!", "-", "/", "=" }
            };
            evalCombo.SelectedItem = MapEvaluationSymbolChar(ev.Eval);

            var okButton = new Button { Content = "OK", Width = 80, Margin = new Thickness(5) };
            var cancelButton = new Button { Content = "Cancel", Width = 80, Margin = new Thickness(5) };

            okButton.Click += (_, _) =>
            {
                if (playerCombo.SelectedItem is not Player newPlayer ||
                    skillCombo.SelectedItem is not BasicSkill newSkill ||
                    evalCombo.SelectedItem is not string evalSymbol)
                {
                    tcs.TrySetResult(false);
                    dlg.Close();
                    return;
                }

                var newEval = evalSymbol switch
                {
                    "#" => EvaluationSymbol.Point,
                    "+" => EvaluationSymbol.Positive,
                    "!" => EvaluationSymbol.Good,
                    "-" => EvaluationSymbol.Poor,
                    "/" => EvaluationSymbol.Over,
                    "=" => EvaluationSymbol.Error,
                    _ => EvaluationSymbol.Error
                };

                ev.Skill = newSkill;
                ev.Eval = newEval;

                var mp = GetOrCreateMatchPlayer(ev.Side, newPlayer);
                ev.Player = mp;

                var teamSideStr = ev.Side == TeamSide.Home ? "*" : "a";
                var skillStr = newSkill switch
                {
                    BasicSkill.Serve => "S",
                    BasicSkill.Reception => "R",
                    BasicSkill.Set => "E",
                    BasicSkill.Attack => "A",
                    BasicSkill.Block => "B",
                    BasicSkill.Dig => "D",
                    BasicSkill.FreeBall => "F",
                    _ => "A"
                };

                ev.RawCode = $"{teamSideStr}{newPlayer.JerseyNumber}{skillStr}{evalSymbol}";

                tcs.TrySetResult(true);
                dlg.Close();
            };

            cancelButton.Click += (_, _) =>
            {
                tcs.TrySetResult(false);
                dlg.Close();
            };

            dlg.Content = new StackPanel
            {
                Margin = new Thickness(10),
                Children =
        {
            new TextBlock
            {
                Text = "Edit action",
                FontSize = 16,
                Margin = new Thickness(0,0,0,8),
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center
            },
            new StackPanel
            {
                Orientation = Avalonia.Layout.Orientation.Horizontal,
                Margin = new Thickness(0,0,0,4),
                Children =
                {
                    new TextBlock { Text = "Player:", Width = 80, VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center },
                    playerCombo
                }
            },
            new StackPanel
            {
                Orientation = Avalonia.Layout.Orientation.Horizontal,
                Margin = new Thickness(0,0,0,4),
                Children =
                {
                    new TextBlock { Text = "Action:", Width = 80, VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center },
                    skillCombo
                }
            },
            new StackPanel
            {
                Orientation = Avalonia.Layout.Orientation.Horizontal,
                Margin = new Thickness(0,0,0,8),
                Children =
                {
                    new TextBlock { Text = "Eval:", Width = 80, VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center },
                    evalCombo
                }
            },
            new StackPanel
            {
                Orientation = Avalonia.Layout.Orientation.Horizontal,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
                Children = { okButton, cancelButton }
            }
        }
            };

            await dlg.ShowDialog(owner);
            return await tcs.Task;
        }
        private static int ExtractJerseyFromRaw(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw) || raw.Length < 3)
                return 0;

            int i = 1;
            while (i < raw.Length && char.IsDigit(raw[i]))
                i++;

            if (i == 1)
                return 0;

            return int.TryParse(raw.Substring(1, i - 1), out var num) ? num : 0;
        }
        private MatchSet? GetLastUnfinishedSet()
        {
            return _match.Sets
                .OrderBy(s => s.Number)
                .LastOrDefault(s => !IsSetFinished(s));
        }
        private void FixPlayersForAllEvents()
        {
            _matchPlayers.Clear();
            _match.Players.Clear();

            foreach (var set in _match.Sets)
            {
                foreach (var rally in set.Rallies)
                {
                    foreach (var ev in rally.Events)
                    {
                        EnsureEventPlayerFromRawCode(ev);
                    }
                }
            }
        }
        private void UpdateSetExtrasDisplay()
        {
            if (_currentSet == null)
            {
                HomeTimeoutsTextBlock.Text = "0";
                AwayTimeoutsTextBlock.Text = "0";
                HomeSubsTextBlock.Text = "0";
                AwaySubsTextBlock.Text = "0";
                return;
            }

            HomeTimeoutsTextBlock.Text = _currentSet.HomeTimeouts.ToString();
            AwayTimeoutsTextBlock.Text = _currentSet.AwayTimeouts.ToString();
            HomeSubsTextBlock.Text = _currentSet.HomeSubstitutions.ToString();
            AwaySubsTextBlock.Text = _currentSet.AwaySubstitutions.ToString();
        }
        private void HomeTimeoutButton_OnClick(object? sender, RoutedEventArgs e)
        {
            if (!_setActive || _currentSet == null)
                return;

            _currentSet.HomeTimeouts++;
            UpdateSetExtrasDisplay();
        }

        private void AwayTimeoutButton_OnClick(object? sender, RoutedEventArgs e)
        {
            if (!_setActive || _currentSet == null)
                return;

            _currentSet.AwayTimeouts++;
            UpdateSetExtrasDisplay();
        }

        private void HomeSubButton_OnClick(object? sender, RoutedEventArgs e)
        {
            if (!_setActive || _currentSet == null)
                return;

            _currentSet.HomeSubstitutions++;
            UpdateSetExtrasDisplay();
        }

        private void AwaySubButton_OnClick(object? sender, RoutedEventArgs e)
        {
            if (!_setActive || _currentSet == null)
                return;

            _currentSet.AwaySubstitutions++;
            UpdateSetExtrasDisplay();
        }

    }
}
