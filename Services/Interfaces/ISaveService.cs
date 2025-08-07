using FileCraft.Models;

namespace FileCraft.Services.Interfaces
{
    public interface ISaveService
    {
        SaveData LoadSaveData();
        void Save(SaveData saveData);
        void SaveAsPreset(SaveData saveData, int presetNumber);
        SaveData? LoadFromPreset(int presetNumber);
        bool CheckPresetExists(int presetNumber);
        string GetPresetName(int presetNumber);
        void UpdatePresetName(int presetNumber, string newName);
        void DeletePreset(int presetNumber);
        void DeleteSaveData();
        string GetSaveDirectory();
    }
}
