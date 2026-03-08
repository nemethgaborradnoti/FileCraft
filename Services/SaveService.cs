using FileCraft.Models;
using FileCraft.Services.Interfaces;
using FileCraft.Shared.Helpers;
using FileCraft.Shared.Validation;
using LiteDB;
using System.IO;
using System.Text.Json;
using JsonSerializer = System.Text.Json.JsonSerializer;

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
            catch (Exception)
            {
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
                catch (Exception)
                {
                    throw;
                }
            }
        }

        public void SavePreset(string name, string description, SaveData data)
        {
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

            var version = System.Reflection.Assembly.GetEntryAssembly()?.GetName().Version;
            string versionString = version != null ? $"{version.Major}.{version.Minor}.{version.Build}" : "1.0.0";

            var entity = new PresetEntity
            {
                Name = name,
                Description = description,
                CreatedAt = modifiedDate,
                LastModified = modifiedDate,
                AppVersion = versionString,
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
                            var saveData = JsonSerializer.Deserialize<SaveData>(json);

                            if (saveData != null)
                            {
                                string name = saveData.PresetName;
                                if (string.IsNullOrWhiteSpace(name))
                                {
                                    name = $"{ResourceHelper.GetString("Preset_DefaultNamePrefix")} {i:00}";
                                }

                                if (!PresetNameExists(name))
                                {
                                    DateTime fileDate = File.GetLastWriteTime(legacyPath);
                                    SavePresetInternal(name, "Imported from legacy JSON", saveData, fileDate);
                                }
                            }
                        }
                        catch
                        {
                        }
                    }
                }
            }
            catch
            {
            }
        }
    }
}