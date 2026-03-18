using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace VolleyStats.Data
{
    public class RenderJob : ObservableObject
    {
        public required string Name { get; init; }
        public required string OutputPath { get; init; }
        public required IReadOnlyList<VideoSegment> Segments { get; init; }

        private string _status = "Queued";
        public string Status
        {
            get => _status;
            set => SetProperty(ref _status, value);
        }

        public IRelayCommand RemoveCommand => new RelayCommand(
            () => RenderQueueService.Instance.RemoveJob(this),
            () => Status == "Queued");
    }

    public class RenderQueueService : ObservableObject
    {
        public static RenderQueueService Instance { get; } = new();

        public ObservableCollection<RenderJob> Jobs { get; } = new();

        private bool _isProcessing;
        public bool IsProcessing
        {
            get => _isProcessing;
            private set => SetProperty(ref _isProcessing, value);
        }

        private int _currentJobIndex;
        public int CurrentJobIndex
        {
            get => _currentJobIndex;
            private set => SetProperty(ref _currentJobIndex, value);
        }

        private double _overallProgress;
        public double OverallProgress
        {
            get => _overallProgress;
            private set => SetProperty(ref _overallProgress, value);
        }

        private string _queueStatusText = "";
        public string QueueStatusText
        {
            get => _queueStatusText;
            private set => SetProperty(ref _queueStatusText, value);
        }

        public bool HasJobs => Jobs.Count > 0;

        private CancellationTokenSource? _cts;

        public void AddJob(RenderJob job)
        {
            Jobs.Add(job);
            OnPropertyChanged(nameof(HasJobs));
            Console.Error.WriteLine($"[Queue] Added job: {job.Name} -> {job.OutputPath} ({job.Segments.Count} segments)");
        }

        public void RemoveJob(RenderJob job)
        {
            Jobs.Remove(job);
            OnPropertyChanged(nameof(HasJobs));
        }

        public void ClearQueue()
        {
            Jobs.Clear();
            OnPropertyChanged(nameof(HasJobs));
        }

        public void CancelProcessing()
        {
            _cts?.Cancel();
        }

        public async Task ProcessQueueAsync(string encoderMode, string? ffmpegPath, Action? onAllCompleted)
        {
            if (IsProcessing || Jobs.Count == 0) return;

            IsProcessing = true;
            _cts = new CancellationTokenSource();
            var renderer = new VideoRendererService();
            var completed = 0;
            var failed = 0;
            var total = Jobs.Count;

            Console.Error.WriteLine($"[Queue] === Processing {total} jobs ===");

            for (int i = 0; i < Jobs.Count; i++)
            {
                if (_cts.Token.IsCancellationRequested) break;

                var job = Jobs[i];
                CurrentJobIndex = i;
                OverallProgress = (double)i / total * 100;

                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    job.Status = "Rendering...";
                    QueueStatusText = $"Rendering {i + 1}/{total}: {job.Name}";
                });

                Console.Error.WriteLine($"[Queue] Starting job {i + 1}/{total}: {job.Name}");

                var jobProgress = new Progress<VideoRenderProgress>(p =>
                {
                    var jobFraction = 1.0 / total;
                    var jobBase = (double)i / total * 100;
                    OverallProgress = jobBase + p.OverallPercent * jobFraction;
                });

                try
                {
                    await renderer.RenderAsync(
                        job.Segments, job.OutputPath, encoderMode, ffmpegPath,
                        jobProgress, _cts.Token);

                    await Dispatcher.UIThread.InvokeAsync(() => job.Status = "Done");
                    completed++;
                    Console.Error.WriteLine($"[Queue] Job {i + 1}/{total} completed: {job.Name}");
                }
                catch (OperationCanceledException)
                {
                    await Dispatcher.UIThread.InvokeAsync(() => job.Status = "Cancelled");
                    Console.Error.WriteLine($"[Queue] Cancelled at job {i + 1}/{total}");
                    break;
                }
                catch (Exception ex)
                {
                    await Dispatcher.UIThread.InvokeAsync(() => job.Status = $"Error: {ex.Message}");
                    failed++;
                    Console.Error.WriteLine($"[Queue] Job {i + 1}/{total} failed: {ex.Message}");
                }
            }

            OverallProgress = 100;
            QueueStatusText = $"Done: {completed} completed, {failed} failed, {total - completed - failed} skipped";
            IsProcessing = false;
            _cts = null;

            Console.Error.WriteLine($"[Queue] === Queue finished: {completed}/{total} completed, {failed} failed ===");

            onAllCompleted?.Invoke();
        }
    }
}
