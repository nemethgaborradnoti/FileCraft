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
        private readonly string _appDirectory;

        public SettingsService()
        {
            _appDirectory = AppDomain.CurrentDomain.BaseDirectory;
            _settingsFilePath = Path.Combine(_appDirectory, "settings.json");
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

        private string GetPresetFilePath(int presetNumber)
        {
            return Path.Combine(_appDirectory, $"settings_preset_{presetNumber:00}.json");
        }

        public void SaveSettingsAsPreset(Settings settings, int presetNumber)
        {
            Guard.AgainstNull(settings, nameof(settings));
            string presetFilePath = GetPresetFilePath(presetNumber);
            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                string json = JsonSerializer.Serialize(settings, options);
                File.WriteAllText(presetFilePath, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving preset {presetNumber}: {ex.Message}");
                throw;
            }
        }

        public Settings LoadSettingsFromPreset(int presetNumber)
        {
            string presetFilePath = GetPresetFilePath(presetNumber);
            if (!File.Exists(presetFilePath))
            {
                return null;
            }

            try
            {
                string json = File.ReadAllText(presetFilePath);
                var settings = JsonSerializer.Deserialize<Settings>(json);
                return settings ?? new Settings();
            }
            catch (Exception)
            {
                return new Settings();
            }
        }
    }
}
