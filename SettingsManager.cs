using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace NightGammaBoost
{
    public static class SettingsManager
    {
        private static string _path = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "NightGammaBoost", "settings.json");

        public class Settings
        {
            public double Threshold { get; set; } = 0.15;
            public double MaxBoost { get; set; } = 1.0;
            public bool Global { get; set; } = false;
            public bool Startup { get; set; } = false;
            public List<string> EnabledGames { get; set; } = new();
            public List<string> DisabledScreens { get; set; } = new();
        }

        public static Settings Load()
        {
            try
            {
                if (!File.Exists(_path)) return new Settings();
                string json = File.ReadAllText(_path);
                return JsonSerializer.Deserialize<Settings>(json) ?? new Settings();
            }
            catch { return new Settings(); }
        }

        public static void Save(Settings s)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
                File.WriteAllText(_path, JsonSerializer.Serialize(s, new JsonSerializerOptions { WriteIndented = true }));
            }
            catch { }
        }
    }
}