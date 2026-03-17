using System.ComponentModel;
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
