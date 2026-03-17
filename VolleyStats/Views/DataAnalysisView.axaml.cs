using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using VolleyStats.ViewModels;

namespace VolleyStats.Views
{
    public partial class DataAnalysisView : UserControl
    {
        public DataAnalysisView()
        {
            InitializeComponent();
            DataContextChanged += OnDataContextChanged;
        }

        private void OnDataContextChanged(object? sender, System.EventArgs e)
        {
            if (DataContext is DataAnalysisViewModel vm)
            {
                vm.TriggerMatchReportChoiceDialog = ShowMatchReportChoiceDialogAsync;
            }
        }

        private Window? GetParentWindow()
        {
            return TopLevel.GetTopLevel(this) as Window;
        }

        private async Task<MatchReportDialogResult?> ShowMatchReportChoiceDialogAsync(
            IReadOnlyList<MatchReportChoice> choices,
            string analysisTeam)
        {
            var parentWindow = GetParentWindow();
            if (parentWindow == null) return null;

            MatchReportDialogResult? result = null;

            var content = new StackPanel
            {
                Margin = new Thickness(24, 20),
                Spacing = 12
            };

            content.Children.Add(new TextBlock
            {
                Text = "Choose report type:",
                FontSize = 14,
                FontWeight = FontWeight.SemiBold,
                Margin = new Thickness(0, 0, 0, 4)
            });

            var dialog = new Window
            {
                Title = "Match Report",
                Width = 480,
                MinHeight = 160,
                SizeToContent = SizeToContent.Height,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                CanResize = false,
                Content = content
            };

            // Team aggregate option
            var aggregateBtn = new Button
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                HorizontalContentAlignment = HorizontalAlignment.Left,
                Padding = new Thickness(16, 12),
                CornerRadius = new CornerRadius(8),
                Background = new SolidColorBrush(Color.Parse("#7C3AED")),
                Foreground = Brushes.White,
                Content = new StackPanel
                {
                    Children =
                    {
                        new TextBlock
                        {
                            Text = $"{analysisTeam} — all {choices.Count} match(es)",
                            FontWeight = FontWeight.SemiBold,
                            FontSize = 13,
                            Foreground = Brushes.White
                        },
                        new TextBlock
                        {
                            Text = "Aggregate player stats across all selected matches",
                            FontSize = 11,
                            Foreground = new SolidColorBrush(Color.Parse("#E9D5FF"))
                        }
                    }
                }
            };
            aggregateBtn.Click += (_, _) =>
            {
                result = new MatchReportDialogResult { Mode = ReportMode.TeamAggregate };
                dialog.Close();
            };
            content.Children.Add(aggregateBtn);

            // Separator
            if (choices.Count > 0)
            {
                content.Children.Add(new TextBlock
                {
                    Text = "Or pick a single match:",
                    FontSize = 12,
                    Foreground = new SolidColorBrush(Color.Parse("#64748B")),
                    Margin = new Thickness(0, 4, 0, 0)
                });
            }

            // Individual match buttons
            foreach (var choice in choices)
            {
                var matchBtn = new Button
                {
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    HorizontalContentAlignment = HorizontalAlignment.Left,
                    Padding = new Thickness(16, 10),
                    CornerRadius = new CornerRadius(8),
                    Content = new StackPanel
                    {
                        Children =
                        {
                            new TextBlock
                            {
                                Text = choice.Label,
                                FontWeight = FontWeight.SemiBold,
                                FontSize = 13
                            },
                            new TextBlock
                            {
                                Text = choice.Detail,
                                FontSize = 11,
                                Foreground = new SolidColorBrush(Color.Parse("#64748B"))
                            }
                        }
                    }
                };

                var capturedPath = choice.FilePath;
                matchBtn.Click += (_, _) =>
                {
                    result = new MatchReportDialogResult
                    {
                        Mode = ReportMode.SingleMatch,
                        SelectedFilePath = capturedPath
                    };
                    dialog.Close();
                };
                content.Children.Add(matchBtn);
            }

            await dialog.ShowDialog(parentWindow);
            return result;
        }
    }
}
