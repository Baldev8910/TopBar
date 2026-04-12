using System;
using System.IO;
using System.Text.Json;

namespace TopBar.Helpers
{
    public class AppSettings
    {
        public double BarHeight { get; set; } = 40;
        public double BarOpacity { get; set; } = 0.85;
        public double AnimationSpeed { get; set; } = 1.0;
        public string TemperatureUnit { get; set; } = "C";
        public int WeatherRefreshMinutes { get; set; } = 15;
        public bool LaunchOnStartup { get; set; } = false;
    }

    public static class SettingsService
    {
        private static readonly string _filePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "TopBar", "settings.json");

        private static AppSettings? _current;

        public static AppSettings Current
        {
            get
            {
                if (_current == null) Load();
                return _current!;
            }
        }

        public static void Load()
        {
            try
            {
                if (File.Exists(_filePath))
                {
                    var json = File.ReadAllText(_filePath);
                    _current = JsonSerializer.Deserialize<AppSettings>(json)
                               ?? new AppSettings();
                }
                else
                {
                    _current = new AppSettings();
                }
            }
            catch
            {
                _current = new AppSettings();
            }
        }

        public static void Save()
        {
            try
            {
                var json = JsonSerializer.Serialize(_current,
                    new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_filePath, json);
            }
            catch { }
        }
    }
}