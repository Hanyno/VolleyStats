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
        }

        public string? LoadLastSeason()
        {
            if (!File.Exists(FilePath)) return null;
            try
            {
                var json = File.ReadAllText(FilePath);
                return JsonSerializer.Deserialize<AppSettings>(json)?.LastSeason;
            }
            catch
            {
                return null;
            }
        }

        public void SaveLastSeason(string? season)
        {
            try
            {
                var json = JsonSerializer.Serialize(
                    new AppSettings { LastSeason = season },
                    new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(FilePath, json);
            }
            catch { }
        }
    }
}
