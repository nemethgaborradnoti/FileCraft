using FileCraft.Models;
using FileCraft.ViewModels.Shared;

namespace FileCraft.Services.Interfaces
{
    public interface IDialogService
    {
        string? SelectFolder(string description);
        void ShowNotification(string title, string message, DialogIconType iconType);
        bool ShowConfirmation(string title, string message, DialogIconType iconType, int? filesAffected = null);

        bool ShowCopyTreeConfirmation(string title, DialogIconType iconType, CopyTreeConfirmationViewModel contentViewModel);

        ExitConfirmationResult ShowExitConfirmation(string title, string message);
        string? ShowRenamePresetDialog(string currentName, int presetNumber);
        string? ShowEditIgnoredFoldersDialog(string currentFolders);
        bool ShowBulkSearchDialog(IEnumerable<SelectableFile> allFiles);
        IEnumerable<string>? ShowIgnoredCommentsDialog(IEnumerable<SelectableFile> selectedFiles, IEnumerable<string> previouslyIgnoredFiles);
    }
}