using System.ComponentModel;
using System.Threading.Tasks;
using Avalonia.Controls;
using VolleyStats.ViewModels;

namespace VolleyStats.Views
{
    public partial class VideoAnalysisView : UserControl
    {
        public VideoAnalysisView()
        {
            InitializeComponent();
            DataContextChanged += OnDataContextChanged;
        }

        private void OnDataContextChanged(object? sender, System.EventArgs e)
        {
            if (DataContext is VideoAnalysisViewModel vm)
            {
                vm.PropertyChanged += OnViewModelPropertyChanged;
                vm.InitializationCompleted += OnInitializationCompleted;
                vm.PauseVideoRequested += OnPauseVideoRequested;

                vm.RequestSavePath = async options =>
                {
                    var topLevel = TopLevel.GetTopLevel(this);
                    if (topLevel == null) return null;

                    var file = await topLevel.StorageProvider.SaveFilePickerAsync(options);
                    return file?.Path.LocalPath;
                };

                vm.RequestShowMessage = async (title, message) =>
                {
                    var window = TopLevel.GetTopLevel(this) as Window;
                    if (window == null) return;

                    var dialog = new Window
                    {
                        Title = title,
                        Width = 420,
                        Height = 180,
                        WindowStartupLocation = WindowStartupLocation.CenterOwner,
                        CanResize = false,
                        Content = new StackPanel
                        {
                            Margin = new Avalonia.Thickness(24, 20),
                            Spacing = 16,
                            Children =
                            {
                                new TextBlock
                                {
                                    Text = message,
                                    FontSize = 14,
                                    TextWrapping = Avalonia.Media.TextWrapping.Wrap
                                },
                                new Button
                                {
                                    Content = "OK",
                                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
                                    Padding = new Avalonia.Thickness(24, 8)
                                }
                            }
                        }
                    };

                    // Wire OK button to close
                    if (dialog.Content is StackPanel sp && sp.Children[1] is Button okBtn)
                        okBtn.Click += (_, _) => dialog.Close();

                    await dialog.ShowDialog(window);
                };

                VideoPlayer.PlaybackStateChanged += (_, isPlaying) =>
                {
                    vm.IsVideoPlaying = isPlaying;
                };

                if (vm.VideoSourcePath != null)
                    VideoPlayer.VideoPath = vm.VideoSourcePath;
            }
        }

        private void OnPauseVideoRequested(object? sender, System.EventArgs e)
        {
            VideoPlayer.PausePlayback();
        }

        private void OnInitializationCompleted(object? sender, System.EventArgs e)
        {
            if (sender is VideoAnalysisViewModel vm && vm.AllVideoPaths.Count > 0)
                VideoPlayer.PreloadVideos(vm.AllVideoPaths);
        }

        private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (sender is not VideoAnalysisViewModel vm) return;

            if (e.PropertyName == nameof(VideoAnalysisViewModel.VideoSourcePath))
                VideoPlayer.VideoPath = vm.VideoSourcePath;
            else if (e.PropertyName == nameof(VideoAnalysisViewModel.VideoSeekSeconds))
                VideoPlayer.VideoSeekSeconds = vm.VideoSeekSeconds;
            else if (e.PropertyName == nameof(VideoAnalysisViewModel.SelectedCode) && vm.SelectedCode != null)
            {
                CodesList.ScrollIntoView(vm.SelectedCode);
                var index = vm.FilteredCodes.IndexOf(vm.SelectedCode);
                if (index >= 0)
                {
                    var container = CodesList.ContainerFromIndex(index);
                    container?.Focus();
                }
            }
        }
    }
}
