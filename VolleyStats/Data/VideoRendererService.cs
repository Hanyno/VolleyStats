using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FFMpegCore;
using FFMpegCore.Arguments;
using FFMpegCore.Enums;

namespace VolleyStats.Data
{
    public class VideoSegment
    {
        public required string FilePath { get; init; }
        public double StartSeconds { get; init; }
        public double EndSeconds { get; init; }
    }

    public class VideoRenderProgress
    {
        public int CurrentSegment { get; init; }
        public int TotalSegments { get; init; }
        public required string Phase { get; init; }
        public double OverallPercent { get; init; }
    }

    public class VideoRendererService
    {
        public async Task RenderAsync(
            IReadOnlyList<VideoSegment> segments,
            string outputPath,
            string encoderMode,
            string? ffmpegPath,
            IProgress<VideoRenderProgress>? progress,
            CancellationToken cancellationToken)
        {
            Console.Error.WriteLine($"[Render] === RenderAsync START ===");
            Console.Error.WriteLine($"[Render] Segments: {segments.Count}, Output: {outputPath}");
            Console.Error.WriteLine($"[Render] EncoderMode: {encoderMode}, FfmpegPath: {ffmpegPath ?? "(null/PATH)"}");

            if (segments.Count == 0)
                throw new InvalidOperationException("No segments to render.");

            ConfigureFfmpeg(ffmpegPath);

            // Create temp folder next to the output video
            var outputDir = Path.GetDirectoryName(outputPath) ?? Path.GetTempPath();
            var tempDir = Path.Combine(outputDir, ".volley_render_" + Guid.NewGuid().ToString("N")[..8]);
            Directory.CreateDirectory(tempDir);
            Console.Error.WriteLine($"[Render] Temp dir: {tempDir}");

            try
            {
                // Phase 1: Extract clips using stream copy (fast)
                var clipPaths = new List<string>();
                for (int i = 0; i < segments.Count; i++)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var seg = segments[i];
                    var clipPath = Path.Combine(tempDir, $"clip_{i:D4}.mp4");

                    Console.Error.WriteLine($"[Render] Clip {i + 1}/{segments.Count}: {seg.FilePath}");
                    Console.Error.WriteLine($"[Render]   Start: {seg.StartSeconds}s, End: {seg.EndSeconds}s, Duration: {seg.EndSeconds - seg.StartSeconds}s");

                    progress?.Report(new VideoRenderProgress
                    {
                        CurrentSegment = i + 1,
                        TotalSegments = segments.Count,
                        Phase = "Extracting clips",
                        OverallPercent = (double)i / segments.Count * 50
                    });

                    var duration = seg.EndSeconds - seg.StartSeconds;
                    if (duration <= 0)
                    {
                        Console.Error.WriteLine($"[Render]   SKIP: duration <= 0");
                        continue;
                    }

                    if (!File.Exists(seg.FilePath))
                    {
                        Console.Error.WriteLine($"[Render]   SKIP: file not found: {seg.FilePath}");
                        continue;
                    }

                    var startTime = TimeSpan.FromSeconds(seg.StartSeconds);
                    var durationTs = TimeSpan.FromSeconds(duration);

                    try
                    {
                        Console.Error.WriteLine($"[Render]   Running ffmpeg extract: -ss {startTime} -t {durationTs} -> {clipPath}");

                        await FFMpegArguments
                            .FromFileInput(seg.FilePath, verifyExists: false, options => options
                                .Seek(startTime))
                            .OutputToFile(clipPath, overwrite: true, options => options
                                .CopyChannel()
                                .WithDuration(durationTs))
                            .CancellableThrough(cancellationToken)
                            .ProcessAsynchronously();

                        if (File.Exists(clipPath))
                        {
                            var fileSize = new FileInfo(clipPath).Length;
                            Console.Error.WriteLine($"[Render]   OK: clip created, size={fileSize} bytes");
                            clipPaths.Add(clipPath);
                        }
                        else
                        {
                            Console.Error.WriteLine($"[Render]   WARN: clip file not created");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine($"[Render]   ERROR extracting clip {i}: {ex.GetType().Name}: {ex.Message}");
                        Console.Error.WriteLine($"[Render]   Stack: {ex.StackTrace}");
                    }
                }

                Console.Error.WriteLine($"[Render] Phase 1 done: {clipPaths.Count} clips extracted");

                if (clipPaths.Count == 0)
                    throw new InvalidOperationException("No clips were extracted. Check that source video files exist.");

                cancellationToken.ThrowIfCancellationRequested();

                // Phase 2: Concatenate with encoding
                progress?.Report(new VideoRenderProgress
                {
                    CurrentSegment = segments.Count,
                    TotalSegments = segments.Count,
                    Phase = "Concatenating",
                    OverallPercent = 50
                });

                var concatListPath = Path.Combine(tempDir, "concat_list.txt");
                var concatLines = clipPaths.Select(p => $"file '{p.Replace("\\", "/").Replace("'", "'\\''")}'");
                await File.WriteAllLinesAsync(concatListPath, concatLines, cancellationToken);

                Console.Error.WriteLine($"[Render] Concat list written to: {concatListPath}");
                foreach (var line in concatLines)
                    Console.Error.WriteLine($"[Render]   {line}");

                var encoder = await ResolveEncoder(encoderMode, cancellationToken);
                Console.Error.WriteLine($"[Render] Resolved encoder: {encoder}");

                await RunConcatEncode(concatListPath, outputPath, encoder, progress, segments.Count, cancellationToken);

                if (File.Exists(outputPath))
                {
                    var outputSize = new FileInfo(outputPath).Length;
                    Console.Error.WriteLine($"[Render] === RenderAsync COMPLETE === Output: {outputPath}, size={outputSize} bytes");
                }
                else
                {
                    Console.Error.WriteLine($"[Render] === RenderAsync COMPLETE but output file not found! ===");
                }

                progress?.Report(new VideoRenderProgress
                {
                    CurrentSegment = segments.Count,
                    TotalSegments = segments.Count,
                    Phase = "Complete",
                    OverallPercent = 100
                });
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                Console.Error.WriteLine($"[Render] === RenderAsync FAILED === {ex.GetType().Name}: {ex.Message}");
                Console.Error.WriteLine($"[Render] Stack: {ex.StackTrace}");
                throw;
            }
            finally
            {
                Console.Error.WriteLine($"[Render] Cleaning up temp dir: {tempDir}");
                try { Directory.Delete(tempDir, recursive: true); } catch { }
            }
        }

        private static void ConfigureFfmpeg(string? ffmpegPath)
        {
            if (!string.IsNullOrEmpty(ffmpegPath))
            {
                var dir = Path.GetDirectoryName(ffmpegPath);
                Console.Error.WriteLine($"[Render] ConfigureFfmpeg: custom binary folder = {dir}");
                if (dir != null)
                    GlobalFFOptions.Configure(options => options.BinaryFolder = dir);
                return;
            }

            // Try to find ffmpeg on PATH first
            var pathDirs = Environment.GetEnvironmentVariable("PATH")?.Split(Path.PathSeparator) ?? [];
            foreach (var dir in pathDirs)
            {
                try
                {
                    var candidate = Path.Combine(dir, "ffmpeg.exe");
                    if (File.Exists(candidate))
                    {
                        Console.Error.WriteLine($"[Render] ConfigureFfmpeg: found on PATH at {dir}");
                        GlobalFFOptions.Configure(options => options.BinaryFolder = dir);
                        return;
                    }
                }
                catch { }
            }

            // Search common install locations
            var searchPaths = new List<string>();

            // WinGet packages folder
            var wingetPkgs = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Microsoft", "WinGet", "Packages");
            if (Directory.Exists(wingetPkgs))
            {
                try
                {
                    foreach (var binDir in Directory.EnumerateDirectories(wingetPkgs, "bin", SearchOption.AllDirectories))
                        searchPaths.Add(binDir);
                }
                catch { }
            }

            // Common manual install locations
            searchPaths.Add(@"C:\ffmpeg\bin");
            searchPaths.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "ffmpeg", "bin"));
            searchPaths.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ffmpeg", "bin"));

            // WinGet links folder
            searchPaths.Add(Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Microsoft", "WinGet", "Links"));

            foreach (var dir in searchPaths)
            {
                var candidate = Path.Combine(dir, "ffmpeg.exe");
                if (File.Exists(candidate))
                {
                    Console.Error.WriteLine($"[Render] ConfigureFfmpeg: found at {dir}");
                    GlobalFFOptions.Configure(options => options.BinaryFolder = dir);
                    return;
                }
            }

            Console.Error.WriteLine($"[Render] ConfigureFfmpeg: ffmpeg.exe NOT FOUND anywhere!");
        }

        private static async Task<string> ResolveEncoder(string encoderMode, CancellationToken ct)
        {
            Console.Error.WriteLine($"[Render] ResolveEncoder: mode={encoderMode}");
            return encoderMode switch
            {
                "GPU (NVIDIA)" => "h264_nvenc",
                "GPU (AMD)" => "h264_amf",
                "GPU (Intel)" => "h264_qsv",
                "GPU (Auto)" => await DetectBestEncoder(ct),
                _ => "libx264"
            };
        }

        private static async Task<string> DetectBestEncoder(CancellationToken ct)
        {
            var candidates = new[] { "h264_nvenc", "h264_amf", "h264_qsv" };

            foreach (var encoder in candidates)
            {
                Console.Error.WriteLine($"[Render] Testing GPU encoder: {encoder}");
                if (await TestEncoder(encoder, ct))
                {
                    Console.Error.WriteLine($"[Render] GPU encoder available: {encoder}");
                    return encoder;
                }
                Console.Error.WriteLine($"[Render] GPU encoder not available: {encoder}");
            }

            Console.Error.WriteLine($"[Render] No GPU encoder found, using libx264");
            return "libx264";
        }

        private static async Task<bool> TestEncoder(string encoder, CancellationToken ct)
        {
            try
            {
                var tempOut = Path.Combine(Path.GetTempPath(), $"fftest_{Guid.NewGuid():N}.mp4");
                try
                {
                    await FFMpegArguments
                        .FromFileInput("lavfi", verifyExists: false, options => options
                            .WithCustomArgument("-f lavfi -i color=black:s=64x64:d=0.1"))
                        .OutputToFile(tempOut, overwrite: true, options => options
                            .WithVideoCodec(encoder)
                            .WithFramerate(25)
                            .WithCustomArgument("-frames:v 1"))
                        .CancellableThrough(ct)
                        .ProcessAsynchronously();

                    return File.Exists(tempOut);
                }
                finally
                {
                    try { File.Delete(tempOut); } catch { }
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[Render] TestEncoder({encoder}) failed: {ex.Message}");
                return false;
            }
        }

        private static async Task RunConcatEncode(
            string concatListPath,
            string outputPath,
            string encoder,
            IProgress<VideoRenderProgress>? progress,
            int totalSegments,
            CancellationToken ct)
        {
            Console.Error.WriteLine($"[Render] RunConcatEncode: encoder={encoder}, concatList={concatListPath}, output={outputPath}");

            var args = FFMpegArguments
                .FromFileInput(concatListPath, verifyExists: false, options => options
                    .WithCustomArgument("-f concat -safe 0"))
                .OutputToFile(outputPath, overwrite: true, options =>
                {
                    options.WithVideoCodec(encoder);
                    options.WithAudioCodec("aac");

                    if (encoder == "libx264")
                        options.WithCustomArgument("-preset fast -crf 23");
                    else if (encoder == "h264_nvenc")
                        options.WithCustomArgument("-preset p4 -cq 23");
                    else if (encoder == "h264_amf")
                        options.WithCustomArgument("-quality balanced");
                    else if (encoder == "h264_qsv")
                        options.WithCustomArgument("-preset medium -global_quality 23");
                })
                .CancellableThrough(ct);

            try
            {
                Console.Error.WriteLine($"[Render] Starting concat encode...");
                await args.ProcessAsynchronously();
                Console.Error.WriteLine($"[Render] Concat encode finished successfully");
            }
            catch (Exception ex) when (encoder != "libx264")
            {
                Console.Error.WriteLine($"[Render] GPU encode failed: {ex.GetType().Name}: {ex.Message}");
                Console.Error.WriteLine($"[Render] Falling back to CPU (libx264)...");

                // GPU encoder failed, fall back to CPU
                progress?.Report(new VideoRenderProgress
                {
                    CurrentSegment = totalSegments,
                    TotalSegments = totalSegments,
                    Phase = "GPU failed, falling back to CPU",
                    OverallPercent = 55
                });

                await FFMpegArguments
                    .FromFileInput(concatListPath, verifyExists: false, options => options
                        .WithCustomArgument("-f concat -safe 0"))
                    .OutputToFile(outputPath, overwrite: true, options =>
                    {
                        options.WithVideoCodec("libx264");
                        options.WithAudioCodec("aac");
                        options.WithCustomArgument("-preset fast -crf 23");
                    })
                    .CancellableThrough(ct)
                    .ProcessAsynchronously();

                Console.Error.WriteLine($"[Render] CPU fallback encode finished successfully");
            }
        }
    }
}
