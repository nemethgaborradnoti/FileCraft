using FileCraft.Models;
using FileCraft.Services.Interfaces;
using FileCraft.Shared.Validation;
using System.IO;
using System.Text.Json;

namespace FileCraft.Services
{
    public class PathPresetService : IPathPresetService
    {
        private readonly string _presetsDirectory;

        public PathPresetService()
        {
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            _presetsDirectory = Path.Combine(appDataPath, "FileCraft", "PathPresets");
            Directory.CreateDirectory(_presetsDirectory);
        }

        public void SavePreset(PathPreset preset)
        {
            Guard.AgainstNull(preset, nameof(preset));
            Guard.AgainstNullOrWhiteSpace(preset.Name, nameof(preset.Name));

            preset.LastModified = DateTime.Now;
            string filePath = GetPresetFilePath(preset.Name);

            var options = new JsonSerializerOptions { WriteIndented = true };
            string json = JsonSerializer.Serialize(preset, options);
            File.WriteAllText(filePath, json);
        }

        public IEnumerable<PathPreset> LoadPresets()
        {
            if (!Directory.Exists(_presetsDirectory))
            {
                yield break;
            }

            foreach (var filePath in Directory.EnumerateFiles(_presetsDirectory, "*.json"))
            {
                PathPreset? preset = null;
                try
                {
                    string json = File.ReadAllText(filePath);
                    preset = JsonSerializer.Deserialize<PathPreset>(json);
                }
                catch
                {
                    continue;
                }

                if (preset != null)
                {
                    yield return preset;
                }
            }
        }

        public void DeletePreset(string name)
        {
            Guard.AgainstNullOrWhiteSpace(name, nameof(name));
            string filePath = GetPresetFilePath(name);

            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }

        public void RenamePreset(string oldName, string newName)
        {
            Guard.AgainstNullOrWhiteSpace(oldName, nameof(oldName));
            Guard.AgainstNullOrWhiteSpace(newName, nameof(newName));

            string oldPath = GetPresetFilePath(oldName);
            string newPath = GetPresetFilePath(newName);

            if (File.Exists(oldPath))
            {
                if (File.Exists(newPath))
                {
                    File.Delete(newPath);
                }

                var preset = LoadPresetFromFile(oldPath);
                if (preset != null)
                {
                    preset.Name = newName;
                    preset.LastModified = DateTime.Now;

                    var options = new JsonSerializerOptions { WriteIndented = true };
                    string json = JsonSerializer.Serialize(preset, options);
                    File.WriteAllText(newPath, json);

                    File.Delete(oldPath);
                }
                else
                {
                    File.Move(oldPath, newPath);
                }
            }
        }

        public bool PresetExists(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return false;
            return File.Exists(GetPresetFilePath(name));
        }

        private string GetPresetFilePath(string name)
        {
            foreach (char c in Path.GetInvalidFileNameChars())
            {
                name = name.Replace(c, '_');
            }
            return Path.Combine(_presetsDirectory, $"{name}.json");
        }

        private PathPreset? LoadPresetFromFile(string filePath)
        {
            try
            {
                string json = File.ReadAllText(filePath);
                return JsonSerializer.Deserialize<PathPreset>(json);
            }
            catch
            {
                return null;
            }
        }
    }
}