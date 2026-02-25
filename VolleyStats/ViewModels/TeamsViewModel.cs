using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using VolleyStats.Data.Repositories;
using VolleyStats.Domain;

namespace VolleyStats.ViewModels
{
    public class TeamsViewModel : ViewModelBase
    {
        private readonly TeamsRepository _repository;
        private readonly ITeamDialogService _dialogService;
        private readonly IFilePickerService _filePickerService;

        public ObservableCollection<Team> Teams { get; } = new();

        private Team? _selectedTeam;
        public Team? SelectedTeam
        {
            get => _selectedTeam;
            set
            {
                if (SetProperty(ref _selectedTeam, value))
                {
                    (EditSelectedTeamCommand as AsyncRelayCommand)?.NotifyCanExecuteChanged();
                    (DeleteSelectedTeamCommand as AsyncRelayCommand)?.NotifyCanExecuteChanged();
                    (ExportSelectedTeamCommand as AsyncRelayCommand)?.NotifyCanExecuteChanged();
                }
            }
        }

        public IAsyncRelayCommand LoadTeamsCommand { get; }
        public IAsyncRelayCommand NewTeamCommand { get; }
        public IAsyncRelayCommand EditSelectedTeamCommand { get; }
        public IAsyncRelayCommand DeleteSelectedTeamCommand { get; }
        public IAsyncRelayCommand ImportTeamsCommand { get; }
        public IAsyncRelayCommand EditTeamCommand { get; }
        public IAsyncRelayCommand ExportSelectedTeamCommand { get; }

        public TeamsViewModel(TeamsRepository repository, ITeamDialogService dialogService, IFilePickerService filePickerService)
        {
            _repository = repository;
            _dialogService = dialogService;
            _filePickerService = filePickerService;

            LoadTeamsCommand = new AsyncRelayCommand(LoadTeamsAsync);
            NewTeamCommand = new AsyncRelayCommand(NewTeamAsync);
            EditSelectedTeamCommand = new AsyncRelayCommand(EditSelectedTeamAsync, () => SelectedTeam != null);
            DeleteSelectedTeamCommand = new AsyncRelayCommand(DeleteSelectedTeamAsync, () => SelectedTeam != null);
            ImportTeamsCommand = new AsyncRelayCommand(ImportTeamsAsync);
            EditTeamCommand = new AsyncRelayCommand<Team>(EditTeamAsync);
            ExportSelectedTeamCommand = new AsyncRelayCommand(ExportSelectedTeamAsync, () => SelectedTeam != null);
        }

        private async Task EditTeamAsync(Team? team)
        {
            if (team is null) return;
            SelectedTeam = team;
            await EditSelectedTeamAsync();
        }

        private async Task LoadTeamsAsync()
        {
            Teams.Clear();
            var items = _repository.GetAllTeamsWithPlayers();
            foreach (var team in items)
            {
                Teams.Add(team);
            }
        }

        private async Task ExportSelectedTeamAsync()
        {
            if (SelectedTeam == null)
                return;

            var defaultName = string.IsNullOrWhiteSpace(SelectedTeam.TeamCode)
                ? "team.sq"
                : $"{SelectedTeam.TeamCode}.sq";

            var path = await _filePickerService.PickSqSavePathAsync(defaultName);
            if (string.IsNullOrWhiteSpace(path))
                return;

            _repository.ExportTeamToSq(SelectedTeam, path);
        }

        private async Task NewTeamAsync()
        {
            var result = await _dialogService.ShowTeamDialogAsync(new Team());
            if (result?.Result == TeamDialogResultType.Save && result.Team != null)
            {
                _repository.SaveTeam(result.Team);
                await LoadTeamsAsync();
            }
        }

        private async Task EditSelectedTeamAsync()
        {
            if (SelectedTeam == null)
                return;

            var teamCopy = CloneTeam(SelectedTeam);
            var result = await _dialogService.ShowTeamDialogAsync(teamCopy);
            if (result == null)
                return;

            if (result.Result == TeamDialogResultType.Save && result.Team != null)
            {
                _repository.SaveTeam(result.Team);
                await LoadTeamsAsync();
            }
            else if (result.Result == TeamDialogResultType.Delete && result.Team != null)
            {
                _repository.DeleteTeam(result.Team.Id);
                await LoadTeamsAsync();
            }
        }

        private async Task DeleteSelectedTeamAsync()
        {
            if (SelectedTeam == null)
                return;

            _repository.DeleteTeam(SelectedTeam.Id);
            await LoadTeamsAsync();
        }

        private static Team CloneTeam(Team team)
        {
            var clone = new Team
            {
                Id = team.Id,
                TeamCode = team.TeamCode,
                Name = team.Name,
                CoachName = team.CoachName,
                AssistantCoachName = team.AssistantCoachName,
                Abbreviation = team.Abbreviation,
                CharacterEncoding = team.CharacterEncoding,
            };

            if (team.Players != null)
            {
                clone.Players = team.Players.Select(p => new Player
                {
                    Id = p.Id,
                    TeamId = p.TeamId,
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
                }).ToList();
            }

            return clone;
        }

        private async Task ImportTeamsAsync()
        {
            var path = await _filePickerService.PickSqFileAsync();
            if (string.IsNullOrWhiteSpace(path))
                return;

            if (_repository.TryImportFromSq(path, out _))
            {
                await LoadTeamsAsync();
            }
        }
    }
}
