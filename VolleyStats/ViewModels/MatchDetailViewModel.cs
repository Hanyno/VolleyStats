using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using VolleyStats.Data;
using VolleyStats.Domain;

namespace VolleyStats.ViewModels
{
    public partial class MatchDetailViewModel : ViewModelBase
    {
        private readonly string _filePath;
        private readonly Func<Task> _navigateBack;
        private Match? _match;

        public string HomeTeamName { get; private set; } = string.Empty;
        public string AwayTeamName { get; private set; } = string.Empty;
        public string ScoreText { get; private set; } = "-";

        public ObservableCollection<CodeViewModel> Codes { get; } = new();

        private CodeViewModel? _selectedCode;
        public CodeViewModel? SelectedCode
        {
            get => _selectedCode;
            set
            {
                if (SetProperty(ref _selectedCode, value))
                    UpdateCourtPositions(value?.Code);
            }
        }

        // Home court positions (jersey numbers, default = position number)
        public string HomePos1 { get; private set; } = "1";
        public string HomePos2 { get; private set; } = "2";
        public string HomePos3 { get; private set; } = "3";
        public string HomePos4 { get; private set; } = "4";
        public string HomePos5 { get; private set; } = "5";
        public string HomePos6 { get; private set; } = "6";

        // Away court positions (jersey numbers, default = position number)
        public string AwayPos1 { get; private set; } = "1";
        public string AwayPos2 { get; private set; } = "2";
        public string AwayPos3 { get; private set; } = "3";
        public string AwayPos4 { get; private set; } = "4";
        public string AwayPos5 { get; private set; } = "5";
        public string AwayPos6 { get; private set; } = "6";

        private int _homeScore;
        public int HomeScore
        {
            get => _homeScore;
            private set => SetProperty(ref _homeScore, value);
        }

        private int _awayScore;
        public int AwayScore
        {
            get => _awayScore;
            private set => SetProperty(ref _awayScore, value);
        }

        private string _newCodeText = string.Empty;
        public string NewCodeText
        {
            get => _newCodeText;
            set => SetProperty(ref _newCodeText, value);
        }

        public IRelayCommand AddHomePointCommand { get; }
        public IRelayCommand AddAwayPointCommand { get; }
        public IAsyncRelayCommand BackCommand { get; }

        public MatchDetailViewModel(string filePath, Func<Task> navigateBack)
        {
            _filePath = filePath;
            _navigateBack = navigateBack;

            AddHomePointCommand = new RelayCommand(() => HomeScore++);
            AddAwayPointCommand = new RelayCommand(() => AwayScore++);
            BackCommand = new AsyncRelayCommand(_navigateBack);
        }

        public async Task InitializeAsync()
        {
            await Task.Run(() =>
            {
                var parser = new DvwFileParser();
                _match = parser.ParseDvwFile(_filePath);
            });

            if (_match == null) return;

            HomeTeamName = _match.HomeTeam?.Name ?? string.Empty;
            AwayTeamName = _match.AwayTeam?.Name ?? string.Empty;
            ScoreText = $"{_match.HomeTeam?.SetsWon ?? 0} - {_match.AwayTeam?.SetsWon ?? 0}";

            OnPropertyChanged(nameof(HomeTeamName));
            OnPropertyChanged(nameof(AwayTeamName));
            OnPropertyChanged(nameof(ScoreText));

            Codes.Clear();
            if (_match.ScoutCodes != null)
            {
                foreach (var code in _match.ScoutCodes)
                    Codes.Add(new CodeViewModel(code));
            }
        }

        private void UpdateCourtPositions(Code? code)
        {
            if (code == null || code.HomeZones.Length == 0)
            {
                HomePos1 = "1"; HomePos2 = "2"; HomePos3 = "3";
                HomePos4 = "4"; HomePos5 = "5"; HomePos6 = "6";
            }
            else
            {
                HomePos1 = ZoneText(code.HomeZones, 0);
                HomePos2 = ZoneText(code.HomeZones, 1);
                HomePos3 = ZoneText(code.HomeZones, 2);
                HomePos4 = ZoneText(code.HomeZones, 3);
                HomePos5 = ZoneText(code.HomeZones, 4);
                HomePos6 = ZoneText(code.HomeZones, 5);
            }

            if (code == null || code.AwayZones.Length == 0)
            {
                AwayPos1 = "1"; AwayPos2 = "2"; AwayPos3 = "3";
                AwayPos4 = "4"; AwayPos5 = "5"; AwayPos6 = "6";
            }
            else
            {
                AwayPos1 = ZoneText(code.AwayZones, 0);
                AwayPos2 = ZoneText(code.AwayZones, 1);
                AwayPos3 = ZoneText(code.AwayZones, 2);
                AwayPos4 = ZoneText(code.AwayZones, 3);
                AwayPos5 = ZoneText(code.AwayZones, 4);
                AwayPos6 = ZoneText(code.AwayZones, 5);
            }

            OnPropertyChanged(nameof(HomePos1)); OnPropertyChanged(nameof(HomePos2));
            OnPropertyChanged(nameof(HomePos3)); OnPropertyChanged(nameof(HomePos4));
            OnPropertyChanged(nameof(HomePos5)); OnPropertyChanged(nameof(HomePos6));
            OnPropertyChanged(nameof(AwayPos1)); OnPropertyChanged(nameof(AwayPos2));
            OnPropertyChanged(nameof(AwayPos3)); OnPropertyChanged(nameof(AwayPos4));
            OnPropertyChanged(nameof(AwayPos5)); OnPropertyChanged(nameof(AwayPos6));
        }

        private static string ZoneText(int[] zones, int index)
        {
            if (index >= zones.Length) return (index + 1).ToString();
            var v = zones[index];
            return v > 0 ? v.ToString() : (index + 1).ToString();
        }
    }
}
