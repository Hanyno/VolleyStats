using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using CommunityToolkit.Mvvm.Input;
using VolleyStats.Enums;
using VolleyStats.Models;

namespace VolleyStats.ViewModels
{
    public class StartSetViewModel : ViewModelBase
    {
        private readonly string _homeTeamName;
        private readonly string _awayTeamName;
        private readonly IReadOnlyList<MatchPlayer> _homePlayers;
        private readonly IReadOnlyList<MatchPlayer> _awayPlayers;

        // Internal storage – always home/away regardless of swap
        private readonly string[] _homePos = { "", "", "", "", "", "" };
        private readonly string[] _awayPos = { "", "", "", "", "", "" };
        private string _homeSetter = "";
        private string _awaySetter = "";

        private bool _isSwapped;
        public bool IsSwapped
        {
            get => _isSwapped;
            private set => SetProperty(ref _isSwapped, value);
        }

        // Which arrays the top/bottom of the court currently point to
        private string[] TopPos => _isSwapped ? _homePos : _awayPos;
        private string[] BotPos => _isSwapped ? _awayPos : _homePos;

        // ── Team names ──────────────────────────────────────────────────────
        public string TopTeamName => _isSwapped ? _homeTeamName : _awayTeamName;
        public string BottomTeamName => _isSwapped ? _awayTeamName : _homeTeamName;

        // ── Player rosters (excluding liberos) ─────────────────────────────
        private IReadOnlyList<MatchPlayer>? _homePlayersFiltered;
        private IReadOnlyList<MatchPlayer>? _awayPlayersFiltered;
        private IReadOnlyList<MatchPlayer> HomePlayersFiltered =>
            _homePlayersFiltered ??= _homePlayers.Where(p => p.Position != PlayerPost.Libero).ToList();
        private IReadOnlyList<MatchPlayer> AwayPlayersFiltered =>
            _awayPlayersFiltered ??= _awayPlayers.Where(p => p.Position != PlayerPost.Libero).ToList();

        public IReadOnlyList<MatchPlayer> TopPlayers => _isSwapped ? HomePlayersFiltered : AwayPlayersFiltered;
        public IReadOnlyList<MatchPlayer> BottomPlayers => _isSwapped ? AwayPlayersFiltered : HomePlayersFiltered;

        // ── Top half positions (zones 1-6) ──────────────────────────────────
        public string TopP1 { get => TopPos[0]; set { if (TopPos[0] != value) { TopPos[0] = value; OnPropertyChanged(); } } }
        public string TopP2 { get => TopPos[1]; set { if (TopPos[1] != value) { TopPos[1] = value; OnPropertyChanged(); } } }
        public string TopP3 { get => TopPos[2]; set { if (TopPos[2] != value) { TopPos[2] = value; OnPropertyChanged(); } } }
        public string TopP4 { get => TopPos[3]; set { if (TopPos[3] != value) { TopPos[3] = value; OnPropertyChanged(); } } }
        public string TopP5 { get => TopPos[4]; set { if (TopPos[4] != value) { TopPos[4] = value; OnPropertyChanged(); } } }
        public string TopP6 { get => TopPos[5]; set { if (TopPos[5] != value) { TopPos[5] = value; OnPropertyChanged(); } } }

        public string TopSetter
        {
            get => _isSwapped ? _homeSetter : _awaySetter;
            set
            {
                if (_isSwapped) { if (_homeSetter != value) { _homeSetter = value; OnPropertyChanged(); } }
                else            { if (_awaySetter != value) { _awaySetter = value; OnPropertyChanged(); } }
            }
        }

        // ── Bottom half positions (zones 1-6) ───────────────────────────────
        public string BottomP1 { get => BotPos[0]; set { if (BotPos[0] != value) { BotPos[0] = value; OnPropertyChanged(); } } }
        public string BottomP2 { get => BotPos[1]; set { if (BotPos[1] != value) { BotPos[1] = value; OnPropertyChanged(); } } }
        public string BottomP3 { get => BotPos[2]; set { if (BotPos[2] != value) { BotPos[2] = value; OnPropertyChanged(); } } }
        public string BottomP4 { get => BotPos[3]; set { if (BotPos[3] != value) { BotPos[3] = value; OnPropertyChanged(); } } }
        public string BottomP5 { get => BotPos[4]; set { if (BotPos[4] != value) { BotPos[4] = value; OnPropertyChanged(); } } }
        public string BottomP6 { get => BotPos[5]; set { if (BotPos[5] != value) { BotPos[5] = value; OnPropertyChanged(); } } }

        public string BottomSetter
        {
            get => _isSwapped ? _awaySetter : _homeSetter;
            set
            {
                if (_isSwapped) { if (_awaySetter != value) { _awaySetter = value; OnPropertyChanged(); } }
                else            { if (_homeSetter != value) { _homeSetter = value; OnPropertyChanged(); } }
            }
        }

        // ── Dynamic colors (swap-aware) ─────────────────────────────────────
        public string TopBackground    => _isSwapped ? "#1E3A5F" : "#0D4F27";
        public string TopBorder        => _isSwapped ? "#60A5FA" : "#4ADE80";
        public string TopAccent        => _isSwapped ? "#60A5FA" : "#4ADE80";
        public string BottomBackground => _isSwapped ? "#0D4F27" : "#1E3A5F";
        public string BottomBorder     => _isSwapped ? "#4ADE80" : "#60A5FA";
        public string BottomAccent     => _isSwapped ? "#4ADE80" : "#60A5FA";

        // ── Setter zone highlighting ───────────────────────────────────────
        private static bool IsSetter(string pos, string setter) =>
            !string.IsNullOrEmpty(setter) && pos == setter;

        public bool IsTopP1Setter => IsSetter(TopP1, TopSetter);
        public bool IsTopP2Setter => IsSetter(TopP2, TopSetter);
        public bool IsTopP3Setter => IsSetter(TopP3, TopSetter);
        public bool IsTopP4Setter => IsSetter(TopP4, TopSetter);
        public bool IsTopP5Setter => IsSetter(TopP5, TopSetter);
        public bool IsTopP6Setter => IsSetter(TopP6, TopSetter);
        public bool IsBottomP1Setter => IsSetter(BottomP1, BottomSetter);
        public bool IsBottomP2Setter => IsSetter(BottomP2, BottomSetter);
        public bool IsBottomP3Setter => IsSetter(BottomP3, BottomSetter);
        public bool IsBottomP4Setter => IsSetter(BottomP4, BottomSetter);
        public bool IsBottomP5Setter => IsSetter(BottomP5, BottomSetter);
        public bool IsBottomP6Setter => IsSetter(BottomP6, BottomSetter);

        // ── Serving team ───────────────────────────────────────────────────
        // true = home is serving; follows the swap: when swapped, top=home, bot=away
        private bool _isHomeServing = true;

        public bool IsTopServing
        {
            get => _isSwapped ? _isHomeServing : !_isHomeServing;
            set
            {
                bool newHomeServing = _isSwapped ? value : !value;
                if (_isHomeServing != newHomeServing)
                {
                    _isHomeServing = newHomeServing;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(IsBottomServing));
                }
            }
        }

        public bool IsBottomServing
        {
            get => !IsTopServing;
            set => IsTopServing = !value;
        }

        public bool GetIsHomeServing() => _isHomeServing;

        public IRelayCommand ToggleServingCommand { get; }

        // ── Validation ──────────────────────────────────────────────────────
        public bool CanConfirm =>
            _homePos.All(IsValid) && _awayPos.All(IsValid) &&
            IsValid(_homeSetter) && IsValid(_awaySetter) &&
            _homePos.Contains(_homeSetter) && _awayPos.Contains(_awaySetter);

        private static bool IsValid(string s) => int.TryParse(s, out var n) && n >= 1 && n <= 99;

        // ── Commands ────────────────────────────────────────────────────────
        public IRelayCommand SwapSidesCommand { get; }

        public StartSetViewModel(
            string homeTeamName, string awayTeamName,
            IReadOnlyList<MatchPlayer> homePlayers, IReadOnlyList<MatchPlayer> awayPlayers)
        {
            _homeTeamName = homeTeamName;
            _awayTeamName = awayTeamName;
            _homePlayers  = homePlayers;
            _awayPlayers  = awayPlayers;
            SwapSidesCommand = new RelayCommand(SwapSides);
            ToggleServingCommand = new RelayCommand(ToggleServing);
        }

        private void SwapSides()
        {
            IsSwapped = !IsSwapped;
            NotifyAllSwappableProperties();
        }

        private void ToggleServing()
        {
            _isHomeServing = !_isHomeServing;
            OnPropertyChanged(nameof(IsTopServing));
            OnPropertyChanged(nameof(IsBottomServing));
        }

        // ── Result extraction (always home/away, swap-independent) ──────────
        public string[] GetHomePositions() => (string[])_homePos.Clone();
        public string[] GetAwayPositions() => (string[])_awayPos.Clone();
        public string GetHomeSetter() => _homeSetter;
        public string GetAwaySetter() => _awaySetter;

        // ── Notifications ───────────────────────────────────────────────────
        protected override void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);
            if (e.PropertyName != nameof(CanConfirm))
                base.OnPropertyChanged(new PropertyChangedEventArgs(nameof(CanConfirm)));

            // Re-evaluate setter highlights when any position or setter changes
            if (e.PropertyName is nameof(TopP1) or nameof(TopP2) or nameof(TopP3) or
                nameof(TopP4) or nameof(TopP5) or nameof(TopP6) or nameof(TopSetter))
            {
                NotifySetterHighlights(true);
            }
            if (e.PropertyName is nameof(BottomP1) or nameof(BottomP2) or nameof(BottomP3) or
                nameof(BottomP4) or nameof(BottomP5) or nameof(BottomP6) or nameof(BottomSetter))
            {
                NotifySetterHighlights(false);
            }
        }

        private void NotifySetterHighlights(bool top)
        {
            if (top)
            {
                base.OnPropertyChanged(new PropertyChangedEventArgs(nameof(IsTopP1Setter)));
                base.OnPropertyChanged(new PropertyChangedEventArgs(nameof(IsTopP2Setter)));
                base.OnPropertyChanged(new PropertyChangedEventArgs(nameof(IsTopP3Setter)));
                base.OnPropertyChanged(new PropertyChangedEventArgs(nameof(IsTopP4Setter)));
                base.OnPropertyChanged(new PropertyChangedEventArgs(nameof(IsTopP5Setter)));
                base.OnPropertyChanged(new PropertyChangedEventArgs(nameof(IsTopP6Setter)));
            }
            else
            {
                base.OnPropertyChanged(new PropertyChangedEventArgs(nameof(IsBottomP1Setter)));
                base.OnPropertyChanged(new PropertyChangedEventArgs(nameof(IsBottomP2Setter)));
                base.OnPropertyChanged(new PropertyChangedEventArgs(nameof(IsBottomP3Setter)));
                base.OnPropertyChanged(new PropertyChangedEventArgs(nameof(IsBottomP4Setter)));
                base.OnPropertyChanged(new PropertyChangedEventArgs(nameof(IsBottomP5Setter)));
                base.OnPropertyChanged(new PropertyChangedEventArgs(nameof(IsBottomP6Setter)));
            }
        }

        private void NotifyAllSwappableProperties()
        {
            OnPropertyChanged(nameof(TopTeamName));
            OnPropertyChanged(nameof(BottomTeamName));
            OnPropertyChanged(nameof(TopPlayers));
            OnPropertyChanged(nameof(BottomPlayers));

            OnPropertyChanged(nameof(TopP1)); OnPropertyChanged(nameof(TopP2));
            OnPropertyChanged(nameof(TopP3)); OnPropertyChanged(nameof(TopP4));
            OnPropertyChanged(nameof(TopP5)); OnPropertyChanged(nameof(TopP6));
            OnPropertyChanged(nameof(TopSetter));

            OnPropertyChanged(nameof(BottomP1)); OnPropertyChanged(nameof(BottomP2));
            OnPropertyChanged(nameof(BottomP3)); OnPropertyChanged(nameof(BottomP4));
            OnPropertyChanged(nameof(BottomP5)); OnPropertyChanged(nameof(BottomP6));
            OnPropertyChanged(nameof(BottomSetter));

            OnPropertyChanged(nameof(TopBackground)); OnPropertyChanged(nameof(TopBorder));
            OnPropertyChanged(nameof(TopAccent));
            OnPropertyChanged(nameof(BottomBackground)); OnPropertyChanged(nameof(BottomBorder));
            OnPropertyChanged(nameof(BottomAccent));

            OnPropertyChanged(nameof(IsTopServing));
            OnPropertyChanged(nameof(IsBottomServing));
        }
    }
}
