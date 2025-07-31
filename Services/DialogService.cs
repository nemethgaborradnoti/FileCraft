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

        public void ShowNotification(string title, string message)
        {
            var notificationWindow = new NotificationWindow(title, message);
            notificationWindow.ShowDialog();
        }

        public bool ShowConfirmation(string title, string message, int? filesAffected = null)
        {
            var viewModel = new ConfirmationViewModel
            {
                ActionName = title,
                Message = message,
                FilesAffected = filesAffected
            };
            var confirmationWindow = new ConfirmationWindow(viewModel);
            return confirmationWindow.ShowDialog() ?? false;
        }
    }
}
