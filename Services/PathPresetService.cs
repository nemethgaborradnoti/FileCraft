using FileCraft.Models;
using FileCraft.Services.Interfaces;
using FileCraft.Shared.Validation;
using LiteDB;

namespace FileCraft.Services
{
    public class PathPresetService : IPathPresetService
    {
        private readonly IDatabaseService _databaseService;
        private const string CollectionName = "PathPresets";

        public PathPresetService(IDatabaseService databaseService)
        {
            _databaseService = databaseService;
        }

        private ILiteCollection<PathPreset> GetCollection()
        {
            return _databaseService.GetCollection<PathPreset>(CollectionName);
        }

        public void SavePreset(PathPreset preset)
        {
            Guard.AgainstNull(preset, nameof(preset));
            Guard.AgainstNullOrWhiteSpace(preset.Name, nameof(preset.Name));

            preset.LastModified = DateTime.Now;
            var col = GetCollection();

            var existing = col.FindOne(p => p.Name.Equals(preset.Name, StringComparison.OrdinalIgnoreCase));
            if (existing != null)
            {
                preset.Id = existing.Id;
                col.Update(preset);
            }
            else
            {
                col.Insert(preset);
            }

            // Ensure index on Name for faster lookups
            col.EnsureIndex(x => x.Name);
        }

        public IEnumerable<PathPreset> LoadPresets()
        {
            return GetCollection().FindAll();
        }

        public void DeletePreset(string name)
        {
            Guard.AgainstNullOrWhiteSpace(name, nameof(name));
            var col = GetCollection();
            var existing = col.FindOne(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

            if (existing != null)
            {
                col.Delete(existing.Id);
            }
        }

        public void RenamePreset(string oldName, string newName)
        {
            Guard.AgainstNullOrWhiteSpace(oldName, nameof(oldName));
            Guard.AgainstNullOrWhiteSpace(newName, nameof(newName));

            var col = GetCollection();
            var preset = col.FindOne(p => p.Name.Equals(oldName, StringComparison.OrdinalIgnoreCase));

            if (preset != null)
            {
                // Check if target name exists
                var targetExists = col.Exists(p => p.Name.Equals(newName, StringComparison.OrdinalIgnoreCase));
                if (targetExists)
                {
                    // If target exists, delete it (overwrite behavior logic should be handled by UI confirmation, 
                    // but here we ensure consistency)
                    var target = col.FindOne(p => p.Name.Equals(newName, StringComparison.OrdinalIgnoreCase));
                    col.Delete(target.Id);
                }

                preset.Name = newName;
                preset.LastModified = DateTime.Now;
                col.Update(preset);
            }
        }

        public bool PresetExists(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return false;
            return GetCollection().Exists(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        }
    }
}