using Avalonia.Controls;
using Avalonia.Platform.Storage;
using System.Linq;
using System.Threading.Tasks;
using VolleyStats.Models;
using VolleyStats.ViewModels;

namespace VolleyStats.Views
{
    public partial class TeamsWindow : Window, ITeamDialogService, IFilePickerService
    {
        private TeamsViewModel? _viewModel;

        public TeamsWindow()
        {
            InitializeComponent();
        }

        public void SetViewModel(TeamsViewModel viewModel)
        {
            _viewModel = viewModel;
            DataContext = _viewModel;

            this.Opened -= TeamsWindow_Opened;
            this.Opened += TeamsWindow_Opened;
        }

        private async void TeamsWindow_Opened(object? sender, System.EventArgs e)
        {
            if (_viewModel != null)
            {
                await _viewModel.LoadTeamsCommand.ExecuteAsync(null);
            }
        }

        public async Task<TeamDialogResult?> ShowTeamDialogAsync(Team team)
        {
            var detailWindow = new TeamDetailWindow(team);
            return await detailWindow.ShowDialog<TeamDialogResult?>(this);
        }

        public async Task<string?> PickSqFileAsync()
        {
            var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                AllowMultiple = false,
                FileTypeFilter = new[]
                {
                    new FilePickerFileType("DataVolley sq") { Patterns = new[] { "*.sq" } },
                    new FilePickerFileType("All files") { Patterns = new[] { "*.*" } }
                }
            });

            return files.Count > 0 ? files[0].TryGetLocalPath() : null;
        }

        public async Task<string?> PickSqSavePathAsync(string defaultFileName)
        {
            var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                DefaultExtension = "sq",
                SuggestedFileName = defaultFileName,
                FileTypeChoices = new[]
                {
                    new FilePickerFileType("DataVolley sq") { Patterns = new[] { "*.sq" } },
                    new FilePickerFileType("All files") { Patterns = new[] { "*.*" } }
                }
            });

            return file?.TryGetLocalPath();
        }
    }
}