using FileCraft.Models;

namespace FileCraft.Services.Interfaces
{
    public interface ISettingsService
    {
        Settings LoadSettings();

        void SaveSettings(Settings settings);
    }
}