using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using VolleyStats.Data;
using VolleyStats.Data.Repositories;

namespace VolleyStats.ViewModels
{
    public partial class MainWindowViewModel : ViewModelBase
    {
        private readonly MatchSummaryLoader _matchSummaryLoader;
        private readonly TeamsRepository _teamsRepository;
        private readonly KeyboardShortcutsStore _shortcutsStore = new();

        public ObservableCollection<TabItemViewModel> Tabs { get; } = new();

        private TabItemViewModel? _selectedTab;
        public TabItemViewModel? SelectedTab
        {
            get => _selectedTab;
            set => SetProperty(ref _selectedTab, value);
        }

        public IRelayCommand AddTabCommand { get; }

        public MainWindowViewModel(MatchSummaryLoader matchSummaryLoader, TeamsRepository teamsRepository)
        {
            _matchSummaryLoader = matchSummaryLoader;
            _teamsRepository = teamsRepository;

            AddTabCommand = new RelayCommand(AddTab);
        }

        public async Task InitializeAsync()
        {
            if (Tabs.Count == 0)
            {
                await AddTabAsync();
            }
        }

        private void AddTab()
        {
            _ = AddTabAsync();
        }

        public async Task AddTabAsync()
        {
            var placeholder = new HomePageViewModel(_matchSummaryLoader, _teamsRepository);
            var tab = new TabItemViewModel("Home", placeholder, CloseTab);
            Tabs.Add(tab);
            SelectedTab = tab;

            var content = CreateHomeViewModel(tab);
            tab.Content = content;
            await content.InitializeAsync();
        }

        private HomePageViewModel CreateHomeViewModel(TabItemViewModel tab)
        {
            return new HomePageViewModel(
                _matchSummaryLoader,
                _teamsRepository,
                async matchItem => await OpenMatchInTab(tab, matchItem),
                async () =>
                {
                    var settingsVm = new SettingsViewModel(
                        async () =>
                        {
                            var homeVm = CreateHomeViewModel(tab);
                            tab.Header = "Home";
                            tab.Content = homeVm;
                            await homeVm.InitializeAsync();
                        },
                        _shortcutsStore);
                    tab.Header = "Settings";
                    tab.Content = settingsVm;
                });
        }

        private async Task OpenMatchInTab(TabItemViewModel tab, MatchListItemViewModel matchItem)
        {
            var matchVm = new MatchDetailViewModel(matchItem.FilePath, async () =>
            {
                var homeVm = CreateHomeViewModel(tab);
                tab.Header = "Home";
                tab.Content = homeVm;
                await homeVm.InitializeAsync();
            });

            tab.Header = $"{matchItem.HomeTeam} vs {matchItem.AwayTeam}";
            tab.Content = matchVm;
            await matchVm.InitializeAsync();
        }

        private void CloseTab(TabItemViewModel tab)
        {
            if (Tabs.Count <= 1)
                return;

            var index = Tabs.IndexOf(tab);
            var wasSelected = SelectedTab == tab;
            Tabs.Remove(tab);

            if (wasSelected)
            {
                SelectedTab = index < Tabs.Count ? Tabs[index] : Tabs[^1];
            }
        }
    }
}
