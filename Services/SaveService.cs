using FileCraft.Models;
using FileCraft.Services.Interfaces;
using FileCraft.Shared.Validation;
using System.IO;
using System.Text.Json;

namespace FileCraft.Services
{
    public class SaveService : ISaveService
    {
        private readonly string _saveFilePath;
        private readonly string _appDirectory;

        public SaveService()
        {
            _appDirectory = AppDomain.CurrentDomain.BaseDirectory;
            _saveFilePath = Path.Combine(_appDirectory, "save.json");
        }

        public SaveData LoadSaveData()
        {
            if (!File.Exists(_saveFilePath))
            {
                return new SaveData();
            }

            try
            {
                string json = File.ReadAllText(_saveFilePath);
                var saveData = JsonSerializer.Deserialize<SaveData>(json);
                return saveData ?? new SaveData();
            }
            catch (Exception)
            {
                return new SaveData();
            }
        }

        public void Save(SaveData saveData)
        {
            Guard.AgainstNull(saveData, nameof(saveData));

            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                string json = JsonSerializer.Serialize(saveData, options);
                File.WriteAllText(_saveFilePath, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving data: {ex.Message}");
            }
        }

        private string GetPresetFilePath(int presetNumber)
        {
            return Path.Combine(_appDirectory, $"save_preset_{presetNumber:00}.json");
        }

        public void SaveAsPreset(SaveData saveData, int presetNumber)
        {
            Guard.AgainstNull(saveData, nameof(saveData));
            string presetFilePath = GetPresetFilePath(presetNumber);
            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                string json = JsonSerializer.Serialize(saveData, options);
                File.WriteAllText(presetFilePath, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving preset {presetNumber}: {ex.Message}");
                throw;
            }
        }

        public SaveData? LoadFromPreset(int presetNumber)
        {
            string presetFilePath = GetPresetFilePath(presetNumber);
            if (!File.Exists(presetFilePath))
            {
                return null;
            }

            try
            {
                string json = File.ReadAllText(presetFilePath);
                var saveData = JsonSerializer.Deserialize<SaveData>(json);
                return saveData ?? new SaveData();
            }
            catch (Exception)
            {
                return new SaveData();
            }
        }

        public bool CheckPresetExists(int presetNumber)
        {
            string presetFilePath = GetPresetFilePath(presetNumber);
            return File.Exists(presetFilePath);
        }

        public string GetPresetName(int presetNumber)
        {
            var presetData = LoadFromPreset(presetNumber);
            return presetData?.PresetName ?? string.Empty;
        }
    }
}
