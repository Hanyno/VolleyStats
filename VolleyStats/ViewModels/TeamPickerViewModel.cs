using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using VolleyStats.Data.Repositories;
using VolleyStats.Domain;

namespace VolleyStats.ViewModels
{
    public class TeamPickerViewModel : ViewModelBase
    {
        private readonly TeamsRepository _repository;

        public ObservableCollection<Team> Teams { get; } = new();

        private Team? _selectedTeam;
        public Team? SelectedTeam
        {
            get => _selectedTeam;
            set => SetProperty(ref _selectedTeam, value);
        }

        public TeamPickerViewModel(TeamsRepository repository)
        {
            _repository = repository;
        }

        public Task LoadTeamsAsync()
        {
            Teams.Clear();
            var teams = _repository.GetAllTeamsWithPlayers();
            foreach (var team in teams.OrderBy(t => t.Name))
            {
                Teams.Add(team);
            }

            SelectedTeam = Teams.FirstOrDefault();
            return Task.CompletedTask;
        }
    }
}
