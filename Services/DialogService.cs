using FileCraft.Models;
using FileCraft.Services.Interfaces;
using FileCraft.ViewModels.Shared;
using FileCraft.Views.Shared;

namespace FileCraft.Services
{
    public class DialogService : IDialogService
    {
        public string? SelectFolder(string description)
        {
            var dialog = new Microsoft.Win32.OpenFolderDialog
            {
                Title = description
            };

            if (dialog.ShowDialog() == true)
            {
                return dialog.FolderName;
            }

            return null;
        }

        public void ShowNotification(string title, string message, DialogIconType iconType)
        {
            string iconPath = GetIconPath(iconType);
            var notificationWindow = new NotificationWindow(title, message, iconPath);
            notificationWindow.ShowDialog();
        }

        public bool ShowConfirmation(string title, string message, DialogIconType iconType, int? filesAffected = null)
        {
            var viewModel = new ConfirmationViewModel
            {
                ActionName = title,
                Message = message,
                FilesAffected = filesAffected,
                IconPath = GetIconPath(iconType)
            };
            var confirmationWindow = new ConfirmationWindow(viewModel);
            return confirmationWindow.ShowDialog() ?? false;
        }

        public ExitConfirmationResult ShowExitConfirmation(string title, string message)
        {
            var confirmationWindow = new ExitConfirmationWindow(title, message);
            confirmationWindow.ShowDialog();
            return confirmationWindow.Result;
        }

        public string? ShowRenamePresetDialog(string currentName, int presetNumber)
        {
            var renameWindow = new RenamePresetWindow(currentName, presetNumber);
            if (renameWindow.ShowDialog() == true)
            {
                return renameWindow.NewPresetName;
            }
            return null;
        }

        private string GetIconPath(DialogIconType iconType)
        {
            return iconType switch
            {
                DialogIconType.Info => "pack://application:,,,/Resources/info01.png",
                DialogIconType.Warning => "pack://application:,,,/Resources/warning01.png",
                DialogIconType.Success => "pack://application:,,,/Resources/checked01.png",
                DialogIconType.Error => "pack://application:,,,/Resources/cancel01.png",
                _ => throw new ArgumentOutOfRangeException(nameof(iconType), $"Not supported icon type: {iconType}")
            };
        }
    }
}
