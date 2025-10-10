using FileCraft.Models;

namespace FileCraft.Services.Interfaces
{
    public interface IDialogService
    {
        string? SelectFolder(string description);
        void ShowNotification(string title, string message, DialogIconType iconType);
        bool ShowConfirmation(string title, string message, DialogIconType iconType, int? filesAffected = null);
        bool ShowCopyTreeConfirmation(string title, DialogIconType iconType, string sourceName, string? sourceIcon, int sourceCount, string destName, string? destIcon, int destCount);
        ExitConfirmationResult ShowExitConfirmation(string title, string message);
        string? ShowRenamePresetDialog(string currentName, int presetNumber);
        string? ShowEditIgnoredFoldersDialog(string currentFolders);
        void ShowPreview(string title, string content);
    }
}
