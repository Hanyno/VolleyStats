using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using VolleyStats.Models;

namespace VolleyStats.ViewModels
{
    public class SetterPickerViewModel : ViewModelBase
    {
        public string TeamLabel { get; }
        public ObservableCollection<PlayerPickItem> CourtPlayers { get; } = new();

        private PlayerPickItem? _selected;
        public PlayerPickItem? Selected
        {
            get => _selected;
            set
            {
                SetProperty(ref _selected, value);
                OnPropertyChanged(nameof(CanConfirm));
            }
        }

        public bool CanConfirm => Selected != null;

        public SetterPickerViewModel(string teamLabel, string[] courtPositions, IReadOnlyList<MatchPlayer> allPlayers)
        {
            TeamLabel = teamLabel;
            var playerMap = allPlayers.ToDictionary(
                p => p.JerseyNumber.ToString(),
                p => (p.LastName ?? string.Empty, p.FirstName ?? string.Empty));

            foreach (var jersey in courtPositions)
            {
                playerMap.TryGetValue(jersey, out var nm);
                CourtPlayers.Add(new PlayerPickItem(jersey, nm.Item1, nm.Item2));
            }
        }
    }

    public class PlayerPickItem
    {
        public string JerseyNumber { get; }
        public string DisplayText  { get; }

        public PlayerPickItem(string jerseyNumber, string lastName, string firstName)
        {
            JerseyNumber = jerseyNumber;
            var name = $"{lastName} {firstName}".Trim();
            DisplayText  = string.IsNullOrEmpty(name) ? $"#{jerseyNumber}" : $"#{jerseyNumber}  {name}";
        }
    }

    public class SubstitutionViewModel : ViewModelBase
    {
        public string TeamLabel { get; }

        public ObservableCollection<PlayerPickItem> CourtPlayers { get; } = new();
        public ObservableCollection<PlayerPickItem> BenchPlayers { get; } = new();

        private PlayerPickItem? _selectedCourt;
        public PlayerPickItem? SelectedCourt
        {
            get => _selectedCourt;
            set
            {
                SetProperty(ref _selectedCourt, value);
                OnPropertyChanged(nameof(CanConfirm));
            }
        }

        private PlayerPickItem? _selectedBench;
        public PlayerPickItem? SelectedBench
        {
            get => _selectedBench;
            set
            {
                SetProperty(ref _selectedBench, value);
                OnPropertyChanged(nameof(CanConfirm));
            }
        }

        public bool CanConfirm => SelectedCourt != null && SelectedBench != null;

        public SubstitutionViewModel(
            string teamLabel,
            string[] courtPositions,
            IReadOnlyList<MatchPlayer> allPlayers)
        {
            TeamLabel = teamLabel;

            // Build lookup: jerseyNumber string → (lastName, firstName)
            var playerMap = allPlayers.ToDictionary(
                p => p.JerseyNumber.ToString(),
                p => (p.LastName ?? string.Empty, p.FirstName ?? string.Empty));

            var courtSet = new HashSet<string>(courtPositions);

            // Court: the 6 current positions with name lookup
            foreach (var jersey in courtPositions)
            {
                playerMap.TryGetValue(jersey, out var nm);
                CourtPlayers.Add(new PlayerPickItem(jersey, nm.Item1, nm.Item2));
            }

            // Bench: all players whose jersey is not in the current positions
            foreach (var player in allPlayers.OrderBy(p => p.JerseyNumber))
            {
                var j = player.JerseyNumber.ToString();
                if (!courtSet.Contains(j))
                    BenchPlayers.Add(new PlayerPickItem(j, player.LastName ?? string.Empty, player.FirstName ?? string.Empty));
            }
        }
    }
}
