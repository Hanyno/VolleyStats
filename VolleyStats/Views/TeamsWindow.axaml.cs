using Avalonia.Controls;
using System.Collections.Generic;
using System.Threading.Tasks;
using VolleyStats.Domain;
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
            var dialog = new OpenFileDialog
            {
                AllowMultiple = false,
                Filters = new List<FileDialogFilter>
                {
                    new FileDialogFilter
                    {
                        Name = "DataVolley sq",
                        Extensions = new List<string> { "sq" }
                    },
                    new FileDialogFilter
                    {
                        Name = "All files",
                        Extensions = new List<string> { "*" }
                    }
                }
            };

            var result = await dialog.ShowAsync(this);
            if (result != null && result.Length > 0)
            {
                return result[0];
            }

            return null;
        }

        public async Task<string?> PickSqSavePathAsync(string defaultFileName)
        {
            var dialog = new SaveFileDialog
            {
                DefaultExtension = "sq",
                InitialFileName = defaultFileName,
                Filters = new List<FileDialogFilter>
                {
                    new FileDialogFilter
                    {
                        Name = "DataVolley sq",
                        Extensions = new List<string> { "sq" }
                    },
                    new FileDialogFilter
                    {
                        Name = "All files",
                        Extensions = new List<string> { "*" }
                    }
                }
            };

            return await dialog.ShowAsync(this);
        }
    }
}