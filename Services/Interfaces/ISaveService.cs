using FileCraft.Models;

namespace FileCraft.Services.Interfaces
{
    public interface ISaveService
    {
        // Current State (AutoSave) methods
        SaveData LoadSaveData();
        void Save(SaveData saveData);
        void DeleteSaveData();
        string GetSaveDirectory();

        // Dynamic Presets (LiteDB) methods
        void SavePreset(string name, string description, SaveData data);
        IEnumerable<PresetEntity> LoadPresets();
        SaveData? LoadPresetData(int id);
        void DeletePreset(int id);
        void UpdatePreset(int id, string name, string description);
        bool PresetNameExists(string name);

        // Migration
        void ImportLegacyPresets();
    }
}