using FileCraft.Models;

namespace FileCraft.Services.Interfaces
{
    public interface IPathPresetService
    {
        void SavePreset(PathPreset preset);
        IEnumerable<PathPreset> LoadPresets();
        void DeletePreset(string name);
        void RenamePreset(string oldName, string newName);
        bool PresetExists(string name);
    }
}