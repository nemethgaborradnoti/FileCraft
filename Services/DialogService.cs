using FileCraft.Models;
using FileCraft.Services.Interfaces;
using FileCraft.ViewModels.Shared;
using FileCraft.Views.Shared;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media.Imaging;
using Application = System.Windows.Application;

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

        public bool ShowCopyTreeConfirmation(string title, DialogIconType iconType, string sourceName, string? sourceIcon, int sourceCount, string destName, string? destIcon, int destCount)
        {
            var viewModel = new ConfirmationViewModel
            {
                ActionName = title,
                IconPath = GetIconPath(iconType),
                IsCopyTreeMessage = true,
                SourceTabName = sourceName,
                SourceTabIcon = sourceIcon,
                SourceFolderCount = sourceCount,
                DestinationTabName = destName,
                DestinationTabIcon = destIcon,
                DestinationFolderCount = destCount
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

        public string? ShowEditIgnoredFoldersDialog(string currentFolders)
        {
            var editWindow = new EditIgnoredFoldersWindow(currentFolders);
            if (editWindow.ShowDialog() == true)
            {
                return editWindow.IgnoredFoldersText;
            }
            return null;
        }

        public void ShowPreview(string title, string content)
        {
            var previewWindow = new PreviewWindow(title, content);
            previewWindow.ShowDialog();
        }

        public bool ShowBulkSearchDialog(IEnumerable<SelectableFile> allFiles)
        {
            var bulkSearchWindow = new BulkSearchWindow(allFiles);
            return bulkSearchWindow.ShowDialog() ?? false;
        }

        private string GetIconPath(DialogIconType iconType)
        {
            string key = iconType switch
            {
                DialogIconType.Info => "IconInfo",
                DialogIconType.Warning => "IconWarning",
                DialogIconType.Success => "IconSuccess",
                DialogIconType.Error => "IconError",
                _ => throw new ArgumentOutOfRangeException(nameof(iconType), $"Not supported icon type: {iconType}")
            };

            if (Application.Current.FindResource(key) is BitmapImage image)
            {
                return image.UriSource.ToString();
            }

            return string.Empty;
        }
    }
}
