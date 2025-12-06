using FileCraft.Models;
using FileCraft.Services.Interfaces;
using FileCraft.ViewModels.Shared;
using FileCraft.Views.Shared;
using Fonts;
using System.Windows.Media.Imaging;
using Application = System.Windows.Application;
using Brush = System.Windows.Media.Brush;
using Brushes = System.Windows.Media.Brushes;

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
            string? iconPath = GetIconPath(iconType);
            var notificationWindow = new NotificationWindow(title, message, iconPath ?? string.Empty);
            notificationWindow.ShowDialog();
        }

        public bool ShowConfirmation(string title, string message, DialogIconType iconType, int? filesAffected = null)
        {
            var viewModel = new ConfirmationViewModel
            {
                ActionName = title,
                Message = message,
                FilesAffected = filesAffected,
                IconPath = GetIconPath(iconType) ?? string.Empty
            };
            var confirmationWindow = new ConfirmationWindow(viewModel);
            return confirmationWindow.ShowDialog() ?? false;
        }

        public bool ShowCopyTreeConfirmation(string title, DialogIconType iconType, string sourceName, string? sourceIcon, int sourceCount, string destName, string? destIcon, int destCount)
        {
            var viewModel = new ConfirmationViewModel
            {
                ActionName = title,
                IconPath = GetIconPath(iconType) ?? string.Empty,
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
            var (iconGlyph, iconBrush) = GetMaterialIconData(DialogIconType.Warning);
            var confirmationWindow = new ExitConfirmationWindow(title, message, iconGlyph, iconBrush);
            confirmationWindow.ShowDialog();
            return confirmationWindow.Result;
        }

        public string? ShowRenamePresetDialog(string currentName, int presetNumber)
        {
            var renameWindow = new RenamePresetWindow(currentName, presetNumber, this);
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
            string infoIconPath = GetIconPath(DialogIconType.Info) ?? string.Empty;
            var bulkSearchWindow = new BulkSearchWindow(allFiles, infoIconPath);
            return bulkSearchWindow.ShowDialog() ?? false;
        }

        public IEnumerable<string>? ShowIgnoredCommentsDialog(IEnumerable<SelectableFile> selectedFiles, IEnumerable<string> previouslyIgnoredFiles)
        {
            var window = new IgnoredCommentsWindow(selectedFiles, previouslyIgnoredFiles);
            if (window.ShowDialog() == true)
            {
                return window.GetIgnoredFilePaths();
            }
            return null;
        }

        public string? GetIconPath(string resourceKey)
        {
            if (Application.Current.FindResource(resourceKey) is BitmapImage image)
            {
                return image.UriSource.ToString();
            }
            return null;
        }

        private string? GetIconPath(DialogIconType iconType)
        {
            string key = iconType switch
            {
                DialogIconType.Info => "IconInfo",
                DialogIconType.Warning => "IconWarning",
                DialogIconType.Success => "IconSuccess",
                DialogIconType.Error => "IconError",
                _ => throw new ArgumentOutOfRangeException(nameof(iconType), $"Not supported icon type: {iconType}")
            };

            return GetIconPath(key);
        }

        private (string Glyph, Brush Brush) GetMaterialIconData(DialogIconType iconType)
        {
            string glyph = iconType switch
            {
                DialogIconType.Info => MaterialIcons.info,
                DialogIconType.Warning => MaterialIcons.warning,
                DialogIconType.Success => MaterialIcons.check_circle,
                DialogIconType.Error => MaterialIcons.cancel,
                _ => MaterialIcons.help_outline
            };

            string colorKey = iconType switch
            {
                DialogIconType.Info => "PrimaryBrush",
                DialogIconType.Warning => "DangerBrush",
                DialogIconType.Success => "SuccessBrush",
                DialogIconType.Error => "DangerBrush",
                _ => "TextBrush"
            };

            var brush = Application.Current.FindResource(colorKey) as Brush ?? Brushes.Black;
            return (glyph, brush);
        }
    }
}