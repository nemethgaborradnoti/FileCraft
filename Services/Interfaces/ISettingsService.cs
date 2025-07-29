using FileCraft.Models;

namespace FileCraft.Services.Interfaces
{
    public interface ISettingsService
    {
        Settings LoadSettings();
        void SaveSettings(Settings settings);
        void SaveSettingsAsPreset(Settings settings, int presetNumber);
        Settings LoadSettingsFromPreset(int presetNumber);
    }
}
