using FileCraft.Models;

namespace FileCraft.Services.Interfaces
{
    public interface ISaveService
    {
        SaveData LoadSaveData();
        void Save(SaveData saveData);
        void DeleteSaveData();
        string GetSaveDirectory();

        void SavePreset(string name, string description, SaveData data);
        IEnumerable<PresetEntity> LoadPresets();
        SaveData? LoadPresetData(int id);
        void DeletePreset(int id);
        void UpdatePreset(int id, string name, string description);
        void UpdatePresetData(int id, SaveData data);
        bool PresetNameExists(string name);

        void ImportLegacyPresets();
    }
}