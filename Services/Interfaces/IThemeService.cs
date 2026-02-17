using FileCraft.Models;

namespace FileCraft.Services.Interfaces
{
    public interface IThemeService
    {
        void SwitchTheme(AppTheme theme);
        AppTheme GetCurrentTheme();
    }
}