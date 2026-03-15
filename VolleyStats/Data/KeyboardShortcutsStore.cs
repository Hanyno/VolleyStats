using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using VolleyStats.Models;

namespace VolleyStats.Data
{
    public class KeyboardShortcutsStore
    {
        private static readonly string FilePath =
            Path.Combine(AppContext.BaseDirectory, "keyboard_shortcuts.json");

        public List<KeyboardShortcut> Load()
        {
            if (!File.Exists(FilePath)) return new List<KeyboardShortcut>();
            try
            {
                var json = File.ReadAllText(FilePath);
                return JsonSerializer.Deserialize<List<KeyboardShortcut>>(json) ?? new List<KeyboardShortcut>();
            }
            catch
            {
                return new List<KeyboardShortcut>();
            }
        }

        public void Save(IEnumerable<KeyboardShortcut> shortcuts)
        {
            var json = JsonSerializer.Serialize(
                new List<KeyboardShortcut>(shortcuts),
                new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(FilePath, json);
        }
    }
}
