using System;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.Input;
using VolleyStats.Models;

namespace VolleyStats.ViewModels
{
    public partial class TeamDetailViewModel : ViewModelBase
    {
        public event EventHandler<TeamDialogResult>? CloseRequested;

        private string _teamCode = string.Empty;
        public string TeamCode
        {
            get => _teamCode;
            set
            {
                if (SetProperty(ref _teamCode, value))
                {
                    SaveCommand.NotifyCanExecuteChanged();
                }
            }
        }

        private string _name = string.Empty;
        public string Name
        {
            get => _name;
            set
            {
                if (SetProperty(ref _name, value))
                {
                    SaveCommand.NotifyCanExecuteChanged();
                }
            }
        }

        private string? _coachName;
        public string? CoachName
        {
            get => _coachName;
            set => SetProperty(ref _coachName, value);
        }

        private string? _assistantCoachName;
        public string? AssistantCoachName
        {
            get => _assistantCoachName;
            set => SetProperty(ref _assistantCoachName, value);
        }

        private string? _abbreviation;
        public string? Abbreviation
        {
            get => _abbreviation;
            set => SetProperty(ref _abbreviation, value);
        }

        private Player? _selectedPlayer;
        public Player? SelectedPlayer
        {
            get => _selectedPlayer;
            set
            {
                if (SetProperty(ref _selectedPlayer, value))
                {
                    RemovePlayerCommand.NotifyCanExecuteChanged();
                }
            }
        }

        public ObservableCollection<Player> Players { get; } = new();

        public TeamDetailViewModel(Team team)
        {
            _teamCode = team.TeamCode;
            _name = team.Name;
            _coachName = team.CoachName;
            _assistantCoachName = team.AssistantCoachName;
            _abbreviation = team.Abbreviation;

            if (team.Players != null)
            {
                foreach (var player in team.Players)
                {
                    Players.Add(ClonePlayer(player));
                }
            }
        }

        [RelayCommand]
        private void AddPlayer()
        {
            Players.Add(new Player
            {
                TeamCode = _teamCode,
                JerseyNumber = 0,
                LastName = string.Empty,
                FirstName = string.Empty
            });
        }

        [RelayCommand(CanExecute = nameof(CanRemovePlayer))]
        private void RemovePlayer()
        {
            if (SelectedPlayer != null)
            {
                Players.Remove(SelectedPlayer);
            }
        }

        private bool CanRemovePlayer() => SelectedPlayer != null;

        [RelayCommand(CanExecute = nameof(CanSave))]
        private void Save()
        {
            var team = BuildTeam();
            CloseRequested?.Invoke(this, new TeamDialogResult
            {
                Result = TeamDialogResultType.Save,
                Team = team
            });
        }

        private bool CanSave() => !string.IsNullOrWhiteSpace(TeamCode) && !string.IsNullOrWhiteSpace(Name);

        [RelayCommand]
        private void Delete()
        {
            CloseRequested?.Invoke(this, new TeamDialogResult
            {
                Result = TeamDialogResultType.Delete,
                Team = BuildTeam()
            });
        }

        [RelayCommand]
        private void Cancel()
        {
            CloseRequested?.Invoke(this, new TeamDialogResult
            {
                Result = TeamDialogResultType.Cancel
            });
        }

        private Team BuildTeam()
        {
            var team = new Team
            {
                TeamCode = TeamCode ?? string.Empty,
                Name = Name ?? string.Empty,
                CoachName = CoachName,
                AssistantCoachName = AssistantCoachName,
                Abbreviation = Abbreviation
            };

            team.Players = Players.Select(ClonePlayer).ToList();
            return team;
        }

        private static Player ClonePlayer(Player p)
        {
            return new Player
            {
                Id = p.Id,
                TeamCode = p.TeamCode,
                JerseyNumber = p.JerseyNumber,
                ExternalPlayerId = p.ExternalPlayerId,
                LastName = p.LastName,
                FirstName = p.FirstName,
                BirthDate = p.BirthDate,
                HeightCm = p.HeightCm,
                Position = p.Position,
                PlayerRole = p.PlayerRole,
                NickName = p.NickName,
                IsForeign = p.IsForeign,
                TransferredOut = p.TransferredOut,
                BirthDateSerial = p.BirthDateSerial
            };
        }
    }
}
