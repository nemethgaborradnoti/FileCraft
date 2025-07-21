using FileCraft.Models;
using FileCraft.Services.Interfaces;
using FileCraft.Shared.Validation;
using System.IO;
using System.Text.Json;

namespace FileCraft.Services
{
    public class SettingsService : ISettingsService
    {
        private readonly string _settingsFilePath;

        public SettingsService()
        {
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string appFolderPath = Path.Combine(appDataPath, "FileCraft");
            string appDirectory = AppDomain.CurrentDomain.BaseDirectory;
            _settingsFilePath = Path.Combine(appDirectory, "settings.json");
        }

        public Settings LoadSettings()
        {
            if (!File.Exists(_settingsFilePath))
            {
                return new Settings();
            }

            try
            {
                string json = File.ReadAllText(_settingsFilePath);
                var settings = JsonSerializer.Deserialize<Settings>(json);
                return settings ?? new Settings();
            }
            catch (Exception)
            {
                return new Settings();
            }
        }

        public void SaveSettings(Settings settings)
        {
            Guard.AgainstNull(settings, nameof(settings));

            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                string json = JsonSerializer.Serialize(settings, options);
                File.WriteAllText(_settingsFilePath, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving settings: {ex.Message}");
            }
        }
    }
}