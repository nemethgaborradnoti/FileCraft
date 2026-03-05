using FileCraft.Models;
using FileCraft.Services.Interfaces;
using FileCraft.Shared.Helpers; // ResourceHelper miatt
using FileCraft.Shared.Validation;
using LiteDB;
using System.IO;
using System.Text.Json;

namespace FileCraft.Services
{
    public class SaveService : ISaveService
    {
        private readonly IDatabaseService _databaseService;
        private readonly string _saveFilePath;
        private readonly string _appDirectory;

        private const string PresetCollectionName = "SavePresets";

        public SaveService(IDatabaseService databaseService)
        {
            _databaseService = databaseService;

            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            _appDirectory = Path.Combine(appDataPath, "FileCraft");
            Directory.CreateDirectory(_appDirectory);
            _saveFilePath = Path.Combine(_appDirectory, "save.json");
        }

        private ILiteCollection<PresetEntity> GetCollection()
        {
            return _databaseService.GetCollection<PresetEntity>(PresetCollectionName);
        }

        #region Current State (JSON)

        public string GetSaveDirectory()
        {
            return _appDirectory;
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
                var saveData = System.Text.Json.JsonSerializer.Deserialize<SaveData>(json);
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
                string json = System.Text.Json.JsonSerializer.Serialize(saveData, options);
                File.WriteAllText(_saveFilePath, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving data: {ex.Message}");
            }
        }

        public void DeleteSaveData()
        {
            if (File.Exists(_saveFilePath))
            {
                try
                {
                    File.Delete(_saveFilePath);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error deleting save data: {ex.Message}");
                    throw;
                }
            }
        }
        #endregion

        #region Dynamic Presets (LiteDB)

        public void SavePreset(string name, string description, SaveData data)
        {
            // Internal logic shared with migration
            SavePresetInternal(name, description, data, DateTime.Now);
        }

        private void SavePresetInternal(string name, string description, SaveData data, DateTime modifiedDate)
        {
            Guard.AgainstNullOrWhiteSpace(name, nameof(name));
            Guard.AgainstNull(data, nameof(data));

            var col = GetCollection();

            var stats = new PresetStatistics
            {
                FolderCount = data.FileContentExport.FolderTreeState.Count +
                              data.FolderContentExport.FolderTreeState.Count +
                              data.TreeGenerator.FolderTreeState.Count,
                FileCount = data.FileContentExport.SelectedFilePaths.Count
            };

            var entity = new PresetEntity
            {
                Name = name,
                Description = description,
                CreatedAt = modifiedDate,
                LastModified = modifiedDate,
                AppVersion = System.Reflection.Assembly.GetEntryAssembly()?.GetName().Version?.ToString() ?? "1.0.0",
                Data = data,
                Statistics = stats
            };

            col.Insert(entity);
            col.EnsureIndex(x => x.Name);
        }

        public IEnumerable<PresetEntity> LoadPresets()
        {
            return GetCollection().FindAll().OrderByDescending(x => x.LastModified);
        }

        public SaveData? LoadPresetData(int id)
        {
            var entity = GetCollection().FindById(id);
            return entity?.Data;
        }

        public void DeletePreset(int id)
        {
            GetCollection().Delete(id);
        }

        public void UpdatePreset(int id, string name, string description)
        {
            var col = GetCollection();
            var entity = col.FindById(id);
            if (entity != null)
            {
                entity.Name = name;
                entity.Description = description;
                entity.LastModified = DateTime.Now;
                col.Update(entity);
            }
        }

        public bool PresetNameExists(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return false;
            return GetCollection().Exists(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        }

        public void ImportLegacyPresets()
        {
            try
            {
                for (int i = 1; i <= 5; i++)
                {
                    string legacyPath = Path.Combine(_appDirectory, $"save_preset_{i:00}.json");
                    if (File.Exists(legacyPath))
                    {
                        try
                        {
                            string json = File.ReadAllText(legacyPath);
                            var saveData = System.Text.Json.JsonSerializer.Deserialize<SaveData>(json);

                            if (saveData != null)
                            {
                                // Determine Name
                                string name = saveData.PresetName;
                                if (string.IsNullOrWhiteSpace(name))
                                {
                                    name = $"{ResourceHelper.GetString("Preset_DefaultNamePrefix")} {i:00}";
                                }

                                // Check if already imported
                                if (!PresetNameExists(name))
                                {
                                    // Use file date as modification date
                                    DateTime fileDate = File.GetLastWriteTime(legacyPath);

                                    SavePresetInternal(name, "Imported from legacy JSON", saveData, fileDate);
                                }
                            }
                        }
                        catch
                        {
                            // Ignore corrupted legacy files
                        }
                    }
                }
            }
            catch
            {
                // Safety catch for any IO/DB errors during migration
            }
        }

        #endregion
    }
}