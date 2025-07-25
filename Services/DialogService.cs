using FileCraft.Services.Interfaces;
using FileCraft.Views.Shared;

namespace FileCraft.Services
{
    public class DialogService : IDialogService
    {
        public string? SelectFolder(string description)
        {
            var dialog = new FolderBrowserDialog
            {
                Description = description,
                UseDescriptionForTitle = true
            };

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                return dialog.SelectedPath;
            }

            return null;
        }

        public void ShowNotification(string title, string message)
        {
            var notificationWindow = new NotificationWindow(title, message);
            notificationWindow.ShowDialog();
        }
    }
}
