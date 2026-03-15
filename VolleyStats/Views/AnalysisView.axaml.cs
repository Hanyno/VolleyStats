using System.ComponentModel;
using Avalonia.Controls;
using VolleyStats.ViewModels;

namespace VolleyStats.Views
{
    public partial class AnalysisView : UserControl
    {
        public AnalysisView()
        {
            InitializeComponent();
            DataContextChanged += OnDataContextChanged;
        }

        private void OnDataContextChanged(object? sender, System.EventArgs e)
        {
            if (DataContext is AnalysisViewModel vm)
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
            if (sender is AnalysisViewModel vm && vm.AllVideoPaths.Count > 0)
                VideoPlayer.PreloadVideos(vm.AllVideoPaths);
        }

        private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (sender is not AnalysisViewModel vm) return;

            if (e.PropertyName == nameof(AnalysisViewModel.VideoSourcePath))
                VideoPlayer.VideoPath = vm.VideoSourcePath;
            else if (e.PropertyName == nameof(AnalysisViewModel.VideoSeekSeconds))
                VideoPlayer.VideoSeekSeconds = vm.VideoSeekSeconds;
            else if (e.PropertyName == nameof(AnalysisViewModel.SelectedCode) && vm.SelectedCode != null)
            {
                CodesList.ScrollIntoView(vm.SelectedCode);
                // Update the ListBox's internal focus so arrow keys work from the new position
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
