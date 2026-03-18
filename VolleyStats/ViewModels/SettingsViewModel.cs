using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VolleyStats.Data;
using VolleyStats.Models;

namespace VolleyStats.ViewModels
{
    public partial class SettingsViewModel : ViewModelBase
    {
        private readonly Func<Task> _navigateBack;
        private readonly KeyboardShortcutsStore _store;
        private readonly AppSettingsStore _appSettingsStore;

        public ObservableCollection<KeyBindingViewModel> KeyBindings { get; } = new();

        public bool HasBindings => KeyBindings.Count > 0;

        public IAsyncRelayCommand BackCommand { get; }

        public List<string> EncoderModeOptions { get; } = new()
        {
            "CPU",
            "GPU (Auto)",
            "GPU (NVIDIA)",
            "GPU (AMD)",
            "GPU (Intel)"
        };

        private string _videoEncoderMode = "CPU";
        public string VideoEncoderMode
        {
            get => _videoEncoderMode;
            set
            {
                if (SetProperty(ref _videoEncoderMode, value))
                    _appSettingsStore.SaveVideoEncoderMode(value);
            }
        }

        private string _ffmpegPath = "";
        public string FfmpegPath
        {
            get => _ffmpegPath;
            set
            {
                if (SetProperty(ref _ffmpegPath, value))
                {
                    _appSettingsStore.SaveFfmpegPath(string.IsNullOrWhiteSpace(value) ? null : value);
                    UpdateFfmpegStatus();
                }
            }
        }

        private string _ffmpegStatus = "";
        public string FfmpegStatus
        {
            get => _ffmpegStatus;
            private set => SetProperty(ref _ffmpegStatus, value);
        }

        public IRelayCommand BrowseFfmpegCommand { get; }

        // Event for view to handle file picker
        public Func<Task<string?>>? RequestBrowseFfmpeg { get; set; }

        public SettingsViewModel(Func<Task> navigateBack, KeyboardShortcutsStore store, AppSettingsStore appSettingsStore)
        {
            _navigateBack = navigateBack;
            _store = store;
            _appSettingsStore = appSettingsStore;

            BackCommand = new AsyncRelayCommand(_navigateBack);
            BrowseFfmpegCommand = new AsyncRelayCommand(BrowseFfmpegAsync);

            KeyBindings.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HasBindings));

            LoadBindings();
            LoadVideoSettings();
        }

        private void LoadBindings()
        {
            KeyBindings.Clear();
            foreach (var s in _store.Load())
                KeyBindings.Add(new KeyBindingViewModel(s, RemoveBinding));
        }

        private void LoadVideoSettings()
        {
            _videoEncoderMode = _appSettingsStore.LoadVideoEncoderMode();
            OnPropertyChanged(nameof(VideoEncoderMode));

            _ffmpegPath = _appSettingsStore.LoadFfmpegPath() ?? "";
            OnPropertyChanged(nameof(FfmpegPath));

            UpdateFfmpegStatus();
        }

        private void UpdateFfmpegStatus()
        {
            if (!string.IsNullOrWhiteSpace(_ffmpegPath))
            {
                FfmpegStatus = File.Exists(_ffmpegPath) ? "Found (custom path)" : "Not found at specified path";
            }
            else
            {
                // Check if ffmpeg is on PATH
                var pathDirs = Environment.GetEnvironmentVariable("PATH")?.Split(Path.PathSeparator) ?? [];
                var found = pathDirs.Any(dir =>
                {
                    try { return File.Exists(Path.Combine(dir, "ffmpeg.exe")) || File.Exists(Path.Combine(dir, "ffmpeg")); }
                    catch { return false; }
                });
                FfmpegStatus = found ? "Found on PATH" : "Not found (install FFmpeg or set path)";
            }
        }

        private async Task BrowseFfmpegAsync()
        {
            if (RequestBrowseFfmpeg == null) return;
            var path = await RequestBrowseFfmpeg();
            if (path != null)
                FfmpegPath = path;
        }

        public void AddBinding(KeyboardShortcut shortcut)
        {
            KeyBindings.Add(new KeyBindingViewModel(shortcut, RemoveBinding));
            SaveBindings();
        }

        private void RemoveBinding(KeyBindingViewModel vm)
        {
            KeyBindings.Remove(vm);
            SaveBindings();
        }

        private void SaveBindings()
        {
            _store.Save(KeyBindings.Select(vm => vm.Shortcut));
        }
    }
}
