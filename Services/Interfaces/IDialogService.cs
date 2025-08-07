using FileCraft.Models;

namespace FileCraft.Services.Interfaces
{
    public interface IDialogService
    {
        string? SelectFolder(string description);
        void ShowNotification(string title, string message, DialogIconType iconType);
        bool ShowConfirmation(string title, string message, DialogIconType iconType, int? filesAffected = null);
        ExitConfirmationResult ShowExitConfirmation(string title, string message);
        string? ShowRenamePresetDialog(string currentName, int presetNumber);
    }
}
