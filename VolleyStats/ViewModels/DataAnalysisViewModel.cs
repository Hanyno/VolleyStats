using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VolleyStats.Data;
using VolleyStats.Models;

namespace VolleyStats.ViewModels
{
    public partial class DataAnalysisViewModel : ViewModelBase
    {
        private readonly IReadOnlyList<string> _matchFilePaths;
        private readonly string _analysisTeam;

        public string AnalysisTeam => _analysisTeam;
        public int MatchCount => _matchFilePaths.Count;

        public ObservableCollection<PanelTypeInfo> AvailablePanels { get; } = new();
        public ObservableCollection<DockPanelViewModel> Panels { get; } = new();

        [ObservableProperty] private bool _showEmptyState = true;

        public IRelayCommand BackCommand { get; }
        public IRelayCommand<PanelTypeInfo> AddPanelCommand { get; }

        /// <summary>
        /// Dialog delegate: shows match report choice dialog.
        /// Receives (matchFilePaths, analysisTeam).
        /// Returns: (ReportMode, selectedFilePath or null for aggregate).
        /// </summary>
        public Func<IReadOnlyList<MatchReportChoice>, string, Task<MatchReportDialogResult?>>?
            TriggerMatchReportChoiceDialog { get; set; }

        public DataAnalysisViewModel(
            IReadOnlyList<string> matchFilePaths,
            string analysisTeam,
            Func<Task> navigateBack)
        {
            _matchFilePaths = matchFilePaths;
            _analysisTeam = analysisTeam;

            BackCommand = new AsyncRelayCommand(navigateBack);
            AddPanelCommand = new AsyncRelayCommand<PanelTypeInfo>(AddPanelAsync);

            AvailablePanels.Add(new PanelTypeInfo
            {
                Id = "match-report",
                Name = "Match Report",
                Description = "Player stats, serve, reception, attack, block",
                Icon = "◎",
                Color = "#7C3AED"
            });
        }

        private async Task AddPanelAsync(PanelTypeInfo? panelType)
        {
            if (panelType == null) return;

            ViewModelBase? content = panelType.Id switch
            {
                "match-report" => await CreateMatchReportPanelAsync(),
                _ => null
            };

            if (content == null) return;

            var panel = new DockPanelViewModel(
                panelType.Name,
                panelType.Icon,
                panelType.Color,
                content,
                RemovePanel);

            Panels.Add(panel);
            ShowEmptyState = false;
        }

        private async Task<MatchReportViewModel?> CreateMatchReportPanelAsync()
        {
            ReportMode mode;
            string? singleFile = null;

            if (_matchFilePaths.Count == 1)
            {
                // Single match: let user choose between full match report or team-only
                var choices = new List<MatchReportChoice>();

                var parser = new DvwFileParser();
                try
                {
                    var summary = parser.ParseMatchSummary(_matchFilePaths[0]);
                    choices.Add(new MatchReportChoice
                    {
                        FilePath = _matchFilePaths[0],
                        Label = $"{summary.HomeTeam} vs {summary.AwayTeam}",
                        Detail = summary.Date?.ToString("MM/dd/yyyy") ?? "",
                        IsSingleMatch = true
                    });
                }
                catch
                {
                    choices.Add(new MatchReportChoice
                    {
                        FilePath = _matchFilePaths[0],
                        Label = System.IO.Path.GetFileNameWithoutExtension(_matchFilePaths[0]),
                        IsSingleMatch = true
                    });
                }

                if (TriggerMatchReportChoiceDialog != null)
                {
                    var result = await TriggerMatchReportChoiceDialog(choices, _analysisTeam);
                    if (result == null) return null;
                    mode = result.Mode;
                    singleFile = result.SelectedFilePath;
                }
                else
                {
                    mode = ReportMode.SingleMatch;
                }
            }
            else
            {
                // Multiple matches: show choice dialog
                var choices = new List<MatchReportChoice>();
                var parser = new DvwFileParser();

                foreach (var fp in _matchFilePaths)
                {
                    try
                    {
                        var summary = parser.ParseMatchSummary(fp);
                        choices.Add(new MatchReportChoice
                        {
                            FilePath = fp,
                            Label = $"{summary.HomeTeam} vs {summary.AwayTeam}",
                            Detail = summary.Date?.ToString("MM/dd/yyyy") ?? "",
                            IsSingleMatch = true
                        });
                    }
                    catch
                    {
                        choices.Add(new MatchReportChoice
                        {
                            FilePath = fp,
                            Label = System.IO.Path.GetFileNameWithoutExtension(fp),
                            IsSingleMatch = true
                        });
                    }
                }

                if (TriggerMatchReportChoiceDialog != null)
                {
                    var result = await TriggerMatchReportChoiceDialog(choices, _analysisTeam);
                    if (result == null) return null;
                    mode = result.Mode;
                    singleFile = result.SelectedFilePath;
                }
                else
                {
                    mode = ReportMode.SingleMatch;
                }
            }

            var vm = new MatchReportViewModel(
                _matchFilePaths, _analysisTeam, navigateBack: null,
                mode: mode, singleMatchFile: singleFile)
            {
                IsEmbedded = true
            };
            _ = vm.InitializeAsync();
            return vm;
        }

        private void RemovePanel(DockPanelViewModel panel)
        {
            Panels.Remove(panel);
            ShowEmptyState = Panels.Count == 0;
        }
    }

    public class MatchReportChoice
    {
        public string FilePath { get; set; } = "";
        public string Label { get; set; } = "";
        public string Detail { get; set; } = "";
        public bool IsSingleMatch { get; set; }
    }

    public class MatchReportDialogResult
    {
        public ReportMode Mode { get; set; }
        public string? SelectedFilePath { get; set; }
    }

    public class PanelTypeInfo
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public string Icon { get; set; } = "";
        public string Color { get; set; } = "#64748B";
    }

    public partial class DockPanelViewModel : ViewModelBase
    {
        public string Title { get; }
        public string Icon { get; }
        public string Color { get; }
        public ViewModelBase Content { get; }
        public IRelayCommand CloseCommand { get; }

        [ObservableProperty] private bool _isMaximized;

        public IRelayCommand ToggleMaximizeCommand { get; }

        public DockPanelViewModel(
            string title,
            string icon,
            string color,
            ViewModelBase content,
            Action<DockPanelViewModel> removeAction)
        {
            Title = title;
            Icon = icon;
            Color = color;
            Content = content;
            CloseCommand = new RelayCommand(() => removeAction(this));
            ToggleMaximizeCommand = new RelayCommand(() => IsMaximized = !IsMaximized);
        }
    }
}
