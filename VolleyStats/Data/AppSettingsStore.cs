using System;
using System.IO;
using System.Text.Json;

namespace VolleyStats.Data
{
    public class AppSettingsStore
    {
        private static readonly string FilePath =
            Path.Combine(AppContext.BaseDirectory, "app_settings.json");

        private sealed class AppSettings
        {
            public string? LastSeason { get; set; }
            public string VideoEncoderMode { get; set; } = "CPU";
            public string? FfmpegPath { get; set; }
        }

        private AppSettings LoadAll()
        {
            if (!File.Exists(FilePath)) return new AppSettings();
            try
            {
                var json = File.ReadAllText(FilePath);
                return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
            }
            catch
            {
                return new AppSettings();
            }
        }

        private void SaveAll(AppSettings settings)
        {
            try
            {
                var json = JsonSerializer.Serialize(settings,
                    new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(FilePath, json);
            }
            catch { }
        }

        public string? LoadLastSeason()
        {
            return LoadAll().LastSeason;
        }

        public void SaveLastSeason(string? season)
        {
            var settings = LoadAll();
            settings.LastSeason = season;
            SaveAll(settings);
        }

        public string LoadVideoEncoderMode()
        {
            return LoadAll().VideoEncoderMode;
        }

        public void SaveVideoEncoderMode(string mode)
        {
            var settings = LoadAll();
            settings.VideoEncoderMode = mode;
            SaveAll(settings);
        }

        public string? LoadFfmpegPath()
        {
            return LoadAll().FfmpegPath;
        }

        public void SaveFfmpegPath(string? path)
        {
            var settings = LoadAll();
            settings.FfmpegPath = path;
            SaveAll(settings);
        }
    }
}
